using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// Emits the generated half of a <c>[BleService]</c> class and its context type.
/// </summary>
static class ServiceEmitter
{
    public static string EmitContext(ServiceModel service)
    {
        var writer = CodeWriter.File();
        using (writer.Namespace(service.Namespace))
        {
            writer.Line("/// <summary>");
            writer.Line($"/// Per-central state for <see cref=\"{service.ClassName}\"/>. Declare your own half of this");
            writer.Line("/// partial class to stamp whatever properties you want onto every connected central.");
            writer.Line("/// </summary>");
            writer.GeneratedCode();
            using (writer.Block($"{service.Accessibility} partial class {service.ContextTypeName} : {Names.BleServiceContext}"))
            {
                writer.Line("/// <summary>Creates a new instance. Called by generated code.</summary>");
                writer.Line($"public {service.ContextTypeName}(");
                writer.Line($"    {Names.Peripheral} peripheral,");
                writer.Line("    string serviceUuid,");
                writer.Line($"    global::System.Func<{Names.GattService}?> serviceAccessor");
                writer.Line(") : base(peripheral, serviceUuid, serviceAccessor)");
                writer.Line("{");
                writer.Line("}");
            }
        }
        return writer.ToString();
    }


    public static string EmitService(ServiceModel service)
    {
        var writer = CodeWriter.File();
        using (writer.Namespace(service.Namespace))
        {
            writer.GeneratedCode();
            using (writer.Block($"partial class {service.ClassName}"))
            {
                EmitInfrastructure(writer, service);

                foreach (var characteristic in service.Characteristics)
                    EmitHandlers(writer, service, characteristic);

                EmitBuild(writer, service);
                EmitNotifyApi(writer, service);
            }
        }
        return writer.ToString();
    }


    static void EmitInfrastructure(CodeWriter writer, ServiceModel service)
    {
        writer.Line($"/// <summary>The UUID this service is hosted on.</summary>");
        writer.Line($"public const string BleServiceUuid = \"{service.ServiceUuid}\";");
        writer.Line();

        writer.Line($"readonly global::System.Threading.CancellationTokenSource {Names.HostTokenField} = new();");
        writer.Line($"{Names.GattService}? __bleService;");
        foreach (var characteristic in service.Characteristics)
            writer.Line($"{Names.GattCharacteristic}? {characteristic.FieldName};");

        writer.Line();
        writer.Line("/// <summary>Cancelled when the hosted service is torn down. Bind it by declaring a CancellationToken parameter on any handler.</summary>");
        writer.Line($"public {Names.CancellationToken} BleHostToken => this.{Names.HostTokenField}.Token;");
        writer.Line();
        writer.Line("/// <summary>The registered GATT service, or null until registration completes.</summary>");
        writer.Line($"public {Names.GattService}? BleService => this.__bleService;");
        writer.Line();

        writer.Line("/// <summary>Called when a handler throws. Implement it in your half of the class to observe failures.</summary>");
        writer.Line($"partial void OnBleHandlerError(string characteristicUuid, {Names.Exception} exception);");
        writer.Line();
        writer.Line("/// <summary>Called when a request/response reply could not be delivered because the central was not subscribed.</summary>");
        writer.Line($"partial void OnBleResponseDropped(string characteristicUuid, {Names.Peripheral} peripheral, byte[] data);");
        writer.Line();

        writer.Line("/// <summary>Returns this central's context, creating it on first use.</summary>");
        using (writer.Block($"{service.ContextFullyQualifiedName} GetContext({Names.Peripheral} peripheral)"))
        {
            writer.Line($"return {Names.BleContextStore}.GetOrAdd(");
            writer.Line("    peripheral,");
            writer.Line($"    () => new {service.ContextFullyQualifiedName}(peripheral, BleServiceUuid, () => this.__bleService)");
            writer.Line(");");
        }
        writer.Line();

        writer.Line("/// <summary>Records the registered service. Called by generated registration code.</summary>");
        writer.Line($"internal void AttachBleService({Names.GattService} service) => this.__bleService = service;");
        writer.Line();
        writer.Line("/// <summary>Cancels in-flight handlers. Called by generated registration code.</summary>");
        using (writer.Block("internal void ShutdownBleService()"))
        {
            writer.Line($"try {{ this.{Names.HostTokenField}.Cancel(); }} catch ({Names.Exception}) {{ /* already disposed */ }}");
        }
        writer.Line();
    }


    // ---- per characteristic handlers ---------------------------------------------------------

    static void EmitHandlers(CodeWriter writer, ServiceModel service, CharacteristicModel characteristic)
    {
        if (characteristic.Read != null)
            EmitRead(writer, characteristic, characteristic.Read);

        if (characteristic.Write != null)
            EmitWrite(writer, characteristic, characteristic.Write);

        if (characteristic.RequestResponse != null)
            EmitRequestResponse(writer, characteristic, characteristic.RequestResponse);

        if (characteristic.NotifyHook != null)
            EmitSubscription(writer, characteristic, characteristic.NotifyHook);
    }


    static void EmitRead(CodeWriter writer, CharacteristicModel characteristic, HandlerModel handler)
    {
        var name = ReadMethod(characteristic);
        var signature = $"global::System.Threading.Tasks.Task<{Names.GattResult}> {name}({Names.ReadRequest} request)";

        using (writer.Block(handler.IsAwaitable ? "async " + signature : signature))
        {
            using (writer.Block("try"))
            {
                var call = handler.IsAwaitable ? $"await {handler.Invocation}.ConfigureAwait(false)" : handler.Invocation;
                var result = handler.Result == HandlerResult.Bytes
                    ? $"{Names.GattResult}.Success({call})"
                    : call;

                writer.Line(handler.IsAwaitable ? $"return {result};" : $"return {Task()}.FromResult({result});");
            }
            using (writer.Block($"catch ({Names.Exception} ex)"))
            {
                writer.Line($"this.OnBleHandlerError(\"{characteristic.Uuid}\", ex);");
                var error = $"{Names.GattResult}.Error({Names.GattState}.Failure)";
                writer.Line(handler.IsAwaitable ? $"return {error};" : $"return {Task()}.FromResult({error});");
            }
        }
        writer.Line();
    }


    static void EmitWrite(CodeWriter writer, CharacteristicModel characteristic, HandlerModel handler)
    {
        var name = WriteMethod(characteristic);
        var signature = $"{Task()} {name}({Names.WriteRequest} request)";

        using (writer.Block(handler.IsAwaitable ? "async " + signature : signature))
        {
            writer.Line($"var status = {Names.GattState}.Success;");
            using (writer.Block("try"))
            {
                var call = handler.IsAwaitable ? $"await {handler.Invocation}.ConfigureAwait(false)" : handler.Invocation;
                writer.Line(handler.Result == HandlerResult.GattState ? $"status = {call};" : $"{call};");
            }
            using (writer.Block($"catch ({Names.Exception} ex)"))
            {
                writer.Line($"this.OnBleHandlerError(\"{characteristic.Uuid}\", ex);");
                writer.Line($"status = {Names.GattState}.Failure;");
            }

            if (characteristic.ManualRespond)
                writer.Line("// ManualRespond is set - the handler answers the central itself");
            else
                writer.Line("if (request.IsReplyNeeded) request.Respond(status);");

            if (!handler.IsAwaitable)
                writer.Line($"return {Task()}.CompletedTask;");
        }
        writer.Line();
    }


    static void EmitRequestResponse(CodeWriter writer, CharacteristicModel characteristic, HandlerModel handler)
    {
        var name = WriteMethod(characteristic);

        using (writer.Block($"async {Task()} {name}({Names.WriteRequest} request)"))
        {
            writer.Line($"{Names.GattResult} result;");
            using (writer.Block("try"))
            {
                var call = handler.IsAwaitable ? $"await {handler.Invocation}.ConfigureAwait(false)" : handler.Invocation;
                writer.Line(handler.Result == HandlerResult.Bytes
                    ? $"result = {Names.GattResult}.Success({call});"
                    : $"result = {call};");
            }
            using (writer.Block($"catch ({Names.Exception} ex)"))
            {
                writer.Line($"this.OnBleHandlerError(\"{characteristic.Uuid}\", ex);");
                writer.Line($"result = {Names.GattResult}.Error({Names.GattState}.Failure);");
            }
            writer.Line();
            writer.Line("if (request.IsReplyNeeded) request.Respond(result.Status);");
            writer.Line();
            writer.Line("// a GATT write response carries no payload, so the reply travels as a notification");
            writer.Line("// addressed to the central that wrote - which means it has to be subscribed");
            using (writer.Block($"if (result.Status == {Names.GattState}.Success && result.Data is not null)"))
            {
                writer.Line($"var characteristic = this.{characteristic.FieldName};");
                using (writer.Block($"if ({Names.BleHostingRuntime}.IsSubscribed(characteristic, request.Peripheral))"))
                {
                    writer.Line("await characteristic!.Notify(result.Data, request.Peripheral).ConfigureAwait(false);");
                }
                using (writer.Block("else"))
                {
                    writer.Line($"this.OnBleResponseDropped(\"{characteristic.Uuid}\", request.Peripheral, result.Data);");
                }
            }
        }
        writer.Line();
    }


    static void EmitSubscription(CodeWriter writer, CharacteristicModel characteristic, HandlerModel handler)
    {
        var name = SubscriptionMethod(characteristic);
        var signature = $"{Task()} {name}({Names.CharacteristicSubscription} subscription)";

        using (writer.Block(handler.IsAwaitable ? "async " + signature : signature))
        {
            using (writer.Block("try"))
            {
                writer.Line(handler.IsAwaitable
                    ? $"await {handler.Invocation}.ConfigureAwait(false);"
                    : $"{handler.Invocation};");
            }
            using (writer.Block($"catch ({Names.Exception} ex)"))
            {
                writer.Line($"this.OnBleHandlerError(\"{characteristic.Uuid}\", ex);");
            }

            if (!handler.IsAwaitable)
                writer.Line($"return {Task()}.CompletedTask;");
        }
        writer.Line();
    }


    // ---- builder -----------------------------------------------------------------------------

    static void EmitBuild(CodeWriter writer, ServiceModel service)
    {
        writer.Line("/// <summary>Adds this class's characteristics to the service being built. Called by generated registration code.</summary>");
        using (writer.Block($"internal void BuildBleService({Names.GattServiceBuilder} builder)"))
        {
            foreach (var characteristic in service.Characteristics)
            {
                writer.Open($"this.{characteristic.FieldName} = builder.AddCharacteristic(\"{characteristic.Uuid}\", characteristic =>");
                foreach (var line in BuildCalls(characteristic))
                    writer.Line(line);

                writer.Close(");");
                writer.Line();
            }
        }
        writer.Line();
    }


    static IEnumerable<string> BuildCalls(CharacteristicModel characteristic)
    {
        if (characteristic.Read != null)
            yield return $"characteristic.SetRead(this.{ReadMethod(characteristic)}, {(characteristic.ReadEncrypted ? "true" : "false")});";

        if (characteristic.Write != null || characteristic.RequestResponse != null)
            yield return $"characteristic.SetWrite(this.{WriteMethod(characteristic)}, {characteristic.WriteOptionsExpression});";

        if (characteristic.HasNotify)
        {
            var hook = characteristic.NotifyHook == null ? "null" : $"this.{SubscriptionMethod(characteristic)}";
            yield return $"characteristic.SetNotification({hook}, {characteristic.NotificationOptionsExpression});";
        }
    }


    // ---- notify push api ---------------------------------------------------------------------

    static void EmitNotifyApi(CodeWriter writer, ServiceModel service)
    {
        foreach (var characteristic in service.Characteristics)
        {
            if (!characteristic.HasNotify || characteristic.NotifyName == null)
                continue;

            var name = characteristic.NotifyName;

            writer.Line($"/// <summary>Pushes a value on characteristic {characteristic.Uuid} to subscribed centrals, or to the ones named.</summary>");
            using (writer.Block($"public {Task()} Notify{name}(byte[] data, params {Names.Peripheral}[] centrals)"))
            {
                writer.Line($"var characteristic = this.{characteristic.FieldName};");
                writer.Line($"return characteristic == null ? {Task()}.CompletedTask : characteristic.Notify(data, centrals);");
            }
            writer.Line();

            writer.Line($"/// <summary>The centrals currently subscribed to characteristic {characteristic.Uuid}.</summary>");
            writer.Line($"public global::System.Collections.Generic.IReadOnlyList<{Names.Peripheral}> {name}Subscribers");
            writer.Line($"    => this.{characteristic.FieldName}?.SubscribedCentrals ?? global::System.Array.Empty<{Names.Peripheral}>();");
            writer.Line();

            writer.Line($"/// <summary>Whether any central is subscribed to characteristic {characteristic.Uuid}.</summary>");
            writer.Line($"public bool Has{name}Subscribers => this.{name}Subscribers.Count > 0;");
            writer.Line();
        }
    }


    static string Task() => "global::System.Threading.Tasks.Task";

    static string ReadMethod(CharacteristicModel characteristic) => "__BleRead_" + Uuids.ToIdentifier(characteristic.Uuid);
    static string WriteMethod(CharacteristicModel characteristic) => "__BleWrite_" + Uuids.ToIdentifier(characteristic.Uuid);
    static string SubscriptionMethod(CharacteristicModel characteristic) => "__BleSubscription_" + Uuids.ToIdentifier(characteristic.Uuid);
}

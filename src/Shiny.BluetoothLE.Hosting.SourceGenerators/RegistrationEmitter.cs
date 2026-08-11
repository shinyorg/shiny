using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// All the classes that merge into one hosted service UUID, plus the L2CAP listener (if any) whose
/// PSM is published on it.
/// </summary>
sealed record ServiceGroup(
    string Uuid,
    bool Primary,
    string RegistrationName,
    bool Advertise,
    IReadOnlyList<ServiceModel> Members,
    L2CapModel? PsmOwner,
    string? PsmCharacteristicUuid
);


/// <summary>
/// Emits the single assembly-level registration class.
/// </summary>
static class RegistrationEmitter
{
    public static string Emit(
        string rootNamespace,
        IReadOnlyList<ServiceGroup> groups,
        IReadOnlyList<L2CapModel> l2caps,
        bool hasDependencyInjection
    )
    {
        var parameters = ParameterNames(groups, l2caps);

        var writer = CodeWriter.File();
        using (writer.Namespace(rootNamespace))
        {
            writer.Line("/// <summary>");
            writer.Line("/// Registration for every [BleService] and [L2CapService] declared in this assembly.");
            writer.Line("/// </summary>");
            writer.GeneratedCode();
            using (writer.Block("public static class BleHostedServiceRegistrationExtensions"))
            {
                if (hasDependencyInjection)
                {
                    EmitDiRegistration(writer, groups, l2caps);
                    EmitAttachFromProvider(writer, groups, l2caps);
                }

                EmitAttach(writer, groups, l2caps, parameters);
                EmitAdvertising(writer, groups);

                foreach (var group in groups)
                    EmitAddGroup(writer, group, parameters);

                foreach (var l2cap in l2caps)
                    EmitAddL2Cap(writer, l2cap, parameters);
            }
        }
        return writer.ToString();
    }


    static void EmitDiRegistration(CodeWriter writer, IReadOnlyList<ServiceGroup> groups, IReadOnlyList<L2CapModel> l2caps)
    {
        writer.Line("/// <summary>Registers every generated BLE hosted service as a singleton.</summary>");
        using (writer.Block($"public static {Names.ServiceCollection} AddBleHostedServices(this {Names.ServiceCollection} services)"))
        {
            foreach (var type in AllTypes(groups, l2caps))
                writer.Line($"global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<{type}>(services);");

            writer.Line("return services;");
        }
        writer.Line();
    }


    static void EmitAttachFromProvider(
        CodeWriter writer,
        IReadOnlyList<ServiceGroup> groups,
        IReadOnlyList<L2CapModel> l2caps
    )
    {
        var types = AllTypes(groups, l2caps).ToList();

        writer.Line("/// <summary>Resolves every generated BLE hosted service from the container and brings it up. Dispose the result to tear everything down.</summary>");
        using (writer.Block($"public static {SessionTask()} AttachBleHostedServices(this {Names.BleHostingManager} manager, {Names.ServiceProvider} services)"))
        {
            writer.Line("return AttachBleHostedServices(");
            writer.Line("    manager" + (types.Count > 0 ? "," : ""));

            for (var i = 0; i < types.Count; i++)
            {
                var comma = i == types.Count - 1 ? "" : ",";
                writer.Line($"    ({types[i]})services.GetService(typeof({types[i]}))!{comma}");
            }
            writer.Line(");");
        }
        writer.Line();
    }


    static void EmitAttach(
        CodeWriter writer,
        IReadOnlyList<ServiceGroup> groups,
        IReadOnlyList<L2CapModel> l2caps,
        IReadOnlyDictionary<string, string> parameters
    )
    {
        var arguments = AllTypes(groups, l2caps).Select(t => $"{t} {parameters[t]}").ToList();

        writer.Line("/// <summary>Brings up every generated BLE hosted service. Dispose the result to tear everything down.</summary>");
        using (writer.Block($"public static async {SessionTask()} AttachBleHostedServices(this {Names.BleHostingManager} manager, {String.Join(", ", arguments)})"))
        {
            writer.Line($"var gattServices = new global::System.Collections.Generic.List<{Names.GattService}>({groups.Count});");
            writer.Line($"var l2capInstances = new global::System.Collections.Generic.List<{Names.L2CapInstance}>({l2caps.Count});");
            writer.Line($"var shutdowns = new global::System.Collections.Generic.List<global::System.Action>({groups.Count + l2caps.Count});");
            writer.Line();

            if (l2caps.Count > 0)
            {
                writer.Line("// listeners first, so a PSM characteristic read right after registration returns a live value");
                foreach (var l2cap in l2caps)
                {
                    var name = parameters[l2cap.FullyQualifiedName];
                    writer.Line($"l2capInstances.Add(await {name}.OpenL2Cap(manager).ConfigureAwait(false));");
                    writer.Line($"shutdowns.Add({name}.ShutdownL2Cap);");
                }
                writer.Line();
            }

            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var local = "service" + i;

                EmitAddServiceCall(writer, group, parameters, local);
                foreach (var member in group.Members)
                {
                    var name = parameters[member.FullyQualifiedName];
                    writer.Line($"{name}.AttachBleService({local});");
                    writer.Line($"shutdowns.Add({name}.ShutdownBleService);");
                }
                writer.Line($"gattServices.Add({local});");
                writer.Line();
            }

            writer.Line($"return new {Names.BleHostedServiceSession}(manager, gattServices, l2capInstances, shutdowns);");
        }
        writer.Line();
    }


    static void EmitAddServiceCall(
        CodeWriter writer,
        ServiceGroup group,
        IReadOnlyDictionary<string, string> parameters,
        string local
    )
    {
        writer.Open($"var {local} = await manager.AddService(\"{group.Uuid}\", {(group.Primary ? "true" : "false")}, builder =>");
        foreach (var member in group.Members)
            writer.Line($"{parameters[member.FullyQualifiedName]}.BuildBleService(builder);");

        if (group.PsmOwner != null && group.PsmCharacteristicUuid != null)
        {
            var owner = parameters[group.PsmOwner.FullyQualifiedName];
            writer.Line();
            writer.Line($"// publishes {group.PsmOwner.ClassName}'s PSM - centrals have no other in-band way to learn it");
            writer.Line($"builder.AddCharacteristic(\"{group.PsmCharacteristicUuid}\", characteristic => characteristic.SetRead(");
            writer.Line("    _ => global::System.Threading.Tasks.Task.FromResult(");
            writer.Line($"        {Names.GattResult}.Success({Names.BleHostingRuntime}.EncodePsm({owner}.Psm))");
            writer.Line("    )");
            writer.Line("));");
        }

        writer.Close(").ConfigureAwait(false);");
    }


    static void EmitAdvertising(CodeWriter writer, IReadOnlyList<ServiceGroup> groups)
    {
        var advertised = groups.Where(g => g.Advertise).Select(g => g.Uuid).ToList();
        if (advertised.Count == 0)
            return;

        writer.Line("/// <summary>Starts advertising every service UUID declared with Advertise = true.</summary>");
        writer.Line("/// <param name=\"manager\">The hosting manager.</param>");
        writer.Line("/// <param name=\"localName\">Optional local name to advertise.</param>");
        using (writer.Block($"public static global::System.Threading.Tasks.Task StartBleHostedAdvertising(this {Names.BleHostingManager} manager, string? localName = null)"))
        {
            var uuids = String.Join(", ", advertised.Select(u => $"\"{u}\""));
            writer.Line($"return manager.StartAdvertising(new {Names.AdvertisementOptions}(localName, {uuids}));");
        }
        writer.Line();
    }


    static void EmitAddGroup(CodeWriter writer, ServiceGroup group, IReadOnlyDictionary<string, string> parameters)
    {
        var types = group.Members.Select(m => m.FullyQualifiedName).ToList();
        if (group.PsmOwner != null)
            types.Add(group.PsmOwner.FullyQualifiedName);

        var arguments = types.Select(t => $"{t} {parameters[t]}");

        writer.Line($"/// <summary>Registers service {group.Uuid} on its own.</summary>");
        using (writer.Block($"public static async global::System.Threading.Tasks.Task<{Names.GattService}> Add{group.RegistrationName}(this {Names.BleHostingManager} manager, {String.Join(", ", arguments)})"))
        {
            EmitAddServiceCall(writer, group, parameters, "service");
            foreach (var member in group.Members)
                writer.Line($"{parameters[member.FullyQualifiedName]}.AttachBleService(service);");

            writer.Line("return service;");
        }
        writer.Line();
    }


    static void EmitAddL2Cap(CodeWriter writer, L2CapModel l2cap, IReadOnlyDictionary<string, string> parameters)
    {
        var name = parameters[l2cap.FullyQualifiedName];

        writer.Line($"/// <summary>Publishes {l2cap.ClassName}'s PSM on its own.</summary>");
        using (writer.Block($"public static global::System.Threading.Tasks.Task<{Names.L2CapInstance}> Add{l2cap.RegistrationName}(this {Names.BleHostingManager} manager, {l2cap.FullyQualifiedName} {name})"))
        {
            writer.Line($"return {name}.OpenL2Cap(manager);");
        }
        writer.Line();
    }


    // ---- helpers -------------------------------------------------------------------------------

    static IEnumerable<string> AllTypes(IReadOnlyList<ServiceGroup> groups, IReadOnlyList<L2CapModel> l2caps)
    {
        foreach (var group in groups)
        {
            foreach (var member in group.Members)
                yield return member.FullyQualifiedName;
        }

        foreach (var l2cap in l2caps)
            yield return l2cap.FullyQualifiedName;
    }


    static IReadOnlyDictionary<string, string> ParameterNames(IReadOnlyList<ServiceGroup> groups, IReadOnlyList<L2CapModel> l2caps)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in AllTypes(groups, l2caps).Distinct())
        {
            var simple = type.Substring(type.LastIndexOf('.') + 1);
            var candidate = Camel(simple);
            var suffix = 2;
            while (!used.Add(candidate))
                candidate = Camel(simple) + suffix++;

            names[type] = candidate;
        }
        return names;
    }


    static string Camel(string value)
    {
        if (value.Length == 0)
            return "service";

        var camel = Char.ToLowerInvariant(value[0]) + value.Substring(1);
        return SyntaxKeywords.Contains(camel) ? "@" + camel : camel;
    }


    static readonly HashSet<string> SyntaxKeywords = new(StringComparer.Ordinal)
    {
        "base", "class", "delegate", "event", "interface", "internal", "namespace",
        "new", "object", "operator", "out", "params", "public", "ref", "return",
        "static", "string", "this", "true", "false", "void"
    };


    static string SessionTask() => $"global::System.Threading.Tasks.Task<{Names.BleHostedServiceSession}>";
}

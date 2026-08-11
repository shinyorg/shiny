using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


static class Parser
{
    public static ParseResult<ServiceModel> ParseService(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol symbol || !IsHostable(symbol, context.TargetNode))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.NotPartial, LocationInfo.From(context.TargetNode), context.TargetSymbol.Name
            ));
            return new ParseResult<ServiceModel>(null, diagnostics.ToEquatableArray());
        }

        var attribute = context.Attributes[0];
        if (!TryUuid(attribute, 0, symbol, diagnostics, out var serviceUuid))
            return new ParseResult<ServiceModel>(null, diagnostics.ToEquatableArray());

        var contextTypeName = symbol.Name + "Context";
        var contextFullName = Qualify(symbol.ContainingNamespace, contextTypeName);

        var characteristics = ParseCharacteristics(symbol, contextFullName, diagnostics);
        var explicitName = Named<string?>(attribute, "Name", null);

        var model = new ServiceModel(
            ClassName: symbol.Name,
            Namespace: NamespaceOf(symbol),
            FullyQualifiedName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ContextTypeName: contextTypeName,
            ContextFullyQualifiedName: contextFullName,
            Accessibility: AccessibilityOf(symbol),
            ServiceUuid: serviceUuid,
            Primary: Named(attribute, "Primary", true),
            Advertise: Named(attribute, "Advertise", false),
            RegistrationName: Identifier(explicitName ?? symbol.Name),
            NameIsExplicit: !String.IsNullOrWhiteSpace(explicitName),
            Characteristics: characteristics.ToEquatableArray(),
            Location: LocationInfo.From(symbol)
        );

        return new ParseResult<ServiceModel>(model, diagnostics.ToEquatableArray());
    }


    public static ParseResult<L2CapModel> ParseL2Cap(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol symbol || !IsHostable(symbol, context.TargetNode))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.NotPartial, LocationInfo.From(context.TargetNode), context.TargetSymbol.Name
            ));
            return new ParseResult<L2CapModel>(null, diagnostics.ToEquatableArray());
        }

        var attribute = context.Attributes[0];
        var location = LocationInfo.From(symbol);

        var psmService = Named<string?>(attribute, "PsmService", null);
        var psmCharacteristic = Named<string?>(attribute, "PsmCharacteristic", null);
        string? psmServiceUuid = null;
        string? psmCharacteristicUuid = null;

        if (psmService != null || psmCharacteristic != null)
        {
            if (psmService == null || psmCharacteristic == null)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.InvalidPsmPublication, location,
                    $"'{symbol.Name}' must set both PsmService and PsmCharacteristic, or neither"
                ));
            }
            else if (!Uuids.TryNormalize(psmService, out var normalizedService))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidUuid, location, psmService));
            }
            else if (!Uuids.TryNormalize(psmCharacteristic, out var normalizedCharacteristic))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidUuid, location, psmCharacteristic));
            }
            else
            {
                psmServiceUuid = normalizedService;
                psmCharacteristicUuid = normalizedCharacteristic;
            }
        }

        var handlers = symbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.GetAttributes().Any(a => IsAttribute(a, Names.OnChannelOpenedAttribute)))
            .ToList();

        HandlerModel? handler = null;
        if (handlers.Count != 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.ChannelHandlerCount, location, symbol.Name, handlers.Count.ToString()
            ));
        }
        else
        {
            handler = SignatureBinder.Bind(
                handlers[0],
                "L2CAP channel",
                new Dictionary<string, string>
                {
                    [Names.L2CapChannel] = "channel",
                    [Names.BleL2CapContext] = "channelContext",
                    [Names.CancellationToken] = "cancellationToken"
                },
                AllowedResults.None,
                diagnostics
            );
        }

        var model = new L2CapModel(
            ClassName: symbol.Name,
            Namespace: NamespaceOf(symbol),
            FullyQualifiedName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Accessibility: AccessibilityOf(symbol),
            Secure: Named(attribute, "Secure", false),
            PsmServiceUuid: psmServiceUuid,
            PsmCharacteristicUuid: psmCharacteristicUuid,
            RegistrationName: Identifier(Named<string?>(attribute, "Name", null) ?? symbol.Name),
            Handler: handler,
            Location: location
        );

        return new ParseResult<L2CapModel>(model, diagnostics.ToEquatableArray());
    }


    /// <summary>
    /// Catches characteristic handlers sitting on a type that never declared [BleService] - they
    /// would otherwise be silently ignored.
    /// </summary>
    public static DiagnosticInfo? ParseOrphanHandler(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol method)
            return null;

        var containing = method.ContainingType;
        if (containing == null || containing.GetAttributes().Any(a => IsAttribute(a, Names.BleServiceAttribute)))
            return null;

        return DiagnosticInfo.Create(
            Diagnostics.MissingBleService,
            LocationInfo.From(method),
            method.Name,
            containing.Name
        );
    }


    // ---- characteristics -------------------------------------------------------------------

    static List<CharacteristicModel> ParseCharacteristics(
        INamedTypeSymbol symbol,
        string contextFullName,
        List<DiagnosticInfo> diagnostics
    )
    {
        // insertion ordered so generated output is stable
        var builders = new List<CharacteristicBuilder>();

        CharacteristicBuilder Get(string uuid, LocationInfo? location)
        {
            var existing = builders.FirstOrDefault(b => b.Uuid == uuid);
            if (existing != null)
                return existing;

            var created = new CharacteristicBuilder(uuid, location);
            builders.Add(created);
            return created;
        }

        // class level [NotifyCharacteristic] - the push API without a subscription hook
        foreach (var attribute in symbol.GetAttributes().Where(a => IsAttribute(a, Names.NotifyCharacteristicAttribute)))
        {
            if (!TryUuid(attribute, 0, symbol, diagnostics, out var uuid))
                continue;

            var name = Named<string?>(attribute, "Name", null);
            if (String.IsNullOrWhiteSpace(name))
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.NotifyNameRequired, LocationInfo.From(symbol), uuid, symbol.Name
                ));
                continue;
            }

            var builder = Get(uuid, LocationInfo.From(symbol));
            if (!builder.DeclareNotify(symbol.Name, diagnostics))
                continue;

            builder.NotifyName = Identifier(name!);
            builder.NotificationOptions = NotificationOptionsFor(attribute, symbol.Name, diagnostics, LocationInfo.From(symbol));
        }

        foreach (var method in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            var location = LocationInfo.From(method);

            foreach (var attribute in method.GetAttributes())
            {
                var kind = AttributeKind(attribute);
                if (kind == null)
                    continue;

                if (!TryUuid(attribute, 0, method, diagnostics, out var uuid))
                    continue;

                var builder = Get(uuid, location);
                switch (kind)
                {
                    case "read":
                        if (!builder.DeclareRead(method.Name, diagnostics))
                            break;

                        builder.ReadEncrypted = Named(attribute, "Encrypted", false);
                        builder.Read = SignatureBinder.Bind(
                            method, "read", ReadBindings(contextFullName), AllowedResults.Bytes | AllowedResults.GattResult, diagnostics, contextFullName
                        );
                        break;

                    case "write":
                        if (!builder.DeclareWrite(method.Name, diagnostics))
                            break;

                        builder.ManualRespond = Named(attribute, "ManualRespond", false);
                        builder.WriteOptions = WriteOptionsFor(attribute, method.Name, diagnostics, location);
                        builder.Write = SignatureBinder.Bind(
                            method, "write", WriteBindings(contextFullName), AllowedResults.None | AllowedResults.GattState, diagnostics, contextFullName
                        );

                        if (builder.ManualRespond && builder.Write != null)
                        {
                            var ownsRequest = builder.Write.Arguments.Contains("request");
                            if (!ownsRequest || builder.Write.Result == HandlerResult.GattState)
                                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ManualRespondMisuse, location, method.Name));
                        }
                        break;

                    case "notify":
                        if (!builder.DeclareNotify(method.Name, diagnostics))
                            break;

                        builder.NotifyName = Identifier(Named<string?>(attribute, "Name", null) ?? StripAffixes(method.Name));
                        builder.NotificationOptions = NotificationOptionsFor(attribute, method.Name, diagnostics, location);
                        builder.NotifyHook = SignatureBinder.Bind(
                            method, "notify", NotifyBindings(contextFullName), AllowedResults.None, diagnostics, contextFullName
                        );
                        break;

                    case "requestresponse":
                        if (!builder.DeclareRequestResponse(method.Name, diagnostics))
                            break;

                        builder.NotifyName = Identifier(Named<string?>(attribute, "Name", null) ?? StripAffixes(method.Name));
                        builder.NotificationOptions = NotificationOptionsFor(attribute, method.Name, diagnostics, location);
                        builder.RequestResponse = SignatureBinder.Bind(
                            method, "request/response", WriteBindings(contextFullName), AllowedResults.Bytes | AllowedResults.GattResult, diagnostics, contextFullName
                        );
                        break;
                }
            }
        }

        return builders.Select(b => b.Build()).ToList();
    }


    static Dictionary<string, string> ReadBindings(string contextFullName) => new()
    {
        [Names.ReadRequest] = "request",
        [contextFullName] = "this.GetContext(request.Peripheral)",
        [Names.Peripheral] = "request.Peripheral",
        [Names.GattCharacteristic] = "request.Characteristic",
        ["int"] = "request.Offset",
        [Names.CancellationToken] = "this.BleHostToken"
    };


    static Dictionary<string, string> WriteBindings(string contextFullName) => new()
    {
        ["byte[]"] = "request.Data",
        [Names.WriteRequest] = "request",
        [contextFullName] = "this.GetContext(request.Peripheral)",
        [Names.Peripheral] = "request.Peripheral",
        [Names.GattCharacteristic] = "request.Characteristic",
        ["int"] = "request.Offset",
        ["bool"] = "request.IsReplyNeeded",
        [Names.CancellationToken] = "this.BleHostToken"
    };


    static Dictionary<string, string> NotifyBindings(string contextFullName) => new()
    {
        [Names.BleSubscription] = $"{Names.BleSubscription}.From(subscription)",
        [Names.CharacteristicSubscription] = "subscription",
        [contextFullName] = "this.GetContext(subscription.Peripheral)",
        [Names.Peripheral] = "subscription.Peripheral",
        [Names.GattCharacteristic] = "subscription.Characteristic",
        ["bool"] = "subscription.IsSubscribing",
        [Names.CancellationToken] = "this.BleHostToken"
    };


    // ---- option enums ----------------------------------------------------------------------

    static string WriteOptionsFor(AttributeData attribute, string owner, List<DiagnosticInfo> diagnostics, LocationInfo? location)
    {
        var selected = new List<string>(3);
        if (Named(attribute, "EncryptionRequired", false)) selected.Add("EncryptionRequired");
        if (Named(attribute, "AuthenticatedSignedWrites", false)) selected.Add("AuthenticatedSignedWrites");
        if (Named(attribute, "WriteWithoutResponse", false)) selected.Add("WriteWithoutResponse");

        var chosen = selected.Count == 0 ? "Write" : selected[0];
        if (selected.Count > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.UnexpressibleOptions, location,
                String.Join(" + ", selected), owner, "WriteOptions", chosen
            ));
        }
        return $"{Names.WriteOptions}.{chosen}";
    }


    static string NotificationOptionsFor(AttributeData attribute, string owner, List<DiagnosticInfo> diagnostics, LocationInfo? location)
    {
        var selected = new List<string>(2);
        // security first: quietly dropping encryption is worse than quietly sending notify for indicate,
        // which the central notices immediately
        if (Named(attribute, "EncryptionRequired", false)) selected.Add("EncryptionRequired");
        if (Named(attribute, "Indicate", false)) selected.Add("Indicate");

        var chosen = selected.Count == 0 ? "Notify" : selected[0];
        if (selected.Count > 1)
        {
            diagnostics.Add(DiagnosticInfo.Create(
                Diagnostics.UnexpressibleOptions, location,
                String.Join(" + ", selected), owner, "NotificationOptions", chosen
            ));
        }
        return $"{Names.NotificationOptions}.{chosen}";
    }


    // ---- helpers ---------------------------------------------------------------------------

    static bool IsHostable(INamedTypeSymbol symbol, SyntaxNode node)
        => node is ClassDeclarationSyntax declaration &&
           declaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
           !symbol.IsStatic &&
           !symbol.IsGenericType &&
           symbol.ContainingType == null;


    static string? AttributeKind(AttributeData attribute)
    {
        var name = attribute.AttributeClass?.ToDisplayString();
        return name switch
        {
            Names.ReadCharacteristicAttribute => "read",
            Names.WriteCharacteristicAttribute => "write",
            Names.NotifyCharacteristicAttribute => "notify",
            Names.RequestResponseCharacteristicAttribute => "requestresponse",
            _ => null
        };
    }


    static bool IsAttribute(AttributeData attribute, string fullName)
        => attribute.AttributeClass?.ToDisplayString() == fullName;


    static bool TryUuid(AttributeData attribute, int index, ISymbol owner, List<DiagnosticInfo> diagnostics, out string uuid)
    {
        uuid = String.Empty;
        var raw = attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as string
            : null;

        if (Uuids.TryNormalize(raw, out uuid))
            return true;

        diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidUuid, LocationInfo.From(owner), raw ?? "(null)"));
        return false;
    }


    static T Named<T>(AttributeData attribute, string name, T fallback)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is T value)
                return value;
        }
        return fallback;
    }


    static string NamespaceOf(INamedTypeSymbol symbol)
        => symbol.ContainingNamespace.IsGlobalNamespace ? String.Empty : symbol.ContainingNamespace.ToDisplayString();


    static string Qualify(INamespaceSymbol ns, string typeName)
        => ns.IsGlobalNamespace ? $"global::{typeName}" : $"global::{ns.ToDisplayString()}.{typeName}";


    static string AccessibilityOf(INamedTypeSymbol symbol) => symbol.DeclaredAccessibility switch
    {
        Microsoft.CodeAnalysis.Accessibility.Public => "public",
        _ => "internal"
    };


    /// <summary>
    /// Trims the affixes people habitually put on hook methods so <c>OnHeartRateAsync</c> yields
    /// <c>HeartRate</c> and the generated members read as <c>NotifyHeartRate</c>.
    /// </summary>
    static string StripAffixes(string methodName)
    {
        var name = methodName;
        if (name.StartsWith("On", StringComparison.Ordinal) && name.Length > 2)
            name = name.Substring(2);

        if (name.EndsWith("Async", StringComparison.Ordinal) && name.Length > 5)
            name = name.Substring(0, name.Length - 5);

        return name.Length == 0 ? methodName : name;
    }


    static string Identifier(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (Char.IsLetterOrDigit(c) || c == '_')
                builder.Append(c);
        }

        var result = builder.ToString();
        if (result.Length == 0)
            return "Characteristic";

        return Char.IsDigit(result[0]) ? "_" + result : result;
    }


    sealed class CharacteristicBuilder(string uuid, LocationInfo? location)
    {
        public string Uuid { get; } = uuid;

        public HandlerModel? Read { get; set; }
        public bool ReadEncrypted { get; set; }
        public HandlerModel? Write { get; set; }
        public string WriteOptions { get; set; } = $"{Names.WriteOptions}.Write";
        public bool ManualRespond { get; set; }
        public HandlerModel? RequestResponse { get; set; }
        public HandlerModel? NotifyHook { get; set; }
        public string? NotifyName { get; set; }
        public string NotificationOptions { get; set; } = $"{Names.NotificationOptions}.Notify";

        bool hasRead;
        bool hasWrite;
        bool hasNotify;
        bool hasRequestResponse;


        public bool DeclareRead(string owner, List<DiagnosticInfo> diagnostics)
            => this.Declare(ref this.hasRead, "read", owner, diagnostics);


        public bool DeclareWrite(string owner, List<DiagnosticInfo> diagnostics)
        {
            if (this.hasRequestResponse)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.RequestResponseConflict, location, this.Uuid, "write"));
                return false;
            }
            return this.Declare(ref this.hasWrite, "write", owner, diagnostics);
        }


        public bool DeclareNotify(string owner, List<DiagnosticInfo> diagnostics)
        {
            if (this.hasRequestResponse)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.RequestResponseConflict, location, this.Uuid, "notify"));
                return false;
            }
            return this.Declare(ref this.hasNotify, "notify", owner, diagnostics);
        }


        public bool DeclareRequestResponse(string owner, List<DiagnosticInfo> diagnostics)
        {
            if (this.hasWrite || this.hasNotify)
            {
                diagnostics.Add(DiagnosticInfo.Create(
                    Diagnostics.RequestResponseConflict, location, this.Uuid, this.hasWrite ? "write" : "notify"
                ));
                return false;
            }
            return this.Declare(ref this.hasRequestResponse, "request/response", owner, diagnostics);
        }


        bool Declare(ref bool flag, string kind, string owner, List<DiagnosticInfo> diagnostics)
        {
            if (flag)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateHandler, location, this.Uuid, kind, owner));
                return false;
            }
            flag = true;
            return true;
        }


        public CharacteristicModel Build() => new(
            Uuid: this.Uuid,
            FieldName: "__char_" + Uuids.ToIdentifier(this.Uuid),
            Read: this.Read,
            ReadEncrypted: this.ReadEncrypted,
            Write: this.Write,
            WriteOptionsExpression: this.WriteOptions,
            ManualRespond: this.ManualRespond,
            RequestResponse: this.RequestResponse,
            HasNotify: this.hasNotify || this.hasRequestResponse,
            NotifyHook: this.NotifyHook,
            NotifyName: this.NotifyName,
            NotificationOptionsExpression: this.NotificationOptions,
            Location: location
        );
    }
}

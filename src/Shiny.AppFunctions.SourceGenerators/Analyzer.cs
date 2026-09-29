using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Shiny.AppFunctions.SourceGenerators;

/// <summary>Turns the compilation into a <see cref="GenerationModel"/>, reporting problems as diagnostics.</summary>
sealed class Analyzer
{
    const string Ns = "Shiny.AppFunctions.";
    static readonly Regex IdPattern = new("^[a-z][a-z0-9_]*$");
    static readonly SymbolDisplayFormat Fqn = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    readonly Compilation compilation;
    readonly List<Diagnostic> diagnostics = new();
    readonly INamedTypeSymbol? functionAttribute, parameterAttribute, entityAttribute, shortcutAttribute;
    readonly INamedTypeSymbol? function0, function1, handler1, handler2, entityQuery, functionDelegate;
    readonly Dictionary<ITypeSymbol, EntityModel> entities = new(SymbolEqualityComparer.Default);
    readonly Dictionary<string, TypeModel> enums = new();

    public Analyzer(Compilation compilation)
    {
        this.compilation = compilation;
        this.functionAttribute = compilation.GetTypeByMetadataName(Ns + "AppFunctionAttribute");
        this.parameterAttribute = compilation.GetTypeByMetadataName(Ns + "AppParameterAttribute");
        this.entityAttribute = compilation.GetTypeByMetadataName(Ns + "AppEntityAttribute");
        this.shortcutAttribute = compilation.GetTypeByMetadataName(Ns + "AppShortcutAttribute");
        this.function0 = compilation.GetTypeByMetadataName(Ns + "IAppFunction");
        this.function1 = compilation.GetTypeByMetadataName(Ns + "IAppFunction`1");
        this.handler1 = compilation.GetTypeByMetadataName(Ns + "IAppFunctionHandler`1");
        this.handler2 = compilation.GetTypeByMetadataName(Ns + "IAppFunctionHandler`2");
        this.entityQuery = compilation.GetTypeByMetadataName(Ns + "IAppEntityQuery`1");
        this.functionDelegate = compilation.GetTypeByMetadataName(Ns + "IAppFunctionDelegate");
    }

    public IReadOnlyList<Diagnostic> Diagnostics => this.diagnostics;

    /// <summary>Null when the compilation does not reference Shiny.AppFunctions.</summary>
    public GenerationModel? Analyze()
    {
        if (this.functionAttribute == null || this.function0 == null || this.handler1 == null || this.handler2 == null || this.entityQuery == null || this.functionDelegate == null)
            return null;

        var types = new List<INamedTypeSymbol>();
        Collect(this.compilation.Assembly.GlobalNamespace, types);

        var model = new GenerationModel();
        var classes = types.Where(t => t.TypeKind == TypeKind.Class && !t.IsAbstract && !t.IsStatic && t.TypeParameters.Length == 0).ToList();

        // entities first: function parameters refer to them
        foreach (var type in types)
        {
            var attr = Attribute(type, this.entityAttribute);
            if (attr != null)
                this.AnalyzeEntity(type, attr, classes, model);
        }

        foreach (var type in types)
        {
            var attr = Attribute(type, this.functionAttribute);
            if (attr != null)
            {
                var fn = this.AnalyzeFunction(type, attr, classes);
                if (fn != null)
                    model.Functions.Add(fn);
            }
        }

        foreach (var cls in classes.Where(c => c.AllInterfaces.Contains(this.functionDelegate, SymbolEqualityComparer.Default)))
        {
            if (this.HasPublicConstructor(cls))
                model.Delegates.Add(cls.ToDisplayString(Fqn));
        }

        model.Enums.AddRange(this.enums.Values.OrderBy(x => x.TypeName, StringComparer.Ordinal));
        this.CheckIds(model, types);
        return model;
    }

    void AnalyzeEntity(INamedTypeSymbol type, AttributeData attr, List<INamedTypeSymbol> classes, GenerationModel model)
    {
        var id = (attr.ConstructorArguments.FirstOrDefault().Value as string) ?? "";
        if (!IdPattern.IsMatch(id))
            this.Report(Rules.InvalidId, type, id);

        var props = PublicProperties(type).ToList();
        var idProp = props.FirstOrDefault(p => p.Name == "Id" && p.Type.SpecialType == SpecialType.System_String);
        if (idProp == null)
        {
            this.Report(Rules.EntityNeedsId, type, type.Name);
            return;
        }

        var display = Named(attr, "DisplayProperty") as string
            ?? props.FirstOrDefault(p => p.Name == "Name")?.Name
            ?? props.FirstOrDefault(p => p.Name == "Title")?.Name
            ?? "Id";
        if (!props.Any(p => p.Name == display))
            this.Report(Rules.EntityDisplay, type, type.Name, display);

        var queryInterface = this.entityQuery!.Construct(type);
        var query = classes.FirstOrDefault(c => c.AllInterfaces.Contains(queryInterface, SymbolEqualityComparer.Default));
        if (query == null)
            this.Report(Rules.EntityNoQuery, type, type.Name);
        else if (!this.HasPublicConstructor(query))
            query = null;

        var entity = new EntityModel
        {
            Id = id,
            Title = Named(attr, "Title") as string ?? Words(type.Name),
            CSharpType = type.ToDisplayString(Fqn),
            SimpleName = type.Name,
            TypeName = TypeName(type),
            DisplayProperty = display,
            QueryType = query?.ToDisplayString(Fqn)
        };
        this.entities[type] = entity;
        model.Entities.Add(entity);
    }

    FunctionModel? AnalyzeFunction(INamedTypeSymbol type, AttributeData attr, List<INamedTypeSymbol> classes)
    {
        var id = (attr.ConstructorArguments.FirstOrDefault().Value as string) ?? "";
        if (!IdPattern.IsMatch(id))
        {
            this.Report(Rules.InvalidId, type, id);
            return null;
        }

        var markers = type.AllInterfaces
            .Where(i => SymbolEqualityComparer.Default.Equals(i, this.function0) || SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, this.function1))
            .ToList();
        if (markers.Count != 1)
        {
            this.Report(Rules.MissingInterface, type, type.Name);
            return null;
        }

        var title = Named(attr, "Title") as string ?? Words(type.Name);
        var fn = new FunctionModel
        {
            Id = id,
            Title = title,
            Description = Named(attr, "Description") as string ?? title,
            OpensApp = Named(attr, "OpensApp") is true,
            RequestType = type.ToDisplayString(Fqn),
            RequestSimpleName = type.Name
        };

        // result
        INamedTypeSymbol handlerInterface;
        if (markers[0].IsGenericType)
        {
            var resultType = markers[0].TypeArguments[0];
            if (!this.TryMapResult(resultType, out var result, out var error, 0))
            {
                this.Report(Rules.UnsupportedType, type, "result", resultType.ToDisplayString(), error);
                return null;
            }
            fn.Result = result;
            handlerInterface = this.handler2!.Construct(type, resultType);
        }
        else
        {
            fn.Result = new TypeModel { Kind = ValueKind.Void };
            handlerInterface = this.handler1!.Construct(type);
        }
        fn.HandlerInterface = handlerInterface.ToDisplayString(Fqn);

        var handlers = classes.Where(c => c.AllInterfaces.Contains(handlerInterface, SymbolEqualityComparer.Default)).ToList();
        if (handlers.Count == 0)
            this.Report(Rules.NoHandler, type, handlerInterface.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), id);
        else if (handlers.Count > 1)
            this.Report(Rules.MultipleHandlers, type, id, String.Join(", ", handlers.Select(h => h.Name)));
        else if (this.HasPublicConstructor(handlers[0]))
            fn.HandlerType = handlers[0].ToDisplayString(Fqn);

        if (!this.AnalyzeParameters(type, fn))
            return null;

        // Apple App Shortcuts
        foreach (var sc in type.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, this.shortcutAttribute)))
        {
            var phrase = sc.ConstructorArguments.FirstOrDefault().Value as string ?? "";
            if (!phrase.Contains("${applicationName}"))
                this.Report(Rules.ShortcutPhrase, type, phrase);
            fn.ShortcutPhrases.Add(phrase);
            fn.ShortcutTitle ??= Named(sc, "ShortTitle") as string;
            fn.ShortcutImage ??= Named(sc, "SystemImage") as string;
        }
        return fn;
    }

    bool AnalyzeParameters(INamedTypeSymbol type, FunctionModel fn)
    {
        var props = PublicProperties(type).ToList();

        // prefer the widest public constructor whose parameters all match properties (positional records)
        var ctor = type.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .Where(c => !(c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, type))) // record copy ctor
            .Where(c => c.Parameters.All(p => props.Any(pr => String.Equals(pr.Name, p.Name, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        if (ctor == null)
        {
            this.Report(Rules.NoConstructor, type, type.Name);
            return false;
        }

        var ok = true;
        // a property that is neither a constructor parameter nor publicly settable is computed: not a parameter
        var parameters = props
            .Select(prop =>
            {
                var ctorParam = ctor.Parameters.FirstOrDefault(p => String.Equals(p.Name, prop.Name, StringComparison.OrdinalIgnoreCase));
                return (Property: prop, CtorIndex: ctorParam == null ? -1 : ctor.Parameters.IndexOf(ctorParam));
            })
            .Where(x => x.CtorIndex >= 0 || x.Property.SetMethod is { DeclaredAccessibility: Accessibility.Public });

        foreach (var (prop, ctorIndex) in parameters)
        {
            if (!this.TryMapParameter(prop.Type, out var mapped, out var error))
            {
                this.Report(Rules.UnsupportedType, prop, prop.Name, prop.Type.ToDisplayString(), error);
                ok = false;
            }
            else
            {
                var pAttr = Attribute(prop, this.parameterAttribute)
                    ?? (ctorIndex >= 0 ? Attribute(ctor.Parameters[ctorIndex], this.parameterAttribute) : null);

                fn.Parameters.Add(new ParameterModel
                {
                    CSharpName = prop.Name,
                    WireName = CamelCase(prop.Name),
                    Title = (pAttr != null ? Named(pAttr, "Title") as string : null) ?? Words(prop.Name),
                    Description = pAttr != null ? Named(pAttr, "Description") as string : null,
                    Type = mapped,
                    // constructor parameters are required unless nullable; initializer properties only when marked `required`
                    IsRequired = !mapped.IsNullable && (ctorIndex >= 0 || prop.IsRequired),
                    ConstructorIndex = ctorIndex
                });
            }
        }

        // constructor parameters must all be satisfiable
        if (ok && ctor.Parameters.Any(p => fn.Parameters.All(x => x.ConstructorIndex != ctor.Parameters.IndexOf(p))))
        {
            this.Report(Rules.NoConstructor, type, type.Name);
            return false;
        }
        return ok;
    }

    bool TryMapParameter(ITypeSymbol type, out TypeModel model, out string error)
    {
        error = "parameters can be string, int, long, double, bool, DateTimeOffset, an enum or an [AppEntity] (optionally nullable)";
        model = this.MapScalar(type)!;
        if (model != null)
            return true;

        var (inner, nullable) = Unwrap(type);
        if (this.entities.TryGetValue(inner, out var entity))
        {
            model = new TypeModel
            {
                Kind = ValueKind.Entity,
                IsNullable = nullable,
                CSharpBaseType = entity.CSharpType,
                CSharpType = entity.CSharpType,
                TypeName = entity.TypeName,
                SimpleName = entity.SimpleName,
                EntityId = entity.Id
            };
            return true;
        }
        model = null!;
        return false;
    }

    bool TryMapResult(ITypeSymbol type, out TypeModel model, out string error, int depth)
    {
        error = "results can be string, int, long, double, bool, DateTimeOffset, an enum, a class/record of those, or a list of them";
        model = this.MapScalar(type)!;
        if (model != null)
            return true;

        if (depth > 4)
        {
            error = "results can be nested at most 4 levels deep";
            return false;
        }

        var (inner, nullable) = Unwrap(type);

        var item = ItemType(inner);
        if (item != null)
        {
            if (ItemType(item) != null || !this.TryMapResult(item, out var itemModel, out error, depth + 1))
                return false;
            model = new TypeModel
            {
                Kind = ValueKind.Array,
                IsNullable = nullable,
                CSharpBaseType = inner.ToDisplayString(Fqn),
                CSharpType = inner.ToDisplayString(Fqn),
                ItemType = itemModel
            };
            return true;
        }

        if (inner is INamedTypeSymbol named && (named.TypeKind == TypeKind.Class || named.TypeKind == TypeKind.Struct) && named.SpecialType == SpecialType.None && !named.IsGenericType)
        {
            var obj = new TypeModel
            {
                Kind = ValueKind.Object,
                IsNullable = nullable,
                IsValueType = named.IsValueType,
                CSharpBaseType = named.ToDisplayString(Fqn),
                CSharpType = type.ToDisplayString(Fqn),
                TypeName = TypeName(named),
                SimpleName = named.Name
            };
            foreach (var prop in PublicProperties(named))
            {
                if (!this.TryMapResult(prop.Type, out var propModel, out var propError, depth + 1))
                {
                    error = $"{named.Name}.{prop.Name}: {propError}";
                    return false;
                }
                obj.Properties.Add(new PropertyModel { CSharpName = prop.Name, WireName = CamelCase(prop.Name), Type = propModel });
            }
            model = obj;
            return true;
        }
        return false;
    }

    TypeModel? MapScalar(ITypeSymbol type)
    {
        var (inner, nullable) = Unwrap(type);
        var kind = inner.SpecialType switch
        {
            SpecialType.System_String => ValueKind.String,
            SpecialType.System_Int32 => ValueKind.Int32,
            SpecialType.System_Int64 => ValueKind.Int64,
            SpecialType.System_Double => ValueKind.Double,
            SpecialType.System_Boolean => ValueKind.Boolean,
            _ => (ValueKind?)null
        };
        if (kind == null && inner.ToDisplayString() == "System.DateTimeOffset")
            kind = ValueKind.DateTimeOffset;

        if (kind != null)
        {
            var baseType = inner.ToDisplayString(Fqn);
            return new TypeModel
            {
                Kind = kind.Value,
                IsNullable = nullable,
                IsValueType = inner.IsValueType,
                CSharpBaseType = baseType,
                CSharpType = nullable && inner.IsValueType ? baseType + "?" : baseType
            };
        }

        if (inner.TypeKind == TypeKind.Enum)
        {
            var typeName = TypeName(inner);
            if (!this.enums.TryGetValue(typeName, out var e))
            {
                e = new TypeModel
                {
                    Kind = ValueKind.Enum,
                    CSharpBaseType = inner.ToDisplayString(Fqn),
                    CSharpType = inner.ToDisplayString(Fqn),
                    TypeName = typeName,
                    SimpleName = inner.Name,
                    EnumValues = inner.GetMembers().OfType<IFieldSymbol>().Where(f => f.HasConstantValue).Select(f => f.Name).ToList()
                };
                this.enums[typeName] = e;
            }
            return new TypeModel
            {
                Kind = ValueKind.Enum,
                IsNullable = nullable,
                IsValueType = true,
                CSharpBaseType = e.CSharpBaseType,
                CSharpType = nullable ? e.CSharpBaseType + "?" : e.CSharpBaseType,
                TypeName = e.TypeName,
                SimpleName = e.SimpleName,
                EnumValues = e.EnumValues
            };
        }
        return null;
    }

    void CheckIds(GenerationModel model, List<INamedTypeSymbol> types)
    {
        var owners = new Dictionary<string, List<string>>();
        void Add(string id, string owner)
        {
            if (!owners.TryGetValue(id, out var list))
                owners[id] = list = new List<string>();
            list.Add(owner);
        }
        foreach (var f in model.Functions)
            Add(f.Id, f.RequestSimpleName);
        foreach (var e in model.Entities)
            Add("search_" + e.Id, e.SimpleName + " (generated search)");

        foreach (var pair in owners.Where(x => x.Value.Count > 1))
            this.diagnostics.Add(Diagnostic.Create(Rules.DuplicateId, Location.None, pair.Key, String.Join(", ", pair.Value)));

        var entityIds = model.Entities.GroupBy(e => e.Id).Where(g => g.Count() > 1);
        foreach (var g in entityIds)
            this.diagnostics.Add(Diagnostic.Create(Rules.DuplicateId, Location.None, g.Key, String.Join(", ", g.Select(e => e.SimpleName))));

        var shortcuts = model.Functions.Count(f => f.ShortcutPhrases.Count > 0);
        if (shortcuts > 10)
            this.diagnostics.Add(Diagnostic.Create(Rules.TooManyShortcuts, Location.None, shortcuts));
    }

    bool HasPublicConstructor(INamedTypeSymbol type)
    {
        if (type.InstanceConstructors.Any(c => c.DeclaredAccessibility == Accessibility.Public))
            return true;
        this.Report(Rules.NoPublicConstructor, type, type.Name);
        return false;
    }

    void Report(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
        => this.diagnostics.Add(Diagnostic.Create(descriptor, symbol.Locations.FirstOrDefault(l => l.IsInSource) ?? Location.None, args));

    static void Collect(INamespaceSymbol ns, List<INamedTypeSymbol> types)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member is INamespaceSymbol child)
                Collect(child, types);
            else if (member is INamedTypeSymbol type)
                CollectType(type, types);
        }
    }

    static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types)
    {
        if (!type.Locations.Any(l => l.IsInSource))
            return;
        types.Add(type);
        foreach (var nested in type.GetTypeMembers())
            CollectType(nested, types);
    }

    static IEnumerable<IPropertySymbol> PublicProperties(INamedTypeSymbol type)
        => type.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic && !p.IsIndexer && p.GetMethod != null && p.Name != "EqualityContract");

    static (ITypeSymbol Type, bool Nullable) Unwrap(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } n)
            return (n.TypeArguments[0], true);
        return (type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), type.NullableAnnotation == NullableAnnotation.Annotated);
    }

    static ITypeSymbol? ItemType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
            return array.ElementType;
        if (type.SpecialType == SpecialType.System_String || type is not INamedTypeSymbol named)
            return null;

        var enumerable = named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T
            ? named
            : named.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
        return enumerable?.TypeArguments[0];
    }

    static AttributeData? Attribute(ISymbol symbol, INamedTypeSymbol? attribute)
        => attribute == null ? null : symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));

    static object? Named(AttributeData attr, string name)
        => attr.NamedArguments.FirstOrDefault(x => x.Key == name).Value.Value;

    static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");

    /// <summary>System.Text.Json's camelCase: lowercases the leading run of capitals ("URLPath" → "urlPath").</summary>
    public static string CamelCase(string name)
    {
        if (name.Length == 0 || !Char.IsUpper(name[0]))
            return name;
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !Char.IsUpper(chars[i]))
                break;
            var hasNext = i + 1 < chars.Length;
            if (i > 0 && hasNext && !Char.IsUpper(chars[i + 1]))
            {
                if (Char.IsSeparator(chars[i + 1]))
                    chars[i] = Char.ToLowerInvariant(chars[i]);
                break;
            }
            chars[i] = Char.ToLowerInvariant(chars[i]);
        }
        return new string(chars);
    }

    /// <summary>"CreateOrder" → "Create Order", "HTTPStatus" → "HTTP Status".</summary>
    public static string Words(string name)
    {
        var words = Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        return words.Replace("_", " ").Trim();
    }
}

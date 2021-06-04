using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public static class Extensions
    {
        public static string GetRootNamespace(this GeneratorExecutionContext context)
        {
            string? rootNamespace = null;
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.RootNamespace", out rootNamespace))
                rootNamespace = context.Compilation.AssemblyName;

            return rootNamespace ?? "ShinyApp";
        }


        public static bool HasReference(this GeneratorExecutionContext context, string neededLibrary)
            => context.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals(neededLibrary));


        public static void Log(this GeneratorExecutionContext context, string id, string message, DiagnosticSeverity severity = DiagnosticSeverity.Warning, Location? location = null)
        {
            location ??= Location.None;
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id,
                        message,
                        message,
                        "Shiny",
                        severity,
                        true
                    ),
                    location
                )
            );
        }


        public static void TryDebug(this GeneratorExecutionContext context)
        {
            //Debugger.IsAttached &&
            var debug = context.Compilation.Assembly.GetTypeByMetadataName("Shiny.ShinyGeneratorDebugAttribute") != null;
            if (debug)
                Debugger.Launch();
        }


        //public static bool IsPartialClass(this ISymbol symbol) => symbol
        //    .DeclaringSyntaxReferences
        //    .Select(reference => reference.GetSyntax(CancellationToken.None))
        //    .OfType<ClassDeclarationSyntax>()
        //    .Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));


        public static AttributeData? GetClassAttributeData(this Compilation compilation, ITypeSymbol symbol, string attributeTypeName)
        {
            var attributeType = compilation.GetTypeByMetadataName(attributeTypeName);
            if (attributeType == null)
                return null;

            return symbol.GetClassAttributeData(attributeType);
        }


        public static AttributeData? GetClassAttributeData(this ITypeSymbol symbol, ITypeSymbol attributeSymbol) => symbol
            .GetAllAttributes()
            .FirstOrDefault(x => x.AttributeClass.Name == attributeSymbol.Name);


        //public static bool HasXamarinForms(this GeneratorExecutionContext context)
        //    => context.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals("Xamarin.Forms", StringComparison.CurrentCultureIgnoreCase));

        public static bool HasMsal(this GeneratorExecutionContext context)
            => context.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals("Microsoft.Identity.Client", StringComparison.CurrentCultureIgnoreCase));

        public static bool HasXamarinEssentials(this GeneratorExecutionContext context)
            => context.Compilation.ReferencedAssemblyNames.Any(x => x.Name.Equals("Xamarin.Essentials", StringComparison.CurrentCultureIgnoreCase));


        public static IEnumerable<IAssemblySymbol> GetAllAssemblies(this GeneratorExecutionContext context)
        {
            yield return context.Compilation.Assembly;
            foreach (var reference in context.Compilation.References)
            {
                if (reference.Properties.Kind == MetadataImageKind.Assembly)
                {
                    var assembly = (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(reference);
                    if (assembly != null)
                        yield return assembly;
                }
            }
        }


        public static IEnumerable<INamedTypeSymbol> GetAllTypeSymbols(this IAssemblySymbol assembly)
        {
            var stack = new Stack<INamespaceSymbol>();
            stack.Push(assembly.GlobalNamespace);

            while (stack.Count > 0)
            {
                var @namespace = stack.Pop();

                foreach (var member in @namespace.GetMembers())
                {
                    if (member is INamespaceSymbol memberAsNamespace)
                        stack.Push(memberAsNamespace);

                    else if (member is INamedTypeSymbol symbol)
                        yield return symbol;

                    else if (member is IAssemblySymbol reference)
                    {
                        var symbols = reference.GetAllTypeSymbols();
                        foreach (var s in symbols)
                            yield return s;
                    }
                }
            }
        }


        public static bool HasMethod(this INamedTypeSymbol symbol, string methodName)
        {
            var members = symbol.GetMembers();
            var exists = members
                .OfType<IMethodSymbol>()
                .Any(x => x.Name.Equals(methodName));
            return exists;
        }


        public static AttributeData? GetCurrentAssemblyAttribute(this GeneratorExecutionContext context, string attributeTypeName)
        {
            var attribute = context.Compilation.GetTypeByMetadataName(attributeTypeName);
            if (attribute == null)
                return null;

            var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
            var attributeData = assemblyAttributes.FirstOrDefault(
                x => x.AttributeClass.ToDisplayString() == attribute.ToDisplayString()
            );

            return attributeData;
        }


        public static bool IsEvent(this IMethodSymbol method) => method.MethodKind switch
        {
            MethodKind.EventAdd => true,
            MethodKind.EventRemove => true,
            _ => false
        };
        public static bool IsProperty(this IMethodSymbol method) => method.MethodKind switch
        {
            MethodKind.PropertyGet => true,
            MethodKind.PropertySet => true,
            _ => false
        };
        public static bool IsPublic(this ITypeSymbol symbol)
            => symbol.DeclaredAccessibility == Accessibility.Public;
        public static bool IsPublic(this IMethodSymbol symbol)
            => symbol.DeclaredAccessibility == Accessibility.Public;
        public static bool IsInterface(this ITypeSymbol type)
            => type != null && type.TypeKind == TypeKind.Interface;

        public static string GetName(this IPropertySymbol property) => property
            .Name
            .Replace("get_", String.Empty)
            .Replace("set_", String.Empty);


        public static bool Implements(this INamedTypeSymbol symbol, ITypeSymbol type)
        {
            if (type == null)
                return false;

            if (!type.IsInterface())
                throw new ArgumentException("Symbol is not an interface");

            return symbol.AllInterfaces.Any(i => type.Equals(i));
        }


        public static bool Inherits(this INamedTypeSymbol symbol, ITypeSymbol type)
        {
            if (type.IsInterface())
                throw new ArgumentException("Type cannot be an interface");

            var current = symbol;
            while (current != null)
            {
                if (type.Equals(current))
                    return true;

                current = current.BaseType;
            }
            return false;
        }


        public static AttributeData? FindAttributeFlattened(this ISymbol symbol, INamedTypeSymbol attributeClassSymbol)
        {
            var attrs = symbol.GetAllAttributes();
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass.Name == attributeClassSymbol.Name)
                    return attr;
            }
            return null;
        }


        public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol symbol)
        {
            while (symbol != null && symbol.Name != "Object")
            {
                var attributes = symbol.GetAttributes();
                foreach (var attribute in attributes)
                    yield return attribute;

                symbol = (symbol as INamedTypeSymbol)?.BaseType;
            }
        }


        public static bool Is(this INamedTypeSymbol symbol, INamedTypeSymbol otherSymbol)
        {
            while (symbol != null && symbol.Name != "Object")
            {
                if (symbol.ToDisplayString() == otherSymbol.ToDisplayString())
                    return true;

                symbol = symbol?.BaseType;
            }

            return false;
        }


        public static IEnumerable<IPropertySymbol> GetAllProperties(this INamedTypeSymbol symbol)
        {
            while (symbol != null && symbol.Name != "Object")
            {
                var properties = symbol
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(x => x.DeclaredAccessibility == Accessibility.Public);

                foreach (var property in properties)
                    yield return property;

                symbol = symbol?.BaseType;
            }
        }


        public static IEnumerable<IMethodSymbol> GetAllPublicMethods(this ITypeSymbol type)
        {
            if (type.IsInterface())
            {
                var interfaces = new List<ITypeSymbol>();
                interfaces.Add(type);
                interfaces.AddRange(type.AllInterfaces);

                var methods = interfaces.SelectMany(x => x
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(y =>
                        y.IsPublic() &&
                        !y.IsProperty() &&
                        !y.IsEvent()
                    )
                );

                foreach (var method in methods)
                    yield return method;

                yield break;
            }
            else
            {
                var currentType = type;

                while (currentType != null)
                {
                    var methods = currentType
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(x =>
                            x.IsPublic() &&
                            !x.IsAbstract &&
                            !x.IsEvent() &&
                            !x.IsProperty()
                        )
                        .ToList();

                    foreach (var method in methods)
                        yield return method;

                    currentType = currentType?.BaseType;
                }
            }
        }


        public static void AppendNamespaces(this IndentedStringBuilder builder, params string[] nameSpaces)
        {
            builder.AppendLineInvariant("using System;");
            builder.AppendLineInvariant("using Shiny;");
            foreach (var ns in nameSpaces)
                builder.AppendLineInvariant($"using {ns};");

            builder.AppendLine();
            builder.AppendLine();
        }
    }
}

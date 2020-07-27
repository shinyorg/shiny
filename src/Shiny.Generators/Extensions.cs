using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    static class Extensions
    {
        //public static bool HasShinyCore(this SourceGeneratorContext context)
        //    => context.Compilation.GetTypeByMetadataName("Shiny.ShinyHost") != null;

        public static bool IsPartialClass(this INamedTypeSymbol symbol) =>
            symbol.Locations.Length > 1 || symbol.DeclaringSyntaxReferences.Length > 1;


        public static bool HasMethod(this INamedTypeSymbol symbol, string methodName)
            => symbol.GetTypeMembers(methodName).OfType<IMethodSymbol>().Any();

        public static bool HasXamarinForms(this SourceGeneratorContext context)
            => context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Application") != null;


        public static bool HasAssemblyAttribute(this SourceGeneratorContext context, string attributeName)
        {
            var attribute = context.Compilation.GetTypeByMetadataName(attributeName);
            return context.Compilation.Assembly.FindAttributeFlattened(attribute) != null;
        }


        public static bool IsEvent(this IMethodSymbol method) =>
            //method.Kind == SymbolKind.Event;
            method.Name.StartsWith("add_") || method.Name.StartsWith("remove_");


        public static bool IsProperty(this IMethodSymbol method) =>
            //method.Kind == SymbolKind.Property;
            method.Name.StartsWith("get_") || method.Name.StartsWith("set_");


        public static string GetName(this IPropertySymbol property) => property
            .Name
            .Replace("get_", String.Empty)
            .Replace("set_", String.Empty);


        public static string BuildArgString(this IMethodSymbol method, bool includeTypes)
        {
            var s = "";
            foreach (var parameter in method.Parameters)
            {
                if (includeTypes)
                    s += $"{parameter.Type.ToDisplayString()} {parameter.Name}, ";
                else
                    s += $"{parameter.Name}, ";
            }
            s = s.TrimEnd(',', ' ');
            return s;
        }


        public static IEnumerable<IMethodSymbol> GetAllPublicMethods(this ITypeSymbol type)
        {
            if (type.IsInterface())
            {
                var methods = type
                    .GetAllInterfacesIncludingThis()
                    .SelectMany(x => x
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(y =>
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
                        .Where(x => x.DeclaredAccessibility == Accessibility.Public)
                        .Where(x => !x.IsEvent() && !x.IsProperty())
                        //.Where(x => !x.IsConstructorOrDestructor())
                        .Where(x => x.Kind == SymbolKind.Method)
                        .Where(x => !x.IsAbstract)
                        .ToList();

                    foreach (var method in methods)
                    {
                        yield return method;
                    }

                    //if (!visitHierarchy)
                    //{
                    //    break;
                    //}

                    currentType = currentType.BaseType;
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


        public static void CreateClass(this IndentedStringBuilder builder, Action build, string nameSpace, string className, params string[] inherit)
        {
            using (builder.BlockInvariant("namespace " + nameSpace))
            {
                var cls = "public partial class " + className;

                if (inherit.Length > 0)
                {
                    cls += " :";
                    foreach (var i in inherit)
                        cls += $" {i},";

                    cls = cls.TrimEnd(',');
                }
                using (builder.BlockInvariant(cls))
                    build();
            }
        }


        public static IEnumerable<INamedTypeSymbol> GetAllInterfaceTypes(this SourceGeneratorContext context) => context
            .Compilation
            .SyntaxTrees
            .Select(x => context.Compilation.GetSemanticModel(x))
            .SelectMany(x => x
                .SyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .Select(y => y.GetDeclaredSymbol(x))
            )
            .OfType<INamedTypeSymbol>();


        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType<T>(this SourceGeneratorContext context)
            => context.GetAllImplementationsOfType(typeof(T));


        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this SourceGeneratorContext context, Type type)
            => context.GetAllImplementationsOfType(type.FullName);


        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this SourceGeneratorContext context, string fullName)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(fullName);
            if (symbol == null)
                return Enumerable.Empty<INamedTypeSymbol>();

            return SymbolFinder
                .FindImplementationsAsync(symbol, context.Project.Solution)
                .Result
                .OfType<INamedTypeSymbol>();
        }


        public static IEnumerable<INamedTypeSymbol> GetAllDerivedClassesForType(this SourceGeneratorContext context, string typeName)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(typeName);
            if (symbol == null)
                return Enumerable.Empty<INamedTypeSymbol>();

            var result = SymbolFinder.FindDerivedClassesAsync(symbol, context.Project.Solution).Result;
            return result;
        }


        public static INamedTypeSymbol? GetShinyStartupSymbol(this SourceGeneratorContext context)
        {
            var log = context.GetLogger();

            var startupClasses = context
                .GetAllImplementationsOfType<IShinyStartup>()
                .WhereNotSystem()
                .ToList();

            INamedTypeSymbol? startupClass = null;
            switch (startupClasses.Count)
            {
                case 0:
                    log.Warn("No Shiny Startup implementation found");
                    break;

                case 1:
                    startupClass = startupClasses.First();
                    break;

                default:
                    log.Warn(startupClasses.Count + " Shiny Startup implementations found");
                    break;
            }
            return startupClass;
        }


        public static IEnumerable<INamedTypeSymbol> WhereNotInAssembly(this IEnumerable<INamedTypeSymbol> en, params string[] names)
            => en.Where(x => !names.Any(y => x.ContainingAssembly.Name.StartsWith(y, StringComparison.OrdinalIgnoreCase)));

        public static IEnumerable<INamedTypeSymbol> WhereNotSystem(this IEnumerable<INamedTypeSymbol> en)
            => en.WhereNotInAssembly("Xamarin.", "Shiny.");
    }
}

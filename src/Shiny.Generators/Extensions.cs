using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;


namespace Shiny.Generators
{
    static class Extensions
    {
        // this is only if the partial has a 2+ partials
        //public static bool IsPartialClass(this INamedTypeSymbol symbol) =>
        //    symbol.Locations.Length > 1 || symbol.DeclaringSyntaxReferences.Length > 1;
        public static string Indent(this string value, int level)
        {
            var newValue = "";
            for (var i = 0; i < level; i++)
                newValue += "\t";

            newValue += value;
            return newValue;
        }

        //public static bool IsIosAppProject(this GeneratorExecutionContext context)
        //    => context.IsProjectType("FEACFBD2-3405-455C-9665-78FE426C6842");
        ////{FEACFBD2-3405-455C-9665-78FE426C6842};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}


        //public static bool IsAndroidAppProject(this GeneratorExecutionContext context)
        //    => context.IsProjectType("EFBA0AD7-5A72-4C68-AF49-83D382785DCF");
        //{EFBA0AD7-5A72-4C68-AF49-83D382785DCF};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}


        //public static bool IsProjectType(this GeneratorExecutionContext context, string projectTypeGuid)
        //{
        //    var guids = context.GetProjectInstance().GetPropertyValue("ProjectTypeGuids")?.ToUpper();
        //    if (guids == null)
        //        return false;

        //    var result = guids.Contains(projectTypeGuid);
        //    return result;
        //}


        //public static int GetAndroidMajorTarget(this GeneratorExecutionContext context)
        //{
        //    var target = context.GetProjectInstance().GetPropertyValue("TargetFrameworkVersion");
        //    if (target == null)
        //        return -1;

        //    target = target.Replace("v", String.Empty);
        //    var len = target.IndexOf(".");
        //    var majorString = len > 0
        //        ? target.Substring(0, len)
        //        : target;

        //    var v = Int32.Parse(majorString);
        //    return v;
        //}


        public static bool HasMethod(this INamedTypeSymbol symbol, string methodName)
        {
            var members = symbol.GetMembers();
            var exists = members
                .OfType<IMethodSymbol>()
                .Any(x => x.Name.Equals(methodName));
            return exists;
        }


        public static bool IsEqualToType(this GeneratorExecutionContext context, ITypeSymbol symbol, string otherTypeName)
        {
            var type = context.Compilation.GetTypeByMetadataName(otherTypeName);
            //var result = symbol.EqualsType(type);
            var result = true;
            return result;
        }


        public static bool IsStream(this GeneratorExecutionContext context, ITypeSymbol symbol)
            => context.IsEqualToType(symbol, typeof(System.IO.Stream).FullName);


        public static bool IsObservable(this GeneratorExecutionContext context, ITypeSymbol symbol)
            => context.IsEqualToType(symbol, "System.IObservable`1");


        public static bool IsGenericAsyncTask(this GeneratorExecutionContext context, ITypeSymbol symbol)
            => context.IsEqualToType(symbol, "System.Threading.Tasks.Task`1");


        public static bool IsAsyncTask(this GeneratorExecutionContext context, ITypeSymbol type)
        {
            var task = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var result = type.Equals(task);
            return result;
        }


        public static bool HasXamarinForms(this GeneratorExecutionContext context)
            => context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Application") != null;


        public static bool HasXamarinEssentials(this GeneratorExecutionContext context)
            => context.Compilation.GetTypeByMetadataName("Xamarin.Essentials.Platform") != null;


        public static bool HasAssemblyAttribute(this GeneratorExecutionContext context, string attributeName)
        {
            var attribute = context.Compilation.GetTypeByMetadataName(attributeName);
            return context.Compilation.Assembly.FindAttributeFlattened(attribute) != null;
        }


        public static bool IsEvent(this IMethodSymbol method) =>
            method.Name.StartsWith("add_") || method.Name.StartsWith("remove_");


        public static bool IsProperty(this IMethodSymbol method) =>
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


        public static bool Is(this INamedTypeSymbol symbol, string typeName)
        {
            while (symbol != null && symbol.Name != "Object")
            {
                if (symbol.ToDisplayString() == typeName)
                    return true;

                symbol = symbol.BaseType;
            }
            return false;
        }


        public static AttributeData FindAttributeFlattened(this ISymbol symbol, INamedTypeSymbol attributeClassSymbol)
        {
            var attrs = symbol.GetAllAttributes();
            foreach (var attr in attrs)
            {
                if (attr.AttributeClass == attributeClassSymbol) // TODO: this is failing
                    return attr;
            }
            return null;
        }


        public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol symbol)
        {
            while (symbol != null)
            {
                var attributes = symbol.GetAttributes();
                foreach (var attribute in attributes)
                    yield return attribute;

                symbol = (symbol as INamedTypeSymbol)?.BaseType;
            }
        }


        public static bool IsInterface(this ITypeSymbol type)
            => type.TypeKind == TypeKind.Interface;



        public static IEnumerable<IMethodSymbol> GetAllPublicMethods(this ITypeSymbol type)
        {
            if (type.IsInterface())
            {
                var methods = type
                    .GetAllInterfaces(true)
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
                        .Where(x => x.Kind == SymbolKind.Method)
                        .Where(x => !x.IsAbstract)
                        .ToList();

                    foreach (var method in methods)
                        yield return method;

                    currentType = currentType.BaseType;
                }
            }
        }


        public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol symbol, bool includeCurrent = true)
        {
            if (includeCurrent && symbol.TypeKind == TypeKind.Interface)
                yield return (INamedTypeSymbol)symbol;

            while (symbol != null && symbol.Name != "Object")
            {
                foreach (var intf in symbol.Interfaces)
                {
                    yield return intf;
                    foreach (var innerInterface in intf.GetAllInterfaces())
                        yield return innerInterface;
                }

                symbol = symbol.BaseType;
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


        //public static IEnumerable<INamedTypeSymbol> GetAllInterfaceTypes(this GeneratorExecutionContext context) => context
        //    .Compilation
        //    .SyntaxTrees
        //    .Select(x => context.Compilation.GetSemanticModel(x))
        //    .SelectMany(x => x
        //        .SyntaxTree
        //        .GetRoot()
        //        .DescendantNodes()
        //        .OfType<InterfaceDeclarationSyntax>()
        //        //.Select(y => y.GetDeclaredSymbol(x))
        //    )
        //    .OfType<INamedTypeSymbol>();


        //public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this GeneratorExecutionContext context, Type type, bool thisProjectOnly = false)
        //    => context.GetAllImplementationsOfType(type.FullName, thisProjectOnly);



        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this IShinyContext context, string fullName, bool thisProjectOnly = false)
        {
            var symbol = context.Context.Compilation.GetTypeByMetadataName(fullName);
            if (symbol == null)
                return Enumerable.Empty<INamedTypeSymbol>();

            return context.GetAllImplementationsOfType(symbol, thisProjectOnly);
        }


        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this IShinyContext context, ISymbol symbol, bool thisProjectOnly = false)
        {
            if (!thisProjectOnly)
            {
                return SymbolFinder
                    .FindImplementationsAsync(symbol, context.CurrentDocument.Project.Solution)
                    .Result
                    .OfType<INamedTypeSymbol>();
            }
            return SymbolFinder
                .FindImplementationsAsync(symbol, context.CurrentDocument.Project.Solution, ImmutableSortedSet.Create(context.CurrentDocument.Project))
                .Result
                .OfType<INamedTypeSymbol>();
        }


        public static IEnumerable<INamedTypeSymbol> GetAllDerivedClassesForType(this IShinyContext context, string typeName, bool thisProjectOnly = false)
        {
            var symbol = context.Context.Compilation.GetTypeByMetadataName(typeName);
            if (symbol == null)
                return Enumerable.Empty<INamedTypeSymbol>();

            if (!thisProjectOnly)
                return SymbolFinder.FindDerivedClassesAsync(symbol, context.CurrentDocument.Project.Solution, true).Result;

            return SymbolFinder.FindDerivedClassesAsync(symbol, context.CurrentDocument.Project.Solution, true, ImmutableSortedSet.Create(context.CurrentDocument.Project)).Result;
        }


        public static IEnumerable<INamedTypeSymbol> WhereNotInAssembly(this IEnumerable<INamedTypeSymbol> en, params string[] names)
            => en.Where(x => !names.Any(y => x.ContainingAssembly.ToDisplayString().StartsWith(y, StringComparison.InvariantCultureIgnoreCase)));


        public static IEnumerable<INamedTypeSymbol> WhereNotNamespace(this IEnumerable<INamedTypeSymbol> en, params string[] names)
            => en.Where(x => !names.Any(y => x.ContainingNamespace.ToDisplayString().StartsWith(y, StringComparison.InvariantCultureIgnoreCase)));


        public static IEnumerable<INamedTypeSymbol> WhereNotSystem(this IEnumerable<INamedTypeSymbol> en)
            => en.WhereNotInAssembly("Xamarin.", "Shiny.").WhereNotNamespace("Android.");
    }
}

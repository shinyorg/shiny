using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    static class Extensions
    {
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
            var attributeData = assemblyAttributes
                .Where(x => x.AttributeClass.Name == attribute.Name)
                .FirstOrDefault();

            // TODO: why is the constructor arg not coming in?
            return attributeData;
        }


        public static bool CurrentAssemblyHasAttribute(this GeneratorExecutionContext context, string attributeTypeName)
            => context.GetCurrentAssemblyAttribute(attributeTypeName) != null;


        public static bool IsEvent(this IMethodSymbol method) => method.Kind == SymbolKind.Event;
        public static bool IsProperty(this IMethodSymbol method) => method.Kind == SymbolKind.Property;
        public static bool IsPublic(this ITypeSymbol symbol)
            => symbol.DeclaredAccessibility == Accessibility.Public;
        public static bool IsPublic(this IMethodSymbol symbol)
            => symbol.DeclaredAccessibility == Accessibility.Public;
        public static bool IsInterface(this ITypeSymbol type)
            => type.TypeKind == TypeKind.Interface;

        public static string GetName(this IPropertySymbol property) => property
            .Name
            .Replace("get_", String.Empty)
            .Replace("set_", String.Empty);


        public static bool Implements(this INamedTypeSymbol symbol, ITypeSymbol type)
            => symbol.AllInterfaces.Any(i => type.Equals(i));


        public static bool Inherits(this INamedTypeSymbol symbol, ITypeSymbol type)
        {
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


        public static IEnumerable<IPropertySymbol> GetAllProperties(this INamedTypeSymbol symbol)
        {
            while (symbol != null && symbol.Name != "Object")
            {
                var properties = symbol.GetMembers().Where(x => x.Kind == SymbolKind.Property);
                foreach (var property in properties)
                    yield return (IPropertySymbol)property;

                symbol = symbol?.BaseType;
            }
        }


        public static IEnumerable<IMethodSymbol> GetAllPublicMethods(this ITypeSymbol type)
        {
            if (type.IsInterface())
            {
                var methods = type
                    .AllInterfaces
                    .SelectMany(x => x
                        .GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(y =>
                            x.IsPublic() &&
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
                            x.IsAbstract &&
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


        //public static void CreateClass(this IndentedStringBuilder builder, Action build, string nameSpace, string className, params string[] inherit)
        //{
        //    using (builder.BlockInvariant("namespace " + nameSpace))
        //    {
        //        var cls = "public partial class " + className;

        //        if (inherit.Length > 0)
        //        {
        //            cls += " :";
        //            foreach (var i in inherit)
        //                cls += $" {i},";

        //            cls = cls.TrimEnd(',');
        //        }
        //        using (builder.BlockInvariant(cls))
        //            build();
        //    }
        //}


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



        //public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this IShinyContext context, string fullName, bool thisProjectOnly = false)
        //{
        //    var symbol = context.Context.Compilation.GetTypeByMetadataName(fullName);
        //    if (symbol == null)
        //        return Enumerable.Empty<INamedTypeSymbol>();

        //    return context.GetAllImplementationsOfType(symbol, thisProjectOnly);
        //}


        //public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType(this IShinyContext context, ISymbol symbol, bool thisProjectOnly = false)
        //{
        //    if (!thisProjectOnly)
        //    {
        //        return SymbolFinder
        //            .FindImplementationsAsync(symbol, context.CurrentDocument.Project.Solution)
        //            .Result
        //            .OfType<INamedTypeSymbol>();
        //    }
        //    return SymbolFinder
        //        .FindImplementationsAsync(symbol, context.CurrentDocument.Project.Solution, ImmutableSortedSet.Create(context.CurrentDocument.Project))
        //        .Result
        //        .OfType<INamedTypeSymbol>();
        //}


        //public static IEnumerable<INamedTypeSymbol> GetAllDerivedClassesForType(this IShinyContext context, string typeName, bool thisProjectOnly = false)
        //{
        //    var symbol = context.Context.Compilation.GetTypeByMetadataName(typeName);
        //    if (symbol == null)
        //        return Enumerable.Empty<INamedTypeSymbol>();

        //    if (!thisProjectOnly)
        //        return SymbolFinder.FindDerivedClassesAsync(symbol, context.CurrentDocument.Project.Solution, true).Result;

        //    return SymbolFinder.FindDerivedClassesAsync(symbol, context.CurrentDocument.Project.Solution, true, ImmutableSortedSet.Create(context.CurrentDocument.Project)).Result;
        //}


        //public static IEnumerable<INamedTypeSymbol> WhereNotInAssembly(this IEnumerable<INamedTypeSymbol> en, params string[] names)
        //    => en.Where(x => !names.Any(y => x.ContainingAssembly.ToDisplayString().StartsWith(y, StringComparison.InvariantCultureIgnoreCase)));


        //public static IEnumerable<INamedTypeSymbol> WhereNotNamespace(this IEnumerable<INamedTypeSymbol> en, params string[] names)
        //    => en.Where(x => !names.Any(y => x.ContainingNamespace.ToDisplayString().StartsWith(y, StringComparison.InvariantCultureIgnoreCase)));


        //public static IEnumerable<INamedTypeSymbol> WhereNotSystem(this IEnumerable<INamedTypeSymbol> en)
        //    => en.WhereNotInAssembly("Xamarin.", "Shiny.").WhereNotNamespace("Android.");

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
    }
}

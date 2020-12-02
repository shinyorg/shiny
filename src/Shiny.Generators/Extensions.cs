using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    static class Extensions
    {
        public static void Source(this GeneratorExecutionContext context, string sourceText, string? fileName = null)
        {
            fileName ??= Guid.NewGuid().ToString();
            context.AddSource(fileName, sourceText);
        }


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


        public static bool HasXamarinEssentials(this GeneratorExecutionContext context)
            => context.Compilation.GetTypeByMetadataName("Xamarin.Essentials.AppInfo") != null;


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
    }
}

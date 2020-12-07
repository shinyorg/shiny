using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Shiny.Generators
{
    public static class Extensions
    {
        public static void Source(this GeneratorExecutionContext context, string sourceText, string? fileName = null)
        {
            fileName ??= Guid.NewGuid().ToString();
            context.AddSource(fileName, sourceText);
        }


        public static bool IsPartialClass(this ISymbol symbol) => symbol
            .DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(CancellationToken.None))
            .OfType<ClassDeclarationSyntax>()
            .Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));


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


        public static IEnumerable<IAssemblySymbol> GetAllAssemblies(this GeneratorExecutionContext context)
        {
            yield return context.Compilation.Assembly;
            foreach (var reference in context.Compilation.References)
                if (reference.Properties.Kind == MetadataImageKind.Assembly)
                    yield return (IAssemblySymbol)context.Compilation.GetAssemblyOrModuleSymbol(reference);
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
        {
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


        //public static bool Is(this INamedTypeSymbol symbol, string typeName)
        //{
        //    while (symbol != null && symbol.Name != "Object")
        //    {
        //        if (symbol.ToDisplayString() == typeName)
        //            return true;

        //        symbol = symbol?.BaseType;
        //    }

        //    return false;
        //}


        ///// <summary>
        ///// Returns the element type of the IEnumerable, if any.
        ///// </summary>
        ///// <param name="resolvedType"></param>
        ///// <returns></returns>
        //public static ITypeSymbol EnumerableOf(this ITypeSymbol resolvedType)
        //{
        //    var intf = resolvedType
        //        .GetAllInterfaces(includeCurrent: true)
        //        .FirstOrDefault(i => i.ToDisplayString().StartsWith("System.Collections.Generic.IEnumerable", StringComparison.OrdinalIgnoreCase));

        //    return intf?.TypeArguments.First();
        //}

        //public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol symbol, bool includeCurrent = true)
        //{
        //    if (symbol != null)
        //    {
        //        if (includeCurrent && symbol.TypeKind == TypeKind.Interface)
        //        {
        //            yield return (INamedTypeSymbol)symbol;
        //        }

        //        do
        //        {
        //            foreach (var intf in symbol.Interfaces)
        //            {
        //                yield return intf;

        //                foreach (var innerInterface in intf.GetAllInterfaces())
        //                {
        //                    yield return innerInterface;
        //                }
        //            }

        //            symbol = symbol.BaseType;

        //            if (symbol == null)
        //            {
        //                break;
        //            }

        //        } while (symbol.Name != "Object");
        //    }
        //}

        //public static bool IsNullable(this ITypeSymbol type)
        //{
        //    return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
        //        && type.OriginalDefinition.ToDisplayString().Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
        //}

        //public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol nullableType)
        //{
        //    if (type.IsNullable())
        //    {
        //        nullableType = ((INamedTypeSymbol)type).TypeArguments.First();
        //        return true;
        //    }
        //    else
        //    {
        //        nullableType = null;
        //        return false;
        //    }
        //}

        //public static ITypeSymbol NullableOf(this ITypeSymbol type)
        //{
        //    return type.IsNullable()
        //        ? ((INamedTypeSymbol)type).TypeArguments.First()
        //        : null;
        //}

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

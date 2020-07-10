using System;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    internal static class Extensions
    {
        public static bool HasAssemblyAttribute(this SourceGeneratorContext context, string attributeName) 
        {
            var attribute = context.Compilation.GetTypeByMetadataName(attributeName);
            return context.Compilation.Assembly.FindAttributeFlattened(attribute) != null;
        }


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

        public static void RegisterIf(this SourceGeneratorContext context, IndentedStringBuilder builder, string typeNameExists, string registerString)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(typeNameExists);
            if (symbol != null)
            {
                context.GetLogger().Info("Registering in Shiny Startup - " + registerString);
                builder.AppendLineInvariant(registerString);
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
    }
}

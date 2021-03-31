using System;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class StaticClassSourceGenerator : ISourceGenerator
    {
        const string ShinyStaticGenerationAttribute = "Shiny.Attributes.StaticGenerationAttribute";
        GeneratorExecutionContext context;
        string useNamespace;


        public void Initialize(GeneratorInitializationContext context) { }
        public void Execute(GeneratorExecutionContext context)
        {
            context.TryDebug();
            this.context = context;
            var attribute = context.GetCurrentAssemblyAttribute("Shiny.GenerateStaticClassesAttribute");
            if (attribute == null)
                return;

            this.context.Log("SHINYINFO", "Shiny static class generation will run on this assembly", DiagnosticSeverity.Info);
            this.useNamespace = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (String.IsNullOrWhiteSpace(this.useNamespace))
                throw new ArgumentException("Namespace not supplied for static generation");

            this.context.Log(
                "SHINYINFO",
                "Shiny static class generation namespace: " + this.useNamespace,
                DiagnosticSeverity.Info
            );

            var shinyAssemblies = this.context
                .GetAllAssemblies()
                .Where(x => x
                    .ToDisplayString()
                    .StartsWith("Shiny.")
                )
                .ToList();

            foreach (var ass in shinyAssemblies)
            {
                var staticAttributes = ass
                    .GetAttributes()
                    .Where(x =>
                    {
                        var other = x.AttributeClass.ToDisplayString();
                        return ShinyStaticGenerationAttribute.Equals(other);
                    })
                    .ToList();

                foreach (var attr in staticAttributes)
                {
                    var typeName = (string)attr.ConstructorArguments[0].Value;
                    var genFileName = (string)attr.ConstructorArguments[1].Value;
                    this.BuildStaticClass(typeName, genFileName);
                }
            }
        }


        void BuildStaticClass(string ifTypeName, string genFileName)
        {
            var type = this.context.Compilation.GetTypeByMetadataName(ifTypeName);
            if (type == null)
                return;

            var builder = new IndentedStringBuilder();
            using (builder.BlockInvariant("namespace " + this.useNamespace))
            {
                using (builder.BlockInvariant("public static partial class " + genFileName))
                {
                    builder.AppendLine($"public static {ifTypeName} Current => Shiny.ShinyHost.Resolve<{ifTypeName}>();");
                    builder.AppendLine();

                    AppendMethods(type, builder);
                    AppendProperties(type, builder);
                }
            }
            this.context.AddSource(genFileName, builder.ToString());
        }


        static void AppendMethods(INamedTypeSymbol type, IndentedStringBuilder builder)
        {
            var methods = type.GetAllPublicMethods();

            foreach (var method in methods)
            {
                var argList = BuildArgString(method, true);
                var argListNoNames = BuildArgString(method, false);

                var returnType = method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString();
                var signature = $"public static {returnType} {method.Name}";
                var args = $"Current.{method.Name}";
                var constraint = "";

                if (method.IsGenericMethod && method.TypeParameters.Length == 1)
                {
                    signature += "<T>";
                    args += "<T>";

                    var p = method.TypeParameters.First();
                    for (var i = 0; i < p.ConstraintTypes.Length; i++)
                    {
                        if (i > 0)
                            constraint += ",";

                        constraint += p.ConstraintTypes[i].ToDisplayString();
                    }

                    if (p.HasConstructorConstraint)
                        constraint += ",new()";

                    if (!String.IsNullOrWhiteSpace(constraint))
                        constraint = " where T: " + constraint;
                }

                builder.AppendLineInvariant($"{signature}({argList}){constraint} => {args}({argListNoNames});");
            }
        }


        static void AppendProperties(INamedTypeSymbol type, IndentedStringBuilder builder)
        {
            foreach (var prop in type.GetAllProperties())
            {
                var propertyName = prop.GetName();
                var hasGet = prop.GetMethod?.IsPublic() ?? false;
                var hasSet = prop.SetMethod?.IsPublic() ?? false;

                if (hasGet && !hasSet)
                {
                    builder.AppendLineInvariant($"public static {prop.Type.ToDisplayString()} {propertyName} => Current.{propertyName};");
                }
                else
                {
                    using (builder.BlockInvariant($"public static {prop.Type.ToDisplayString()} {propertyName}"))
                    {
                        if (hasGet)
                            builder.AppendLineInvariant($"get => Current.{propertyName};");

                        if (hasSet)
                            builder.AppendLineInvariant($"set => Current.{propertyName} = value;");
                    }
                }
            }
        }


        static string BuildArgString(IMethodSymbol method, bool includeTypes)
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
    }
}

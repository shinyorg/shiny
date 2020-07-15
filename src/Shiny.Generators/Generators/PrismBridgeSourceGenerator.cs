using System;
using System.Linq;

using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators
{
    public static class PrismBridgeSourceGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            var log = context.GetLogger();
            var apps = context.GetAllDerivedClassesForType("Prism.DryIoc.PrismApplication");

            switch (apps.Count())
            {
                case 0:
                    log.Info("Prism.DryIoc.PrismApplication not found");
                    break;

                case 1:
                    var app = apps.First();
                    var builder = new IndentedStringBuilder();
                    builder.AppendNamespaces("Prism.Ioc", "Prism.Mvvm", "Prism.DryIoc", "DryIoc");

                    using (builder.BlockInvariant("namespace " + app.ContainingNamespace.Name))
                    { 
                        using (builder.BlockInvariant("public partial class " + app.Name))
                        { 
                            using (builder.BlockInvariant("protected override IContainerExtension CreateContainerExtension()"))
                            {
                                builder.AppendLineInvariant("var container = new Container(this.CreateContainerRules());");
                                builder.Append(@"
Shiny.ShinyHost.Populate((serviceType, func, lifetime) =>
{
    container.RegisterDelegate(
        serviceType,
        _ => func(),
        Reuse.Singleton // HACK: I know everything is singleton
    );
});");
                                if (!AutoStartupSourceGenerator.IsGenerated)
                                {
                                    builder.AppendLineInvariant("Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => ShinyHost.Container.GetService(t));");
                                    builder.AppendLine();
                                }
                                builder.AppendLineInvariant("return new DryIocContainerExtension(container);");
                                builder.AppendLine();
                            }
                        }
                    }
                    context.AddCompilationUnit(app.Name, builder.ToString());
                    break;

                default:
                    log.Warn("More than 1 PrismApplication found");
                    break;
            }
        }
    }
}

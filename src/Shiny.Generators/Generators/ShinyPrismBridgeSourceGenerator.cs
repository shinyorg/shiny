using System;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators
{
    public static class ShinyPrismBridgeSourceGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            // TODO: find the XF/Prism app container
            context.Compilation.GetTypeByMetadataName("Prism.DryIoc.PrimApplication");
            // TODO: find if anything inherits it, if so, make sure it is partial and stub in 

            //protected override IContainerExtension CreateContainerExtension()
            //{
            //    var container = new Container(this.CreateContainerRules());
            //    ShinyHost.Populate((serviceType, func, lifetime) =>
            //        container.RegisterDelegate(
            //            serviceType,
            //            _ => func(),
            //            Reuse.Singleton // HACK: I know everything is singleton
            //        )
            //    );
            //    return new DryIocContainerExtension(container);
            //}
    }
    }
}

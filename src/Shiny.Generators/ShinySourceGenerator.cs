using System;
using Uno.SourceGeneration;
using Shiny.Generators.Generators;
using Shiny.Generators.Generators.iOS;
using Shiny.Generators.Generators.Android;


namespace Shiny.Generators
{
    public class ShinySourceGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            AutoStartupSourceGenerator.Execute(context);
            StaticClassSourceGenerator.Execute(context);
            AppDelegateBoilerplateGenerator.Execute(context);
            ApplicationSourceGenerator.Execute(context);
            ActivitySourceGenerator.Execute(context);

            BleClientSourceGenerator.Execute(context);
        }
    }
}

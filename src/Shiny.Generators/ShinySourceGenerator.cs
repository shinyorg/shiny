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
            // shiny
            AutoStartupSourceGenerator.Execute(context);
            StaticClassSourceGenerator.Execute(context);
            PrismBridgeSourceGenerator.Execute(context);

            // ios
            AppDelegateBoilerplateGenerator.Execute(context);

            // android
            ApplicationSourceGenerator.Execute(context);
            ActivitySourceGenerator.Execute(context);

            // ble
            BleClientSourceGenerator.Execute(context);
        }
    }
}

using System;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    public class MainActivityBoilerplateGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            var project = context.GetProjectInstance();
            var shinyStartupSymbol = context.Compilation.GetTypeByMetadataName(typeof(IShinyStartupTask).FullName);

            if (shinyStartupSymbol is null)
                return;
        }
    }
}

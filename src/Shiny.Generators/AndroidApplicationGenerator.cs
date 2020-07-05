using System;

using Uno.SourceGeneration;

namespace Shiny.Generators
{
    public class AndroidApplicationGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            var shinyStartupSymbol = context.Compilation.GetTypeByMetadataName(typeof(IShinyStartupTask).FullName);
            if (shinyStartupSymbol is null)
                return;

            // if application exists, error or override?

        }
    }
}

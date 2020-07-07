using System;

using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Shiny.Generators
{
    internal static class Extensions
    {
        public static void RegisterIf(this SourceGeneratorContext context, IndentedStringBuilder builder, string typeNameExists, string registerString)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(typeNameExists);
            if (symbol != null)
                builder.AppendLineInvariant(registerString);
        }
    }
}

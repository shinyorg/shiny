using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class UwpSourceGenerator : ShinyApplicationSourceGenerator
    {
        public UwpSourceGenerator() : base("Windows.UI.Xaml.Application")
        {
        }


        protected override void Process(IEnumerable<INamedTypeSymbol> osAppTypeSymbols)
        {
            // nothing to do on UWP at this time
        }
    }
}

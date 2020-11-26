using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public class ShinySymbolVisitor : SymbolVisitor
    {
        public List<INamedTypeSymbol> TypeSymbols { get; } = new List<INamedTypeSymbol>();
        public List<IAssemblySymbol> ShinyAssemblies { get; } = new List<IAssemblySymbol>();
        public bool HasXamarinForms { get; private set; }
        public bool HasXamarinEssentials { get; private set; }


        public override void VisitAssembly(IAssemblySymbol symbol)
        {
            base.VisitAssembly(symbol);

            var s = symbol.ToDisplayString();
            if (s.StartsWith("Shiny."))
            {
                this.ShinyAssemblies.Add(symbol);
            }
            else
            {
                switch (s)
                {
                    case "Xamarin.Forms":
                        this.HasXamarinForms = true;
                        break;

                    case "Xamarin.Essentials":
                        this.HasXamarinEssentials = true;
                        break;
                }
            }
    //        this.shinyAssemblies = context
    //.Compilation
    //.References
    //.Where(x =>
    //    x.Display != null &&
    //    x.Properties.Kind == MetadataImageKind.Assembly &&
    //    (
    //        Regex.IsMatch(x.Display, "Shiny.(.*).dll") ||
    //        x.Display.EndsWith("Xamarin.Forms.dll")
    //    )
    //)
    //.Select(context.Compilation.GetAssemblyOrModuleSymbol)
    //.OfType<IAssemblySymbol>()
    //.ToArray();
        }


        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            base.VisitNamedType(symbol);

            // TODO: filter out shiny, xamarin, bcl, and prism?
            if (symbol.IsPublic() && !symbol.IsAbstract)
                this.TypeSymbols.Add(symbol);
        }
    }
}

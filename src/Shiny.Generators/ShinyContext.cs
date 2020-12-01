using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public interface IShinyContext
    {
        GeneratorExecutionContext Context { get; }
        INamedTypeSymbol? GetShinyType(string fullyQualifiedMetadataName);
    }


    public class ShinyContext : IShinyContext
    {
        readonly Lazy<IAssemblySymbol[]> shinyAssemblies;


        public ShinyContext(GeneratorExecutionContext context)
        {
            this.Context = context;
            this.shinyAssemblies = new Lazy<IAssemblySymbol[]>(() => context
                .Compilation
                .References
                .Where(x =>
                    x.Display != null &&
                    x.Properties.Kind == MetadataImageKind.Assembly &&
                    Regex.IsMatch(x.Display, "Shiny.(.*).dll")
                )
                .Select(context.Compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .ToArray()
            );
        }


        public GeneratorExecutionContext Context { get; private set; }


        public INamedTypeSymbol? GetShinyType(string fullyQualifiedMetadataName) => this.shinyAssemblies
            .Value
            .Select(x => x.GetTypeByMetadataName(fullyQualifiedMetadataName))
            .FirstOrDefault();
    }
}

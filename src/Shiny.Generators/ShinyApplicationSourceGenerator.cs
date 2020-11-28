using System;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public abstract class ShinyApplicationSourceGenerator : ISourceGenerator
    {
        readonly string osApplicationTypeName;
        protected ShinyApplicationSourceGenerator(string osApplicationTypeName) => this.osApplicationTypeName = osApplicationTypeName;
        protected IShinyContext? Context { get; private set; }
        protected void AddSource(string sourceText, string? fileName = null) => this.Context.Context.Source(sourceText, fileName);


        public virtual void Execute(GeneratorExecutionContext context)
        {
            var shinyAppAttribute = context.GetCurrentAssemblyAttribute(Constants.ShinyApplicationAttributeTypeName);
            if (shinyAppAttribute == null)
                return;

            var appType = context.Compilation.GetTypeByMetadataName(this.osApplicationTypeName);
            if (appType == null)
                return;

            var appClasses = context
                .Compilation
                .Assembly
                .GetAllTypeSymbols()
                .Where(x => x.Inherits(appType))
                .ToList();

            this.Context = new ShinyContext(context);
            foreach (var appClass in appClasses)
                this.Process(appClass);
        }


        public virtual void Initialize(GeneratorInitializationContext context) { }


        protected abstract void Process(INamedTypeSymbol osAppTypeSymbol);
    }
}

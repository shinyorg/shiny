using System;
using System.Linq;

using Microsoft.CodeAnalysis;

using Uno.SourceGeneration;


namespace Shiny.Generators
{
    public interface IShinyContext
    {
        SourceGeneratorContext Context { get; }
        ISourceGeneratorLogger Log { get; }
        bool IsStartupGenerated { get; set; }
        string GetRootNamespace();
        string GetShinyStartupClassFullName();
    }


    public class ShinyContext : IShinyContext
    {
        public ShinyContext(SourceGeneratorContext context)
        {
            this.Context = context;
            this.Log = context.GetLogger();
        }


        public string GetShinyStartupClassFullName()
        {
            if (this.IsStartupGenerated)
            {
                var ns = this.GetRootNamespace();
                return $"{ns}.AppShinyStartup";
            }
            var startupClasses = this
                .Context
                .GetAllImplementationsOfType("Shiny.IShinyStartup")
                .WhereNotSystem()
                .ToList();

            INamedTypeSymbol? startupClass = null;
            switch (startupClasses.Count)
            {
                case 0:
                    this.Log.Warn("No Shiny Startup implementation found");
                    break;

                case 1:
                    startupClass = startupClasses.First();
                    break;

                default:
                    this.Log.Warn(startupClasses.Count + " Shiny Startup implementations found");
                    break;
            }
            return startupClass?.ToDisplayString();
        }


        public SourceGeneratorContext Context { get; private set; }
        public ISourceGeneratorLogger Log { get; private set; }
        public bool IsStartupGenerated { get; set; }
        public string GetRootNamespace() => this.Context.GetProjectInstance().GetPropertyValue("RootNamespace");
    }
}

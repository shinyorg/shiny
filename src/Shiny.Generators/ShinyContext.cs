using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    public interface IShinyContext
    {
        SourceGeneratorContext Context { get; }
        ISourceGeneratorLogger Log { get; }
        bool IsStartupGenerated { get; set; }
        string GetRootNamespace();
        string? GetShinyStartupClassFullName();
        string? GetXamFormsAppClassFullName();
    }


    public class ShinyContext : IShinyContext
    {
        public ShinyContext(SourceGeneratorContext context)
        {
            this.Context = context;
            this.Log = context.GetLogger();
        }


        public string? GetXamFormsAppClassFullName()
        {
            var classes = this
                .Context
                .GetAllDerivedClassesForType("Xamarin.Forms.Application")
                .WhereNotSystem()
                .ToList();

            INamedTypeSymbol? appClass = null;
            switch (classes.Count)
            {
                case 0:
                    this.Log.Warn("No Xamarin Forms App implementations found");
                    break;

                case 1:
                    appClass = classes.First();
                    break;

                default:
                    this.Log.Warn(classes.Count + " Xamarin Forms App implementations found");
                    foreach (var cls in classes)
                        this.Log.Warn(" - " + cls.ToDisplayString());
                    break;
            }
            return appClass?.ToDisplayString();
        }


        public string? GetShinyStartupClassFullName()
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
                    foreach (var sc in startupClasses)
                        this.Log.Warn(" - " + sc.ToDisplayString());
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

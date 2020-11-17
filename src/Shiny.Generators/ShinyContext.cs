using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public interface IShinyContext
    {
        GeneratorExecutionContext Context { get; }
        bool IsStartupGenerated { get; set; }
        Document CurrentDocument { get; }
        string GetRootNamespace();
        string? GetShinyStartupClassFullName();
        string? GetXamFormsAppClassFullName();
        bool IsProjectType(string projectTypeGuid);
    }


    public class ShinyContext : IShinyContext
    {
        readonly Lazy<object> msbuildLazy;


        public ShinyContext(GeneratorExecutionContext context)
        {
            this.Context = context;
            this.msbuildLazy = new Lazy<object>(() =>
            {
                //var workspace = MSBuildWorkspace.Create();
                //using (var xmlReader = XmlReader.Create(File.OpenRead(project.FilePath));
                //ProjectRootElement root = ProjectRootElement.Create(xmlReader, new ProjectCollection(), preserveFormatting: true);
                //MSBuildProject msbuildProject = new MSBuildProject(root);
                return null;
            });
        }


        public Document CurrentDocument => ShinySyntaxReceiver.CurrentDocument;
        //public MSBuildProject

        public bool IsProjectType(string projectTypeGuid)
        {
        }


        public string? GetXamFormsAppClassFullName()
        {
            var classes = this
                .Context
                .GetAllDerivedClassesForType("Xamarin.Forms.Application")
                .Where(x => !x.ContainingNamespace.Name.StartsWith("Prism."))
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

                    appClass = this.FindClosestType(classes);
                    if (appClass != null)
                        this.Log.Warn($"Found closest type - {appClass.ToDisplayString()}.  IF this is wrong, please override the type where this is being used");

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

                    startupClass = this.FindClosestType(startupClasses);
                    if (startupClass != null)
                        this.Log.Warn($"Found closest type - {startupClass.ToDisplayString()}.  IF this is wrong, please override the type where this is being used");

                    break;
            }
            return startupClass?.ToDisplayString();
        }


        public INamedTypeSymbol FindClosestType(List<INamedTypeSymbol> symbols)
        {
            var ns = this.GetRootNamespace();
            var index = ns.IndexOf(".");
            if (index > -1)
                ns = ns.Substring(0, index);

            var found = symbols.FirstOrDefault(x => x.ContainingNamespace.Name.StartsWith(ns));
            return found;
        }


        public GeneratorExecutionContext Context { get; private set; }
        public bool IsStartupGenerated { get; set; }
        public string GetRootNamespace() => this.Context.GetProjectInstance().GetPropertyValue("RootNamespace");
    }
}

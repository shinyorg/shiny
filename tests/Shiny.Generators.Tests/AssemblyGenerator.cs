using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;


namespace Shiny.Generators.Tests
{
    public class AssemblyGenerator
    {
        readonly List<MetadataReference> references = new List<MetadataReference>();
        readonly List<SyntaxTree> sources = new List<SyntaxTree>();


        public void AddSource(string sourceText)
        {
            var source = SourceText.From(sourceText, Encoding.UTF8);
            var tree = CSharpSyntaxTree.ParseText(source);
            this.sources.Add(tree);
        }


        public void AddReferences(params string[] assemblies)
        {
            foreach (var assembly in assemblies)
                this.AddReference(assembly);
        }


        public void AddReference(string assemblyName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName);
            var ass = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => x.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase));

            if (ass == null)
                throw new ArgumentException($"Assembly '{assemblyName}' not found at '{path}'");

            var reference = MetadataReference.CreateFromFile(ass.Location);
            this.references.Add(reference);
        }


        public CSharpCompilation Create(string assemblyName) => CSharpCompilation
            .Create(assemblyName)
            .WithReferences(this.references)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddSyntaxTrees(this.sources);


        public Compilation DoGenerate(string dllName, ISourceGenerator sourceGenerator)
        {
            //var meta = MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location);
            //this.references.Add(meta);
            var driver = CSharpGeneratorDriver.Create(sourceGenerator);
            var inputCompilation = this.Create(dllName);

            driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diags
            );
            foreach (var diag in diags)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                    throw new ArgumentException(diag.GetMessage());
            }
            return outputCompilation;
        }
    }
}

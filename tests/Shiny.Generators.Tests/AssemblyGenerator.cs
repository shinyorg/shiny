using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;


namespace Shiny.Generators.Tests
{
    public class AssemblyGenerator
    {
        static readonly MetadataReference CorlibReference =  MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(typeof(GCSettings).Assembly.Location);
        static readonly MetadataReference NetStdReference = MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location);
        static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

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
            this.references.AddRange(new []
            {
                CorlibReference,
                SystemRuntimeReference,
                NetStdReference,
                SystemCoreReference,
                CSharpSymbolsReference,
                CodeAnalysisReference
            });

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

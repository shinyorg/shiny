using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;


namespace Shiny.Tests.Generators
{
    public class AssemblyGenerator
    {
        static readonly MetadataReference CorlibReference =  MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        //static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(typeof(GCSettings).Assembly.Location);
        //static readonly MetadataReference NetStdReference = MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location);
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


        public Compilation DoGenerate(string dllName, ISourceGenerator sourceGenerator, bool validateCompilations = false)
        {
            var dd = typeof(Enumerable).Assembly.Location;
            var coreDir = Directory.GetParent(dd);
            var netstandard = MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "netstandard.dll");
            var runtime = MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.Runtime.dll");
            var objModel = MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "System.ObjectModel.dll");

            this.references.AddRange(new []
            {
                netstandard,
                runtime,
                objModel,
                CorlibReference,
                //SystemRuntimeReference,
                //NetStdReference,
                SystemCoreReference,
                CSharpSymbolsReference,
                CodeAnalysisReference
            });
            this.AddReference("netstandard");

            var driver = CSharpGeneratorDriver.Create(sourceGenerator);
            var inputCompilation = this.Create(dllName);

            driver.RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diags
            );
            this.ThrowAnyErrors(diags);

            // HACK: this is a temporary hack to deal with testing Xamarin iOS and Android
            if (validateCompilations)
            {
                this.ThrowAnyErrors(inputCompilation.GetDiagnostics());
                this.ThrowAnyErrors(outputCompilation.GetDiagnostics());
            }
            return outputCompilation;
        }


        void ThrowAnyErrors(ImmutableArray<Diagnostic> diags)
        {
            foreach (var diag in diags)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    var msg = diag.GetMessage();
                    if (diag.Location != null)
                        msg += " - " + diag.Location.ToString();

                    throw new ArgumentException(msg);
                }
            }
        }
    }
}

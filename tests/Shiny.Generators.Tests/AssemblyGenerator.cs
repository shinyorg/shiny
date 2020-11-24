using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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


//# Searching a type in all available assemblies
//        The method GetTypeByMetadataName returns null if a type is defined in 2 different assemblies.So, if you want to get all types that match a full name, you have to look at all assemblies and call GetTypeByMetadataName per assembly.

//public static IEnumerable<INamedTypeSymbol> GetTypesByMetadataName(this Compilation compilation, string typeMetadataName)
//        {
//            return compilation.References
//                .Select(compilation.GetAssemblyOrModuleSymbol)
//                .OfType<IAssemblySymbol>()
//                .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
//                .Where(t => t != null);
//        }
        public CSharpCompilation Create(string assemblyName)
        {
            this.AddReference("Shiny.Core");
            //this.AddReference(typeof(object).Assembly.Location);

            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyName + ".dll");
            //Assembly ass;
            //ass.GetReferencedAssemblies()
            //ass.ToMetadataReference();

            return CSharpCompilation
                .Create(localPath)
                .WithReferences(this.references)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(this.sources);
        }
    }
}

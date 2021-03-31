using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;


namespace Shiny.Tests.Generators
{
    public abstract class AbstractSourceGeneratorTests<T> : IDisposable where T : class, ISourceGenerator, new()
    {
        protected ITestOutputHelper Output { get; }
        protected AssemblyGenerator Generator { get; }
        protected Compilation Compilation { get; set; }


        protected AbstractSourceGeneratorTests(ITestOutputHelper output, params string[] assemblies)
        {
            this.Output = output;
            this.Generator = new AssemblyGenerator();
            this.Generator.AddReferences(assemblies);
        }


        public void Dispose()
        {
            if (this.Compilation != null)
                foreach (var syntaxTree in this.Compilation.SyntaxTrees)
                    this.Output.WriteLine(syntaxTree.ToString());
        }


        protected virtual void RunGenerator(bool validateCompilations = false, [CallerMemberName] string? compileAssemblyName = null)
        {
            var sourceGenerator = this.Create();
            this.Compilation = this.Generator.DoGenerate(
                compileAssemblyName,
                sourceGenerator,
                validateCompilations
            );
        }


        protected virtual T Create() => new T();
    }
}

using System;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
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
                this.Output.WriteSyntaxTrees(this.Compilation);
        }


        protected virtual void RunGenerator([CallerMemberName] string? compileAssemblyName = null)
        {
            var sourceGenerator = new T();
            this.Compilation = this.Generator.DoGenerate(
                compileAssemblyName,
                sourceGenerator
            );
        }
    }
}

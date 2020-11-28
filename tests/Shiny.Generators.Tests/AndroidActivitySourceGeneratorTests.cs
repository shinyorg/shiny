using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class AndroidActivitySourceGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;


        public AndroidActivitySourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
            this.generator.AddReference("Mono.Android");
            this.generator.AddReference("Shiny");
            this.generator.AddReference("Shiny.Core");
        }


        [Fact]
        public void Test()
        {
            this.generator.AddSource(@"
[assembly:Shiny.GenerateStartupAttribute]
[assembly:Shiny.ShinyApplicationAttribute]
");
        }
    }
}

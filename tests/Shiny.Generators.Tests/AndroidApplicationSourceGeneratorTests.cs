using System;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class AndroidApplicationSourceGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;


        public AndroidApplicationSourceGeneratorTests(ITestOutputHelper output)
        {
            this.output = output;
            this.generator = new AssemblyGenerator();
            this.generator.AddReference("Mono.Android");
            this.generator.AddReference("Shiny");
            this.generator.AddReference("Shiny.Core");
        }


        // TODO: build android
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test with/out XF
        // test for auto-init of 3rd party
        // test for activity
        // test for permissions
        // test for init
        // test for partial
        // test xamessentials


        // TODO: auto startup
        // add shiny libs one by one to ensure output (push has some special considerations)
        // test for xam forms auto init

        [Fact]
        public void Test()
        {
            this.generator.AddSource("[assembly: Shiny.ShinyApplicationAttribute]");
            var compile = this.generator.DoGenerate(
                nameof(Test),
                new AndroidApplicationSourceGenerator()
            );
            this.output.WriteSyntaxTrees(compile);

            //compile.AssertTypesExist("");
        }
    }
}

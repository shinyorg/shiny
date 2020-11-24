using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public class ShinyCoreGeneratorTests
    {
        readonly ITestOutputHelper output;
        readonly AssemblyGenerator generator;

        public ShinyCoreGeneratorTests(ITestOutputHelper output)
        {
            this.generator = new AssemblyGenerator();

            this.output = output;
        }


        // TODO: build xam ios libs, add appdelegate
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test for methods
        // test with/out XF
        // test for auto-init of 3rd party
        // ensure appdelegate methods are generated only when necessary!

        // TODO: build android
        // test for finding custom shiny startup
        // test for auto-gen shiny startup
        // test with/out XF
        // test for auto-init of 3rd party
        // test for activity
            // test for permissions
            // test for init
            // test for partial

        // TODO: auto startup
        // add shiny libs one by one to ensure output (push has some special considerations)
        // test for xam forms auto init

        [Fact]
        public void AutoStartupCore()
        {
            this.generator.AddSource(@"[assembly:Shiny.GenerateStartupAttribute]");
            this.generator.DoGenerate(
                this.output,
                "",
                new ShinyCoreGenerator(),
                ""
            );
        }
    }
}
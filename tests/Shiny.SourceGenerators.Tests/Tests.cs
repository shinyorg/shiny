//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using Xunit.Abstractions;

//namespace Shiny.SourceGenerators.Tests;


//public class Tests
//{
//    readonly ITestOutputHelper output;
//    public Tests(ITestOutputHelper output)
//    {
//        this.output = output;
//    }


//    [Fact]
//    public void Test1()
//    {
//        // need two assemblies?
//        var comp1 = CSharpCompilation.Create(
//            assemblyName: "Tests",
//            syntaxTrees: new[] { mauiProj, prjOne, prjTwo }
//        );

//        var generator = new SourceGenerators()
//        var driver = CSharpGeneratorDriver.Create(generator);
//        driver.RunGeneratorsAndUpdateCompilation(comp1, out var output, out var diags);

//        foreach (var st in output.SyntaxTrees)
//            this.output.WriteLine(st.ToString());
//    }
//}
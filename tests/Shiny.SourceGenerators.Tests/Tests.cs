using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;

namespace Shiny.Auto.Tests;


public class Tests
{
    readonly ITestOutputHelper output;
    public Tests(ITestOutputHelper output)
    {
        this.output = output;
    }


    [Fact]
    public void Test1()
    {
        // this setup more for debugging than an actual output unit test due complexities of the large string comparison
        var mauiProj = CSharpSyntaxTree.ParseText(@"
namespace Microsoft.Maui.Controls
{
    public class Page { }
}");

        var prjOne = CSharpSyntaxTree.ParseText(@"
namespace Project1
{
    public class MyPage : Microsoft.Maui.Controls.Page {}
}
");
        var prjTwo = CSharpSyntaxTree.ParseText(@"
namespace Project2
{
    public class MyPage : Microsoft.Maui.Controls.Page {}
}

namespace Project2.NestedNamespace
{
    public class MyOtherPage : Microsoft.Maui.Controls.Page {}
}
");

        // need two assemblies?
        var comp1 = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { mauiProj, prjOne, prjTwo }
        );

        var generator = (IIncrementalGenerator)new PageNavGenerator
        {
            GlobalOptions = new GlobalOptions("Root", "Test", null)
        };
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(comp1, out var output, out var diags);

        foreach (var st in output.SyntaxTrees)
            this.output.WriteLine(st.ToString());
    }
}
namespace Shiny.BluetoothLE.Hosting.SourceGenerators.Tests;


static class Snippets
{
    const string Usings = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using Shiny.BluetoothLE;
        using Shiny.BluetoothLE.Hosting;

        """;


    /// <summary>
    /// Wraps handler members in a minimal [BleService] class in the TestApp namespace.
    /// </summary>
    public static string Service(string members, string attributes = "[BleService(\"180D\")]")
        => $$"""
            {{Usings}}
            namespace TestApp;

            {{attributes}}
            public partial class TestService
            {
            {{members}}
            }
            """;


    /// <summary>
    /// Wraps arbitrary top level declarations.
    /// </summary>
    public static string Raw(string body) => Usings + "\nnamespace TestApp;\n\n" + body;
}

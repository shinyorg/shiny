using System;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;


namespace Shiny.Generators.Tests
{
    public static class Extensions
    {
        public static void WriteSyntaxTrees(this ITestOutputHelper testOutput, Compilation compile)
        {
            foreach (var syntaxTree in compile.SyntaxTrees)
                testOutput.WriteLine(syntaxTree.ToString());
        }


        public static void AssertTypesExist(this Compilation compile, params string[] assertTypes)
        {
            foreach (var assert in assertTypes)
            {
                var type = compile.GetTypeByMetadataName(assert);
                type.Should().NotBeNull($"Metadata type '{assert}' was not found");
            }
        }
    }
}

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tests
{
    public static class Extensions
    {
        public static void AssertHasContent(this Compilation compile, string content, string? because = null)
            => compile
                .SyntaxTrees
                .Any(x => x.ToString().Contains(content))
                .Should()
                .BeTrue(because);
    }
}

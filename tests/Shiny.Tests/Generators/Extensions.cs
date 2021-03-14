using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators.Tests
{
    public static class Extensions
    {
        public static void AssertContent(this Compilation compile, string content, string? because = null, bool found = true)
            => compile
                .SyntaxTrees
                .Any(x => x.ToString().Contains(content))
                .Should()
                .Be(found, because);
    }
}

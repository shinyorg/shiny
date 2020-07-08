using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Uno.SourceGeneration;


namespace Shiny.BluetoothLE.RefitClient.Generator
{
    public static class Extensions
    {
        public static IEnumerable<INamedTypeSymbol> GetAllInterfaceTypes(this SourceGeneratorContext context) => context
            .Compilation
            .SyntaxTrees
            .Select(x => context.Compilation.GetSemanticModel(x))
            .SelectMany(x => x
                .SyntaxTree
                .GetRoot()
                .DescendantNodes()
                .OfType<InterfaceDeclarationSyntax>()
                .Select(y => y.GetDeclaredSymbol(x))
            )
            .OfType<INamedTypeSymbol>();


        public static IEnumerable<INamedTypeSymbol> GetAllImplementationsOfType<T>(this SourceGeneratorContext context)
             => SymbolFinder.FindImplementationsAsync(context.Compilation.GetTypeByMetadataName(typeof(T).Name), context.Project.Solution).Result.OfType<INamedTypeSymbol>();
    }
}

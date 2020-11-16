using System;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public class ShinySyntaxReceiver : ISyntaxReceiver
    {
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            //syntaxNode.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind);
        }
    }
}

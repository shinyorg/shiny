using System;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public class ShinySyntaxReceiver : ISyntaxReceiver
    {
        internal static Document CurrentDocument { get; private set; }


        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            var container = syntaxNode.SyntaxTree.GetText().Container;
            var ws = Workspace.GetWorkspaceRegistration(container);
            var documentId = ws.Workspace.GetDocumentIdInCurrentContext(container);
            CurrentDocument = ws.Workspace.CurrentSolution.GetDocument(documentId);
        }
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace MockableStaticGenerator
{
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        internal List<ClassDeclarationSyntax> Classes { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                Classes.Add(classDeclarationSyntax);
            }
        }
    }
}
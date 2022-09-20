using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace MockableStaticGenerator
{
    internal class SyntaxReceiver : ISyntaxReceiver
    {
        internal HashSet<ClassDeclarationSyntax> Classes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclarationSyntax)
            {

                Classes.Add(classDeclarationSyntax);
            }
        }
    }
}
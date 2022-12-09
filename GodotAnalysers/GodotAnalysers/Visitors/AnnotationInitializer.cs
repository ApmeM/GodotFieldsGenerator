#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class AnnotationInitializer : CSharpSyntaxRewriter
    {
        private const string SceneReferenceAttributeName = nameof(SceneReferenceAttribute);

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var type = node.Identifier.ToFullString();
            var members = node.AttributeLists
                .SelectMany(a => a.Attributes)
                .Where(a => a.Name.NormalizeWhitespace().ToFullString() == SceneReferenceAttributeName ||
                            a.Name.NormalizeWhitespace().ToFullString() + "Attribute" == SceneReferenceAttributeName)
                .Select(a => ((LiteralExpressionSyntax)a.ArgumentList.Arguments.First().Expression).Token.ValueText)
                .ToArray();

            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node); // Pass ORIGINAL node
            return node.WithAdditionalAnnotations(GetAnnotations(type, node.SyntaxTree.FilePath, members));
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotations(string type, string filePath, string[] members)
        {
            yield return new SyntaxAnnotation(Constants.Type, type);
            yield return new SyntaxAnnotation(Constants.FilePath, filePath);
            foreach (var member in members)
            {
                yield return new SyntaxAnnotation(Constants.Member, member);
            }
        }
    }
}
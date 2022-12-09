#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    public class PartialClassGenerator : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            node = CompilationUnit()
                .WithExterns(node.Externs)
                .WithUsings(node.Usings)
                .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsApplicable).ToArray())
                .AddMembers(node.Members.OfType<NamespaceDeclarationSyntax>().ToArray());
            return base.VisitCompilationUnit(node);
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            node = NamespaceDeclaration(node.Name)
                .WithExterns(node.Externs)
                .WithUsings(node.Usings)
                .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsApplicable).ToArray());
            return base.VisitNamespaceDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var n = ClassDeclaration(node.Identifier)
                            .WithModifiers(node.Modifiers)
                            .WithTypeParameterList(node.TypeParameterList)
                            .AddMembers(node.Members.OfType<ClassDeclarationSyntax>().Where(IsApplicable).ToArray())
                            .CopyAnnotationsFrom(node);
            return base.VisitClassDeclaration(n);
        }

        private static bool IsApplicable(ClassDeclarationSyntax node)
        {
            return node.Modifiers.Any(i => i.Kind() == SyntaxKind.PartialKeyword) &&
                node.GetAnnotations(Constants.Member).Any();
        }
    }
}
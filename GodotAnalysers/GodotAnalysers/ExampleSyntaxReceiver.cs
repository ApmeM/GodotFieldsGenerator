#pragma warning disable RS2008 // Enable analyzer release tracking

namespace Godot
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class ExampleSyntaxReceiver : ISyntaxReceiver
        {
            public List<CompilationUnitSyntax> Units { get; } = new List<CompilationUnitSyntax>();

            void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode node)
            {
                if (node is CompilationUnitSyntax unit) Units.Add(unit);
            }
        }
}
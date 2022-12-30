#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator]
    public partial class SceneFieldsGenerator : ISourceGenerator
    {
        public IFileReader fileReader = new RealFileReader();

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ExampleSyntaxReceiver)context.SyntaxReceiver ?? throw new Exception("SyntaxReceiver is null");

            foreach (var unit in receiver.Units)
            {
                var unitSyntax = unit;

                var name = Path.GetFileNameWithoutExtension(unitSyntax.SyntaxTree.FilePath) + $".Generated.cs";

                unitSyntax = (CompilationUnitSyntax)new AnnotationInitializer().Visit(unitSyntax);
                unitSyntax = (CompilationUnitSyntax)new PartialClassGenerator().Visit(unitSyntax);
                unitSyntax = (CompilationUnitSyntax)new PartialClassContentGenerator(fileReader).Visit(unitSyntax);
                if (unitSyntax.DescendantNodes().OfType<TypeDeclarationSyntax>().Any())
                {
                    context.AddSource(name, unitSyntax.NormalizeWhitespace().ToString());
                }
            }
        }
    }
}
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    [Generator]
    public class SceneFieldsGenerator : ISourceGenerator
    {
        private const string SceneReferenceAttributeName = nameof(SceneReferenceAttribute);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ExampleSyntaxReceiver)context.SyntaxReceiver ?? throw new Exception("SyntaxReceiver is null");

            foreach (var unit in receiver.Units)
            {
                var name = Path.GetFileNameWithoutExtension(unit.SyntaxTree.FilePath) + $".Generated.cs";
                var content = GetSourceContent(unit);
                if (content != null)
                {
                    context.AddSource(name, content.NormalizeWhitespace().ToString());
                }
            }
        }

        private static CompilationUnitSyntax GetSourceContent(CompilationUnitSyntax unit)
        {
            unit = (CompilationUnitSyntax)new AnnotationInitializer().Visit(unit);
            unit = (CompilationUnitSyntax)new PartialClassGenerator().Visit(unit);
            unit = (CompilationUnitSyntax)new PartialClassContentGenerator().Visit(unit);
            if (unit.DescendantNodes().OfType<TypeDeclarationSyntax>().Any()) return unit;
            return null;
        }

        private class ExampleSyntaxReceiver : ISyntaxReceiver
        {

            public List<CompilationUnitSyntax> Units { get; } = new List<CompilationUnitSyntax>();

            void ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode node)
            {
                if (node is CompilationUnitSyntax unit) Units.Add(unit);
            }

        }

        private class AnnotationInitializer : CSharpSyntaxRewriter
        {
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
                yield return new SyntaxAnnotation("Type", type);
                yield return new SyntaxAnnotation("Type.FilePath", filePath);
                foreach (var member in members)
                {
                    yield return new SyntaxAnnotation("Type.Member", member);
                }
            }
        }

        private class PartialClassGenerator : CSharpSyntaxRewriter
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
                    node.GetAnnotations("Type.Member").Any();
            }
        }


        private class PartialClassContentGenerator : CSharpSyntaxRewriter
        {
            private static Regex nodeDefinition = new Regex("\\[node.*name=\"(.*?)\".*]");
            private static Regex builtInTypes = new Regex("type=\"(.*?)\""); 
            private static Regex parentHierarchy = new Regex("parent=\"(.*?)\"");
            private static Regex instanceTypes = new Regex("instance=");

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (node.Modifiers.Any(a => a.IsKind(SyntaxKind.StaticKeyword)))
                {
                    return base.VisitClassDeclaration(node);
                }

                var filePath = node.GetAnnotations("Type.FilePath").Single().Data;
                var members = node.GetAnnotations("Type.Member").Select(i => i.Data).ToArray();

                var tree = new Dictionary<string, string>
                {
                    { ".", "." }
                };

                var memberDeclarations = new List<MemberDeclarationSyntax>();

                var memberDeclarationBuilder = new StringBuilder();
                memberDeclarationBuilder.AppendLine("public void FillMembers() {");

                string baseType = null;

                foreach (var member in members)
                {
                    var scenePath = Path.Combine(Path.GetDirectoryName(filePath), member);
                    var content = File.ReadLines(scenePath);
                    foreach (var line in content)
                    {
                        var nodeDefinitionMatch = nodeDefinition.Match(line);
                        if (!nodeDefinitionMatch.Success)
                            continue;
                        
                        var name = nodeDefinitionMatch.Groups[1].Value;
                        var fieldName = ToFieldName(name);

                        var type = string.Empty;

                        var builtInTypesMatch = builtInTypes.Match(line);
                        if (builtInTypesMatch.Success)
                        {
                            type = builtInTypesMatch.Groups[1].Value;
                        }

                        var instanceTypesMatch = instanceTypes.Match(line);
                        if (instanceTypesMatch.Success)
                        {
                            type = name;
                        }

                        var parentHierarchyMatch = parentHierarchy.Match(line);
                        if (parentHierarchyMatch.Success)
                        {
                            var parent = parentHierarchyMatch.Groups[1].Value;
                            var firstParentElement = parent.Split('/')[0];
                            var otherParentElements = parent.Substring(firstParentElement.Length);
                            var path = tree[firstParentElement] + otherParentElements + "/" + name;
                            tree[name] = path;
                        }
                        else
                        {
                            baseType = type;
                        }

                        if (tree.ContainsKey(name) && !tree[name].Contains("EXAMPLE"))
                        {
                            memberDeclarations.Add(ParseMemberDeclaration($"private {type} {fieldName};"));
                            memberDeclarationBuilder.AppendLine($"this.{fieldName} = this.GetNode<{type}>(\"{tree[name]}\");");
                        }
                    }
                }

                memberDeclarationBuilder.AppendLine("}");

                memberDeclarations.Add(ParseMemberDeclaration(memberDeclarationBuilder.ToString()));

                node = node.AddMembers(memberDeclarations.ToArray());
                if (!string.IsNullOrWhiteSpace(baseType))
                {
                    node = node.AddBaseListTypes(SimpleBaseType(IdentifierName(baseType)));
                }

                return base.VisitClassDeclaration(node);
            }

            private string ToFieldName(string name)
            {
                return name[0].ToString().ToLower() + name.Substring(1);
            }
        }
    }

    public static class Ext
    {
        public static T CopyAnnotationsFrom<T>(this T node, SyntaxNode other) where T : SyntaxNode
        {
            return other.CopyAnnotationsTo(node);
        }
    }
}
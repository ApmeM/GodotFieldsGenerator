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

    public class PartialClassContentGenerator : CSharpSyntaxRewriter
    {
        private static Regex extResourceDefinition = new Regex("\\[ext_resource.*path=\"(.*?)\".*type=\"(.*?)\".*id=(.*?)]");
        private static Regex nodeDefinition = new Regex("\\[node.*name=\"(.*?)\".*]");
        private static Regex builtInTypes = new Regex("type=\"(.*?)\"");
        private static Regex parentHierarchy = new Regex("parent=\"(.*?)\"");
        private static Regex instance = new Regex("instance=");
        private static Regex instanceExtResources = new Regex("instance=ExtResource\\((.*?)\\)");
        private IFileReader fileReader;

        public PartialClassContentGenerator(IFileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.Modifiers.Any(a => a.IsKind(SyntaxKind.StaticKeyword)))
            {
                return base.VisitClassDeclaration(node);
            }

            var filePath = node.GetAnnotations(Constants.FilePath).Single().Data;
            var members = node.GetAnnotations(Constants.Member).Select(i => i.Data).ToArray();

            var tree = new Dictionary<string, string>
                {
                    { ".", "." }
                };

            var memberDeclarations = new List<MemberDeclarationSyntax>();

            var memberDeclarationBuilder = new StringBuilder();
            memberDeclarationBuilder.AppendLine("protected virtual void FillMembers() {");

            string baseType = null;

            foreach (var member in members)
            {
                var extresources = new Dictionary<string, Tuple<string, string>>();

                var scenePath = Path.Combine(Path.GetDirectoryName(filePath), member);
                var content = this.fileReader.ReadLines(scenePath);
                foreach (var line in content)
                {
                    var extResourceDefinitionMatch = extResourceDefinition.Match(line);
                    if (extResourceDefinitionMatch.Success)
                    {
                        var resPath = extResourceDefinitionMatch.Groups[1].Value;
                        var resType = extResourceDefinitionMatch.Groups[2].Value;
                        var resId = extResourceDefinitionMatch.Groups[3].Value.Trim();

                        extresources[resId] = new Tuple<string, string>(resPath, resType);
                        continue;
                    }

                    var nodeDefinitionMatch = nodeDefinition.Match(line);
                    if (nodeDefinitionMatch.Success)
                    {
                        var name = nodeDefinitionMatch.Groups[1].Value;
                        var fieldName = name.ToFieldName();

                        var type = string.Empty;

                        var builtInTypesMatch = builtInTypes.Match(line);
                        if (builtInTypesMatch.Success)
                        {
                            type = builtInTypesMatch.Groups[1].Value;
                        }

                        var instanceMatch = instance.Match(line);
                        if (instanceMatch.Success)
                        {
                            type = name;
                        }

                        var instanceExtResourcesMatch = instanceExtResources.Match(line);
                        if (instanceExtResourcesMatch.Success)
                        {
                            var resNum = instanceExtResources.Match(line);
                            var pathToResource = extresources[resNum.Groups[1].Value.Trim()].Item1;
                            type = Path.GetFileNameWithoutExtension(pathToResource);
                        }

                        var parentHierarchyMatch = parentHierarchy.Match(line);
                        if (parentHierarchyMatch.Success)
                        {
                            var parent = parentHierarchyMatch.Groups[1].Value;
                            var firstParentElement = parent.Split('/')[0];
                            var otherParentElements = parent.Substring(firstParentElement.Length);
                            if (tree.ContainsKey(firstParentElement))
                            {
                                tree[name] = tree[firstParentElement] + otherParentElements + "/" + name;
                            }
                            else
                            {
                                tree[name] = "./" + parent + "/" + name;
                            }
                        }
                        else
                        {
                            baseType = type;
                        }

                        if (!string.IsNullOrWhiteSpace(type) && tree.ContainsKey(name) && !tree[name].Contains("EXAMPLE"))
                        {
                            memberDeclarations.Add(ParseMemberDeclaration($"protected {type} {fieldName} {{ get; private set; }}"));
                            memberDeclarationBuilder.AppendLine($"this.{fieldName} = this.GetNode<{type}>(\"{tree[name]}\");");
                        }

                        continue;
                    }
                }
            }
            memberDeclarations.Add(ParseMemberDeclaration($"protected DependencyInjectorContext di {{ get; private set; }}"));
            memberDeclarationBuilder.AppendLine($"this.di = DependencyInjector.GetNewContext();");

            memberDeclarationBuilder.AppendLine("}");
            memberDeclarations.Add(ParseMemberDeclaration(memberDeclarationBuilder.ToString()));
            node = node.AddMembers(memberDeclarations.ToArray());

            if (!string.IsNullOrWhiteSpace(baseType))
            {
                node = node.AddBaseListTypes(SimpleBaseType(IdentifierName(baseType)));
            }

            return base.VisitClassDeclaration(node);
        }
    }
}
#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [Generator]
    public class DependencyInjectorGenerator : ISourceGenerator
    {
        private class Data
        {
            public List<string> Parameters;
            public bool OnDemand;
            public string TypeName;
            internal string TypeFullName;
        }

        private const string InjectableAttributeName = nameof(InjectableAttribute);
        private const string InjectableConstructorAttributeName = nameof(InjectableConstructorAttribute);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ExampleSyntaxReceiver)context.SyntaxReceiver ?? throw new Exception("SyntaxReceiver is null");

            var data = new List<Data>();

            foreach (var unit in receiver.Units)
            {
                var model = context.Compilation.GetSemanticModel(unit.SyntaxTree);
                new AnnotationInitializer(model, data).Visit(unit);
            }

            var content = GetSourceContent(data);
            context.AddSource("DependencyInjector.Generated.cs", content);
        }

        private string GetSourceContent(List<Data> data)
        {
            var dict = data.ToDictionary(a => a.TypeFullName, a => a);
            var sorted = SortDfs(data, data.ToDictionary(a => a, a => a.Parameters.Select(b => dict[b]).ToList())).SelectMany(a => a);
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("namespace DependencyInjection");
            sb.AppendLine("{");
            sb.AppendLine("    public static class DependencyInjector");
            sb.AppendLine("    {");
            sb.AppendLine("         public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();");
            sb.AppendLine("         public static DependencyInjectorContext GetNewContext()");
            sb.AppendLine("         {");
            sb.AppendLine("             return new DependencyInjectorContext(GlobalContext);");
            sb.AppendLine("         }");
            sb.AppendLine("    }");
            sb.AppendLine("    public class DependencyInjectorContext");
            sb.AppendLine("    {");
            foreach (var def in sorted)
            {
                sb.AppendLine($"        public readonly {def.TypeFullName} {def.TypeName.ToFieldName()};");
            }

            sb.AppendLine("        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)");
            sb.AppendLine("        {");

            foreach (var def in sorted)
            {
                sb.Append($"            {def.TypeName.ToFieldName()} = " + ((def.OnDemand) ? "" : $"copyContext?.{def.TypeName.ToFieldName()} ?? " ) + $" new {def.TypeFullName} (");
                sb.Append(string.Join(",", def.Parameters
                    .Select(a => dict[a])
                    .Select(a => a.TypeName.ToFieldName())));
                sb.AppendLine($");");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static int Dfs<T>(T current, Dictionary<T, List<T>> edges, Dictionary<T, int> result)
        {
            result[current] = 0;

            if (edges.ContainsKey(current))
            {
                foreach (var dependent in edges[current])
                {
                    if (result.ContainsKey(dependent))
                    {
                        result[current] = Math.Max(result[current], result[dependent] + 1);
                        continue;
                    }

                    result[current] = Math.Max(result[current], Dfs(dependent, edges, result) + 1);
                }
            }

            return result[current];
        }

        private static ILookup<int, T> SortDfs<T>(List<T> items, Dictionary<T, List<T>> edges)
        {
            var result = new Dictionary<T, int>(items.Count);

            foreach (var current in items)
            {
                if (result.ContainsKey(current))
                {
                    continue;
                }

                Dfs(current, edges, result);
            }

            return result.OrderBy(a => a.Value).ToLookup(a => a.Value, a => a.Key);
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
            private SemanticModel model;
            private List<Data> data;

            public AnnotationInitializer(SemanticModel model, List<Data> data)
            {
                this.model = model;
                this.data = data;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var type = model.GetDeclaredSymbol(node);
                var attribute = node.AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Where(a => a.Name.NormalizeWhitespace().ToFullString() == InjectableAttributeName ||
                                a.Name.NormalizeWhitespace().ToFullString() + "Attribute" == InjectableAttributeName)
                    .Select(a => ((LiteralExpressionSyntax)a.ArgumentList.Arguments.First().Expression).Token.ValueText)
                    .FirstOrDefault();


                if (attribute != null)
                {
                    node.Members.Where(a=>a.Kind() == SyntaxKind.MethodDeclaration);
                    
                    var onDemand = (bool)bool.Parse(attribute);
                    IMethodSymbol constructor;
                    if (type.Constructors.Count() > 1)
                    {
                        var constructors = type
                            .Constructors
                            .Where(a => a.GetAttributes().Any(b => b.AttributeClass.Name == InjectableConstructorAttributeName))
                            .ToList();
                        if (constructors.Count == 0)
                        {
                            throw new Exception($"Found multiple constructors Please mark one with {InjectableConstructorAttributeName} for type {type.Name}");
                        }
                        if (constructors.Count > 1)
                        {
                            throw new Exception($"Found multiple constructors marked with {InjectableConstructorAttributeName} for type {type.Name}");
                        }
                        constructor = constructors.First();
                    }
                    else
                    {
                        constructor = type.Constructors.First();
                    }

                    var parameterTypes = constructor.Parameters.Select(a => a.Type.GetFullName()).ToList();

                    this.data.Add(new Data
                    {
                        TypeFullName = type.GetFullName(),
                        TypeName = type.Name,
                        OnDemand = onDemand,
                        Parameters = parameterTypes
                    });
                }
                return (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            }
        }
    }
}
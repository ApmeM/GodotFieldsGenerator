#pragma warning disable RS2008 // Enable analyzer release tracking

namespace Godot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public class InjectionInitializer : CSharpSyntaxRewriter
    {
        private const string InjectableAttributeName = nameof(InjectableAttribute);
        private const string InjectableConstructorAttributeName = nameof(InjectableConstructorAttribute);

        private SemanticModel model;
        private DataList data;

        public InjectionInitializer(SemanticModel model, DataList data)
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
                node.Members.Where(a => a.Kind() == SyntaxKind.MethodDeclaration);

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

                this.data.Items.Add(new DataItem
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
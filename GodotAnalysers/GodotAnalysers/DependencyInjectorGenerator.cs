#pragma warning disable RS2008 // Enable analyzer release tracking

namespace GodotAnalysers
{
    using System;
    using Microsoft.CodeAnalysis;

    [Generator]
    public class DependencyInjectorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ExampleSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ExampleSyntaxReceiver)context.SyntaxReceiver ?? throw new Exception("SyntaxReceiver is null");

            var data = new DataList();

            foreach (var unit in receiver.Units)
            {
                var model = context.Compilation.GetSemanticModel(unit.SyntaxTree);
                new InjectionInitializer(model, data).Visit(unit);
            }

            context.AddSource("DependencyInjector.Generated.cs", data.GetSourceContent());
        }
    }
}
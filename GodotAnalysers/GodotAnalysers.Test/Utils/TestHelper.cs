using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

public static class TestHelper
{
    public static void DoTest(ISourceGenerator generator, string sourceText, params string[] resultTexts)
    {
        var expectedResults = new List<string>();
        foreach (var resultText in resultTexts)
        {
            expectedResults.Add(CreateCompilation(resultText).SyntaxTrees.First().GetRoot().NormalizeWhitespace().ToFullString());
        }

        Compilation compilation = CreateCompilation(sourceText);
        var syntaxTree = compilation.SyntaxTrees.First();
        compilation = compilation
            .RemoveSyntaxTrees(syntaxTree)
            .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

        GeneratorDriver driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        System.Console.WriteLine(diagnostics.FirstOrDefault()?.ToString());

        Assert.IsTrue(diagnostics.IsEmpty);
        Assert.AreEqual(expectedResults.Count + 1, outputCompilation.SyntaxTrees.Count());
        // System.Console.WriteLine(string.Join("\n", outputCompilation.GetDiagnostics()));
        // Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        Assert.AreEqual(expectedResults.Count, runResult.GeneratedTrees.Length);
        Assert.IsTrue(runResult.Diagnostics.IsEmpty);

        GeneratorRunResult generatorResult = runResult.Results[0];
        Assert.IsTrue(generatorResult.Generator == generator);
        Assert.IsTrue(generatorResult.Diagnostics.IsEmpty);
        Assert.AreEqual(expectedResults.Count, generatorResult.GeneratedSources.Length);
        Assert.IsNull(generatorResult.Exception);

        for(var i = 0; i < expectedResults.Count; i++)
        {
            Console.WriteLine(generatorResult.GeneratedSources[i].SyntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
            Assert.AreEqual(expectedResults[i], generatorResult.GeneratedSources[i].SyntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
        }
    }

    private static Compilation CreateCompilation(string source)
      => CSharpCompilation.Create("compilation",
          new[] { CSharpSyntaxTree.ParseText(source) },
          new[] { MetadataReference.CreateFromFile(typeof(SceneReferenceAttribute).GetTypeInfo().Assembly.Location) },
          new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace GodotAnalysers.Test
{
    [TestFixture]
    public class SceneFieldsGeneratorTest : System.Object
    {
        [Test]
        public void ResultPropertiesTest()
        {
            var source = @"
using GodotAnalysers;
[SceneReference(""C.txt"")]
public partial class C { }
";

            Compilation compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.First();
            compilation = compilation
                .RemoveSyntaxTrees(syntaxTree)
                .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

            var generator = new SceneFieldsGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Assert.IsTrue(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.IsTrue(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            //Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Assert.IsTrue(runResult.GeneratedTrees.Length == 1);
            Assert.IsTrue(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Assert.IsTrue(generatorResult.Generator == generator);
            Assert.IsTrue(generatorResult.Diagnostics.IsEmpty);
            Assert.IsTrue(generatorResult.GeneratedSources.Length == 1);
            Assert.IsTrue(generatorResult.Exception is null);
        }
        
        [Test]
        public void NormalSceneTest()
        {
            var source = @"
using GodotAnalysers;
[SceneReference(""C.txt"")]
public partial class C { }
";
            
            var expectedResult = CreateCompilation(@"using GodotAnalysers;

public partial class C : RigidBody2D
{
    protected Sprite sprite { get; private set; }
    protected CollisionShape2D collisionShape2D { get; private set; }
    protected CustomTextPopup customTextPopup { get; private set; }
    protected CustomTextPopup customTextPopup1 { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite = this.GetNode<Sprite>(""./Sprite"");
        this.collisionShape2D = this.GetNode<CollisionShape2D>(""./CollisionShape2D"");
        this.customTextPopup = this.GetNode<CustomTextPopup>(""./Sprite/CustomTextPopup"");
        this.customTextPopup1 = this.GetNode<CustomTextPopup>(""./Sprite/CustomTextPopup1"");
    }
}").SyntaxTrees.First().GetRoot().NormalizeWhitespace().ToFullString();

            Compilation compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.First();
            compilation = compilation
                .RemoveSyntaxTrees(syntaxTree)
                .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

            var generator = new SceneFieldsGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Assert.IsTrue(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.IsTrue(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            //Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Assert.IsTrue(runResult.GeneratedTrees.Length == 1);
            Assert.IsTrue(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Assert.IsTrue(generatorResult.Generator == generator);
            Assert.IsTrue(generatorResult.Diagnostics.IsEmpty);
            Assert.IsTrue(generatorResult.GeneratedSources.Length == 1);
            Assert.IsTrue(generatorResult.Exception is null);

            Assert.AreEqual(expectedResult, generatorResult.GeneratedSources[0].SyntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
        }

        
        [Test]
        public void InheritedSceneTest()
        {
            var source = @"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D { }
";
            
            var expectedResult = CreateCompilation(@"using GodotAnalysers;

public partial class D : C
{
    protected Sprite sprite2 { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite2 = this.GetNode<Sprite>(""./Sprite2"");
    }
}").SyntaxTrees.First().GetRoot().NormalizeWhitespace().ToFullString();

            Compilation compilation = CreateCompilation(source);
            var syntaxTree = compilation.SyntaxTrees.First();
            compilation = compilation
                .RemoveSyntaxTrees(syntaxTree)
                .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

            var generator = new SceneFieldsGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // We can now assert things about the resulting compilation:
            Assert.IsTrue(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.IsTrue(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            //Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Assert.IsTrue(runResult.GeneratedTrees.Length == 1);
            Assert.IsTrue(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Assert.IsTrue(generatorResult.Generator == generator);
            Assert.IsTrue(generatorResult.Diagnostics.IsEmpty);
            Assert.IsTrue(generatorResult.GeneratedSources.Length == 1);
            Assert.IsTrue(generatorResult.Exception is null);

            Assert.AreEqual(expectedResult, generatorResult.GeneratedSources[0].SyntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
        }

        private static Compilation CreateCompilation(string source)
          => CSharpCompilation.Create("compilation",
              new[] { CSharpSyntaxTree.ParseText(source) },
              new[] { MetadataReference.CreateFromFile(typeof(SceneReferenceAttribute).GetTypeInfo().Assembly.Location) },
              new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

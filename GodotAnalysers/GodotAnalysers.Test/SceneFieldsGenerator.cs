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
        public void NormalSceneTest()
        {
            DoTest(@"
using GodotAnalysers;
[SceneReference(""C.txt"")]
public partial class C { }
",
"C.txt",
@"
[gd_scene load_steps=5 format=2]
[ext_resource path=""res://art/ships/ships.meteorBrown_big1.tres"" type=""Texture"" id=1]
[ext_resource path=""res://Block.cs"" type=""Script"" id=2]
[ext_resource path=""res://Presentation/CustomTextPopup.tscn"" type=""PackedScene"" id=3]
[sub_resource type=""PhysicsMaterial"" id=2]
friction = 0.0
bounce = 1.0
[sub_resource type=""RectangleShape2D"" id=1]
extents = Vector2( 50, 40 )
[node name=""Block"" type=""RigidBody2D""]
collision_mask = 0
mode = 1
physics_material_override = SubResource( 2 )
script = ExtResource( 2 )
[node name=""Sprite"" type=""Sprite"" parent="".""]
texture = ExtResource( 1 )
[node name=""SpriteEXAMPLE"" type=""Sprite"" parent="".""]
texture = ExtResource( 1 )
[node name=""CollisionShape2D"" type=""CollisionShape2D"" parent="".""]
shape = SubResource( 1 )
[node name=""CustomTextPopup"" parent=""Sprite"" instance=ExtResource( 3 )]
visible = false
__meta__ = {
""_editor_description_"": """"
}
Text = """"
[node name=""CustomTextPopup1"" parent=""Sprite"" instance=ExtResource( 3 )]
visible = false
__meta__ = {
""_editor_description_"": """"
}
Text = """,
@"using GodotAnalysers;

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
}");
        }

        [Test]
        public void InheritedSceneTest()
        {
            DoTest(@"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D { }
",

@"D.txt",

@"
[gd_scene load_steps=3 format=2]
[ext_resource path=""res://Presentation/C.tscn"" type=""PackedScene"" id=1]
[node name=""BaseNode"" instance=ExtResource( 1 )]
[node name=""Sprite2"" type=""Sprite"" parent="".""]
",

@"using GodotAnalysers;

public partial class D : C
{
    protected Sprite sprite2 { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite2 = this.GetNode<Sprite>(""./Sprite2"");
    }
}");
        }

        [Test]
        public void ModifiedInheritedSceneTest()
        {
            DoTest(@"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D { }
",

@"D.txt",

@"
[gd_scene load_steps=3 format=2]
[ext_resource path=""res://Presentation/C.tscn"" type=""PackedScene"" id=1]
[node name=""BaseNode"" instance=ExtResource( 1 )]
[node name=""Sprite2"" type=""Sprite"" parent=""HUD/BottomButonsMargin/BottomButtonsContainer""]
",

@"using GodotAnalysers;

public partial class D : C
{
    protected Sprite sprite2 { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite2 = this.GetNode<Sprite>(""./HUD/BottomButonsMargin/BottomButtonsContainer/Sprite2"");
    }
}");
        }

        public void DoTest(string sourceText, string fileName, string fileContent, string resultText)
        {
            var expectedResult = CreateCompilation(resultText).SyntaxTrees.First().GetRoot().NormalizeWhitespace().ToFullString();

            Compilation compilation = CreateCompilation(sourceText);
            var syntaxTree = compilation.SyntaxTrees.First();
            compilation = compilation
                .RemoveSyntaxTrees(syntaxTree)
                .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

            var generator = new SceneFieldsGenerator();
            generator.fileReader = new FakeFileReader(fileName, fileContent);
            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            System.Console.WriteLine(diagnostics.FirstOrDefault()?.ToString());

            // We can now assert things about the resulting compilation:
            Assert.IsTrue(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.IsTrue(outputCompilation.SyntaxTrees.Count() == 2); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            // System.Console.WriteLine(string.Join("\n", outputCompilation.GetDiagnostics()));
            // Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

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

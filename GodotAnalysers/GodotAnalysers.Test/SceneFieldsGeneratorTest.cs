using NUnit.Framework;

namespace GodotAnalysers.Test
{
    [TestFixture]
    public class SceneFieldsGeneratorTest : System.Object
    {
        [Test]
        public void SimpleScene()
        {
                        DoTest(@"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D { }
",

@"D.txt",

@"
[gd_scene load_steps=5 format=2]
[node name=""Sprite2"" type=""Sprite""]
",

@"using GodotAnalysers;

public partial class D : Sprite
{
    protected DependencyInjectorContext di { get; private set; }
    protected virtual void FillMembers()
    {
        this.di = DependencyInjector.GetNewContext();
    }
}");
        }

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
    protected DependencyInjectorContext di { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite = this.GetNode<Sprite>(""./Sprite"");
        this.collisionShape2D = this.GetNode<CollisionShape2D>(""./CollisionShape2D"");
        this.customTextPopup = this.GetNode<CustomTextPopup>(""./Sprite/CustomTextPopup"");
        this.customTextPopup1 = this.GetNode<CustomTextPopup>(""./Sprite/CustomTextPopup1"");
        this.di = DependencyInjector.GetNewContext();
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
    protected DependencyInjectorContext di { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite2 = this.GetNode<Sprite>(""./Sprite2"");
        this.di = DependencyInjector.GetNewContext();
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
    protected DependencyInjectorContext di { get; private set; }
    protected virtual void FillMembers()
    {
        this.sprite2 = this.GetNode<Sprite>(""./HUD/BottomButonsMargin/BottomButtonsContainer/Sprite2"");
        this.di = DependencyInjector.GetNewContext();
    }
}");
        }


        [Test]
        public void SceneTestWithInnerClass()
        {
                        DoTest(@"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D {
    public class Inner{

    }
}
",

@"D.txt",

@"
[gd_scene load_steps=5 format=2]
[node name=""Sprite2"" type=""Sprite""]
",

@"using GodotAnalysers;

public partial class D : Sprite
{
    protected DependencyInjectorContext di { get; private set; }
    protected virtual void FillMembers()
    {
        this.di = DependencyInjector.GetNewContext();
    }
}");
        }

        [Test]
        public void aa()
        {
                        DoTest(@"
using GodotAnalysers;
[SceneReference(""D.txt"")]
public partial class D {
}
",

@"D.txt",

@"
[gd_scene load_steps=5 format=2]

[ext_resource path=""res://Presentation/Construction.tscn"" type=""PackedScene"" id=1]
[ext_resource path=""res://Presentation/ArtificialWell.cs"" type=""Script"" id=2]

[sub_resource type=""Gradient"" id=1]
colors = PoolColorArray( 1, 1, 1, 1, 0, 0.160784, 1, 1 )

[sub_resource type=""GradientTexture"" id=2]
gradient = SubResource( 1 )
width = 8

[node name=""ArtificialWell"" instance=ExtResource( 1 )]
script = ExtResource( 2 )

[node name=""Sprite"" parent=""."" index=""0""]
scale = Vector2( 4, 16 )

[node name=""Sprite1"" type=""Sprite"" parent=""Sprite"" index=""0""]
visible = false
scale = Vector2( 3, 2.625 )
texture = SubResource( 2 )

[node name=""Label1"" type=""Label"" parent=""Label"" index=""0""]
margin_left = 20.0
margin_top = 26.0
margin_right = 60.0
margin_bottom = 40.0

",

@"using GodotAnalysers;
 
public partial class D : Construction
{
    protected Sprite sprite1 { get; private set; }
    protected Label label1 { get; private set; }
    protected DependencyInjectorContext di { get; private set; }
    
    protected virtual void FillMembers()
    {
        this.sprite1 = this.GetNode<Sprite>(""./Sprite/Sprite1"");
        this.label1 = this.GetNode<Label>(""./Label/Label1"");
        this.di = DependencyInjector.GetNewContext();
    } 
}");
        }

        public static void DoTest(string sourceText, string fileName, string fileContent, string resultText)
        {
            var generator = new SceneFieldsGenerator();
            generator.fileReader = new FakeFileReader(fileName, fileContent);
            TestHelper.DoTest(generator, sourceText, resultText);
        }
    }
}

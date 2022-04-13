# GodotFieldsGenerator

C# fields generator for godot scenes.

Usage:
- Add references to GodotFieldsGenerator and GodotFieldsGeneratorAttributes to your godot project.
- Change scene root script class to be public partial
- Add [SceneReference("pathTo/scene.tscn")] attribute with relative path to scene file (including .tscn)
- Add this.FillMembers() method call to "_Ready" method
- Now you can use local class variables for scene nodes.

Example:

using System;

namespace GodotAnalysers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SceneReferenceAttribute : Attribute
    {
        public SceneReferenceAttribute(string scenePath)
        {
            this.ScenePath = scenePath;
        }

        public string ScenePath { get; }
    }
}

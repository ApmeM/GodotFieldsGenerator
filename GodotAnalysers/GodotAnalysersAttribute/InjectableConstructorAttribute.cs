using System;

namespace Godot
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InjectableConstructorAttribute : Attribute
    {
        public InjectableConstructorAttribute()
        {
        }
    }
}

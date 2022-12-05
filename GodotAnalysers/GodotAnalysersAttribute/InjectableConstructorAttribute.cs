using System;

namespace GodotAnalysers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InjectableConstructorAttribute : Attribute
    {
        public InjectableConstructorAttribute()
        {
        }
    }
}

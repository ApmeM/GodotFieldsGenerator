using System;

namespace Godot
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InjectableAttribute : Attribute
    {
        public InjectableAttribute(bool onDemand)
        {
            this.OnDemand = onDemand;
        }

        public bool OnDemand { get; }
    }
}

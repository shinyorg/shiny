using System;


namespace Shiny.Generators
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ShinyInjectAttribute : Attribute
    {
        public ShinyInjectAttribute(Type interfaceType, Type implementationType) {}
    }
}

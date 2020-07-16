using System;


namespace Shiny.Generators
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ShinyInjectAttribute : Attribute
    {
        public ShinyInjectAttribute(Type interfaceType, Type implementationType) {}
    }
}

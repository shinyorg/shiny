using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class GenerateStaticClassesAttribute : Attribute
    {
        public GenerateStaticClassesAttribute(string? nameSpace = null) { }
    }
}

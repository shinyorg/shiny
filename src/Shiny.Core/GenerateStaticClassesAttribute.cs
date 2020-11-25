using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class GenerateStaticClassesAttribute : Attribute
    {
        public GenerateStaticClassesAttribute(string nameSpace) { }
    }
}

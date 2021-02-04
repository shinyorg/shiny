using System;


namespace Shiny.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class StaticGenerationAttribute : Attribute
    {
        // library name is pulled from raw assembly name
        public StaticGenerationAttribute(string interfaceName, string staticName)
        {
        }
    }
}

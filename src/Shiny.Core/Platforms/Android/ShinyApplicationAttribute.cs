using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ShinyApplicationAttribute : Attribute
    {
        public ShinyApplicationAttribute(bool autoGenerateShinyStartup) { }
    }
}

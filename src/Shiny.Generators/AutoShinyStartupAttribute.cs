using System;


namespace Shiny.Generators
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class AutoShinyStartupAttribute : Attribute
    {
        public AutoShinyStartupAttribute(bool generateStaticReferences) {}
    }
}

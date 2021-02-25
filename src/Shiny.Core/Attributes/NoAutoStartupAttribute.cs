using System;


namespace Shiny.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class NoAutoStartupAttribute : Attribute
    {
        public NoAutoStartupAttribute(string instructions) { }
    }
}

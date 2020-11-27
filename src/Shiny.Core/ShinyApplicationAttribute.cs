using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ShinyApplicationAttribute : Attribute
    {

        public string? ShinyStartupTypeName { get; set; }
        public string? XamarinFormsAppTypeName { get; set; }
    }
}

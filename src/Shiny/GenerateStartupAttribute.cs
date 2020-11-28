using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class GenerateStartupAttribute : Attribute
    {
        public string? TypeName { get; set; }
        public bool ExcludeUserModules { get; set; }
        public bool ExcludeJobs { get; set; }

    }
}

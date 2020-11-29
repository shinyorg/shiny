using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ShinyApplicationAttribute : Attribute
    {

        /// <summary>
        /// If this is not set, a shiny startup will be generated within this project
        /// </summary>
        public string? ShinyStartupTypeName { get; set; }


        /// <summary>
        /// IF this is set, the symbol will be scanned for in the current assembly and all of its references
        /// </summary>
        public string? XamarinFormsAppTypeName { get; set; }


        /// <summary>
        /// Setting this to true will skip scanning for modules to install when a shiny startup file is being generated
        /// </summary>
        public bool ExcludeShinyUserModules { get; set; }


        /// <summary>
        /// Setting this to true will skip scanning for jobs to register when a shiny startup file is being generated
        /// </summary>
        public bool ExcludeShinyUserJobs { get; set; }
    }
}
/*
 * Can inherit forms appdelegate or mvx... whatever
public partial class MyShinyAppDelegate
{
    ... use delegate type name from
    ... wire in normal stuff
}
*/
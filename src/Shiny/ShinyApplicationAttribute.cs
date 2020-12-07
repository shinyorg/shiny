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
        public bool ExcludeModules { get; set; }


        /// <summary>
        /// Setting this to true will skip scanning for jobs to register when a shiny startup file is being generated
        /// </summary>
        public bool ExcludeJobs { get; set; }


        /// <summary>
        /// Setting this to true will skip scanning for startup tasks to register when a shiny file is being generated
        /// </summary>
        public bool ExcludeStartupTasks { get; set; }


        /// <summary>
        /// Will exclude scanning for classes marked with [Shiny.ShinyServiceAttribute]
        /// </summary>
        public bool ExcludeServices { get; set; }
    }
}
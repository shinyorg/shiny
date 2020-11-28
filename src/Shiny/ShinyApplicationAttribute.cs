using System;


namespace Shiny
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ShinyApplicationAttribute : Attribute
    {

        /// <summary>
        /// IF not set, the current assembly will be checked for GenerateStartupAttribute and used if that's the case
        /// </summary>
        public string? ShinyStartupTypeName { get; set; }


        /// <summary>
        /// IF this is set, the symbol will be scanned for in the current assembly and all of its references
        /// </summary>
        public string? XamarinFormsAppTypeName { get; set; }
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
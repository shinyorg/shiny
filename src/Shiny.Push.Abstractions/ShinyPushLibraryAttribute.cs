using System;


namespace Shiny.Push
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ShinyPushLibraryAttribute : Attribute
    {
        public ShinyPushLibraryAttribute() { }
    }
}

using System;


namespace Shiny.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class AutoStartupAttribute : Attribute
    {
        public AutoStartupAttribute(string startupRegistrationServiceExtensionMethodName) { }
    }
}

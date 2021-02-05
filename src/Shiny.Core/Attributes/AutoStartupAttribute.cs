using System;


namespace Shiny.Attributes
{
    //[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    //public class NoAutoStartupAttribute : Attribute
    //{
    //    public AutoStartupAttribute(string startupRegistrationServiceExtensionMethodName) { }
    //}


    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class AutoStartupAttribute : Attribute
    {
        public AutoStartupAttribute(string startupRegistrationServiceExtensionMethodName) { }
    }


    // TODO: IsPushLibrary, UsesBackgroundTransfer
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class AutoStartupWithDelegateAttribute : Attribute
    {
        // if no delegates and required, don't register
        public AutoStartupWithDelegateAttribute(string delegateTypeName, string startupRegistrationServiceExtensionMethodName, bool delegateRequired) { }
    }
}

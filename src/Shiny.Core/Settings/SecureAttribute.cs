using System;


namespace Shiny.Settings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SecureAttribute : Attribute
    {
    }
}

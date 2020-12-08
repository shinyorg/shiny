using System;


namespace Shiny
{
    /// <summary>
    /// This is used for registering services with shiny when your startup class is being generated
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ShinyServiceAttribute : Attribute
    {
    }
}

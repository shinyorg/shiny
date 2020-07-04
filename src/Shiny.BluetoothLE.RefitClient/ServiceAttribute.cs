using System;


namespace Shiny.BluetoothLE.RefitClient
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string uuid) => this.Uuid = uuid;
        public string Uuid { get; }
    }
}

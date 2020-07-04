using System;


namespace Shiny.BluetoothLE.Hosting.Hubs
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ServiceAttribute : Attribute
    {
        public ServiceAttribute(string uuid, bool isPrimary = false)
        {
            this.Uuid = uuid;
            this.IsPrimary = isPrimary;
        }


        public string Uuid { get; }
        public bool IsPrimary { get; set; }
    }
}

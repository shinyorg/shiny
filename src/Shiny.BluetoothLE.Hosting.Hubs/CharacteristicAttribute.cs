using System;


namespace Shiny.BluetoothLE.Hosting.Hubs
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class CharacteristicAttribute : Attribute
    {
        public CharacteristicAttribute(string uuid)
        {
            this.Uuid = uuid;
        }


        public string Uuid { get; }
    }
}

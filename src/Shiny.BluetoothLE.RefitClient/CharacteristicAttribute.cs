using System;


namespace Shiny.BluetoothLE.RefitClient
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CharacteristicAttribute : Attribute
    {
        public CharacteristicAttribute(string serviceUuid, string uuid) 
        {
            this.ServiceUuid = serviceUuid;
            this.Uuid = uuid;
        }


        public string ServiceUuid { get; }
        public string Uuid { get; }
    }
}

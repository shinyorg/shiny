using System;


namespace Shiny.BluetoothLE.RefitClient
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class CharacteristicAttribute : Attribute
    {
        public CharacteristicAttribute(string uuid) => this.Uuid = uuid;
        public string Uuid { get; }
    }
}

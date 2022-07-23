using System;


namespace Shiny.BluetoothLE.Hosting.Managed;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class BleGattServiceAttribute : Attribute
{
    public BleGattServiceAttribute(string serviceUuid, string characteristicUuid)
    {
        this.ServiceUuid = serviceUuid;
        this.CharacteristicUuid = characteristicUuid;
    }


    public string ServiceUuid { get; }
    public string CharacteristicUuid { get; }
    public bool Secure { get; set; }
}

//typeof(Test1).GetMethod("TestMe").DeclaringType == typeof(Test1)
//public static bool IsOverride(MethodInfo m) {
//return m.GetBaseDefinition().DeclaringType != m.DeclaringType;
//}

[BleGattService("", "")]
public class Test : BleGattService
{
    public Test()
    {
        // TODO: DI should work here... careful of foreground
    }
}


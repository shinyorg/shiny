using System;


namespace Shiny.BluetoothLE.Hosting.Managed;




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


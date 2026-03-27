using System;

namespace Shiny.BluetoothLE.Hosting;


static class UuidHelper
{
    public static Guid ToUuid(string value)
    {
        if (value.Length == 4)
            value = $"0000{value}-0000-1000-8000-00805F9B34FB";

        return Guid.Parse(value);
    }
}

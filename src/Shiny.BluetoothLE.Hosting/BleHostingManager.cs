#if PLATFORM
using System;
#if APPLE
using CoreBluetooth;
#endif
#if ANDROID
using Java.Util;
#endif

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager
{
    static bool IsValidUuid(string value)
    {
        if (value.IsEmpty())
            return false;

#if APPLE
        try
        {
            CBUUID.FromString(value);
            return true;
        }
        catch
        {
            return false;
        }
#elif ANDROID
        try
        {
            UUID.FromString(value);
            return true;
        }
        catch
        {
            return false;
        }
#else
        return Guid.TryParse(value, out _);
#endif
    }
}
#endif

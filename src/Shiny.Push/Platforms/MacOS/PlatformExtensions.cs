using System;
using Foundation;

namespace Shiny.Push;


public static class PlatformExtensions
{
    public static string ToPushTokenString(this NSData deviceToken)
    {
        string token = null!;
        if (deviceToken.Length > 0)
        {
            var data = deviceToken.ToArray();
            token = BitConverter
                .ToString(data)
                .Replace("-", "")
                .Replace("\"", "");
        }
        return token;
    }
}

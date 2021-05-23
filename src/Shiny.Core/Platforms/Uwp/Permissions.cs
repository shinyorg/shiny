using System;
using System.IO;


namespace Shiny
{
    public static class Permissions
    {
        static readonly object syncLock = new object();
        static string manifestContent;
        static string GetManifest()
        {
            if (manifestContent == null)
            {
                lock (syncLock)
                {
                    if (manifestContent == null)
                    {
                        manifestContent = File.ReadAllText("AppxManifest.xml");
                    }
                }
            }
            return manifestContent;
        }


        public static bool IsInMainfest(string permission)
        {
            var m = GetManifest();
            var s = $"<DeviceCapability Identifier=\"{permission}\"";
            var r = m.Contains(s);
            return r;
        }
    }
}
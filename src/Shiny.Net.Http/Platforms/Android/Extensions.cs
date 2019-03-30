using System;
using System.IO;


namespace Shiny.Net.Http
{
    public static class Extensions
    {
        public static Android.Net.Uri ToNativeUri(this FileInfo file)
        {
            var native = new Java.IO.File(file.FullName);
            return Android.Net.Uri.FromFile(native);
        }



    }
}

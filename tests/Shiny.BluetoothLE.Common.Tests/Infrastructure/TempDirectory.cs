using System;
using System.IO;

namespace Shiny.BluetoothLE.Common.Tests.Infrastructure;


/// <summary>
/// A scratch directory that cleans itself up.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "shiny-l2cap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.Path);
    }


    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(this.Path, name);


    public string WriteFile(string name, byte[] content)
    {
        var path = this.File(name);
        System.IO.File.WriteAllBytes(path, content);
        return path;
    }


    public static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        new Random(length).NextBytes(bytes);
        return bytes;
    }


    public void Dispose()
    {
        try { Directory.Delete(this.Path, true); } catch { /* best effort */ }
    }
}

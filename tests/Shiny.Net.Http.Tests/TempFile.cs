using System;
using System.IO;

namespace Shiny.Net.Http.Tests;


/// <summary>
/// Disposable temp-file helper for transfer-builder tests that need a real file on disk
/// (the AWS / Azure builders both <c>new FileInfo(path).Exists</c> at build time).
/// </summary>
internal sealed class TempFile : IDisposable
{
    public string Path { get; }
    public long Length { get; }

    public TempFile(int bytes = 1024)
    {
        this.Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"shiny-test-{Guid.NewGuid():N}.bin");
        var data = new byte[bytes];
        new Random(42).NextBytes(data);
        File.WriteAllBytes(this.Path, data);
        this.Length = bytes;
    }

    public void Dispose()
    {
        try { File.Delete(this.Path); }
        catch { /* best effort */ }
    }
}

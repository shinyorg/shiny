using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Shiny.Notifications.Tests;


public class NotificationImagesTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "shiny-notification-images-tests", Guid.NewGuid().ToString("N"));
    string Store => Path.Combine(this.root, "store");

    public void Dispose()
    {
        if (Directory.Exists(this.root))
            Directory.Delete(this.root, true);
    }


    [Fact]
    public async Task LocalPath_IsCopied_AndSourceIsKept()
    {
        var source = this.CreateFile("source.PNG", 16);
        var path = await NotificationImages.TryStore(source, this.Store, "7", NullLogger.Instance);

        Assert.Equal(Path.Combine(this.Store, "7.png"), path);
        Assert.True(File.Exists(path));
        Assert.True(File.Exists(source));
    }


    [Fact]
    public async Task FileUri_IsCopied()
    {
        var source = this.CreateFile("source.jpg", 16);
        var path = await NotificationImages.TryStore(new Uri(source).AbsoluteUri, this.Store, "8", NullLogger.Instance);
        Assert.Equal(Path.Combine(this.Store, "8.jpg"), path);
    }


    [Fact]
    public async Task MissingFile_ReturnsNull()
        => Assert.Null(await NotificationImages.TryStore(Path.Combine(this.root, "nope.png"), this.Store, "9", NullLogger.Instance));


    [Fact]
    public async Task OversizedFile_ReturnsNull()
    {
        var source = this.CreateFile("big.png", NotificationImages.MaxBytes + 1);
        Assert.Null(await NotificationImages.TryStore(source, this.Store, "10", NullLogger.Instance));
    }


    [Fact]
    public async Task StoringAgain_ReplacesThePreviousImage()
    {
        await NotificationImages.TryStore(this.CreateFile("a.png", 4), this.Store, "11", NullLogger.Instance);
        await NotificationImages.TryStore(this.CreateFile("b.gif", 4), this.Store, "11", NullLogger.Instance);

        Assert.Equal([Path.Combine(this.Store, "11.gif")], Directory.GetFiles(this.Store));
        Assert.Equal(Path.Combine(this.Store, "11.gif"), NotificationImages.Find(this.Store, "11"));
    }


    [Fact]
    public async Task Delete_RemovesOnlyThatId()
    {
        await NotificationImages.TryStore(this.CreateFile("a.png", 4), this.Store, "1", NullLogger.Instance);
        await NotificationImages.TryStore(this.CreateFile("b.png", 4), this.Store, "12", NullLogger.Instance);

        NotificationImages.Delete(this.Store, "1");

        Assert.Null(NotificationImages.Find(this.Store, "1"));
        Assert.NotNull(NotificationImages.Find(this.Store, "12"));
    }


    [Fact]
    public async Task Download_UsesContentTypeForExtension()
    {
        using var server = new OneShotServer("200 OK", "image/png", [1, 2, 3]);
        var path = await NotificationImages.TryStore(server.Url + "image", this.Store, "20", NullLogger.Instance);

        Assert.Equal(Path.Combine(this.Store, "20.png"), path);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(path!));
    }


    [Fact]
    public async Task Download_FallsBackToUrlExtension()
    {
        using var server = new OneShotServer("200 OK", "application/octet-stream", [1]);
        var path = await NotificationImages.TryStore(server.Url + "photo.JPG", this.Store, "21", NullLogger.Instance);
        Assert.Equal(Path.Combine(this.Store, "21.jpg"), path);
    }


    [Fact]
    public async Task Download_HttpError_ReturnsNull()
    {
        using var server = new OneShotServer("404 Not Found", "text/plain", [1]);
        Assert.Null(await NotificationImages.TryStore(server.Url + "missing.png", this.Store, "22", NullLogger.Instance));
    }


    [Fact]
    public async Task Download_UnknownType_ReturnsNull()
    {
        using var server = new OneShotServer("200 OK", "application/octet-stream", [1]);
        Assert.Null(await NotificationImages.TryStore(server.Url + "image", this.Store, "23", NullLogger.Instance));
    }


    [Fact]
    public void TryToNative_CopiesImageUri()
    {
        var native = new Notification { Id = 1, ImageUri = "https://example.com/a.png" }.TryToNative<TestNotification>();
        Assert.Equal("https://example.com/a.png", native.ImageUri);
    }


    public class TestNotification : Notification;


    string CreateFile(string name, long length)
    {
        Directory.CreateDirectory(this.root);
        var path = Path.Combine(this.root, name);
        using var fs = File.Create(path);
        fs.SetLength(length);
        return path;
    }


    // a single-response HTTP/1.1 server on a loopback port - enough for one GET
    sealed class OneShotServer : IDisposable
    {
        readonly TcpListener listener = new(IPAddress.Loopback, 0);
        readonly Task serving;

        public OneShotServer(string status, string contentType, byte[] body)
        {
            this.listener.Start();
            this.Url = $"http://127.0.0.1:{((IPEndPoint)this.listener.LocalEndpoint).Port}/";
            this.serving = Task.Run(async () =>
            {
                using var client = await this.listener.AcceptTcpClientAsync();
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                await stream.ReadAsync(buffer);

                var header = System.Text.Encoding.ASCII.GetBytes(
                    $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n"
                );
                await stream.WriteAsync(header);
                await stream.WriteAsync(body);
            });
        }

        public string Url { get; }

        public void Dispose() => this.listener.Stop();
    }
}

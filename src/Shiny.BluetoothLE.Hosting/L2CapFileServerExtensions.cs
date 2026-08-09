using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Turns an L2CAP PSM into a file server so centrals can push files to, and pull files from, this device.
/// </summary>
public static class L2CapFileServerExtensions
{
    /// <summary>
    /// Publishes a PSM that serves files out of <paramref name="rootDirectory"/>.
    /// </summary>
    /// <param name="hosting">The hosting manager.</param>
    /// <param name="rootDirectory">Directory downloads are read from and uploads are written to. Created if missing.</param>
    /// <param name="secure">When true, requires an encrypted/authenticated channel (Android API 29+).</param>
    /// <param name="configure">Optional hook to adjust limits, authorization, and progress callbacks.</param>
    /// <returns>A handle carrying the assigned PSM. Dispose to unpublish and drop connected peers.</returns>
    public static Task<L2CapInstance> OpenL2CapFileServer(
        this IBleHostingManager hosting,
        string rootDirectory,
        bool secure = false,
        Action<L2CapFileServerOptions>? configure = null
    )
    {
        var options = new L2CapFileServerOptions(rootDirectory) { Secure = secure };
        configure?.Invoke(options);
        return hosting.OpenL2CapFileServer(options);
    }


    /// <summary>
    /// Publishes a PSM that serves files according to <paramref name="options"/>.
    /// </summary>
    /// <param name="hosting">The hosting manager.</param>
    /// <param name="options">The server configuration.</param>
    /// <returns>A handle carrying the assigned PSM. Dispose to unpublish and drop connected peers.</returns>
    /// <remarks>
    /// Requested file names are resolved under <see cref="L2CapFileServerOptions.RootDirectory"/>;
    /// absolute paths and anything traversing out of the root are refused.
    /// </remarks>
    public static Task<L2CapInstance> OpenL2CapFileServer(this IBleHostingManager hosting, L2CapFileServerOptions options)
    {
        if (hosting == null) throw new ArgumentNullException(nameof(hosting));
        if (options == null) throw new ArgumentNullException(nameof(options));

        Directory.CreateDirectory(options.RootDirectory);

        return hosting.HandleL2CapRequests(
            options.Secure,
            (request, ct) => Handle(request, options, ct),
            options.Transfer,
            (request, ex) => options.OnError?.Invoke(request, ex)
        );
    }


    /// <summary>
    /// Publishes a PSM and hands every inbound transfer request to <paramref name="onRequest"/>.
    /// Use this when the built-in directory server is not the shape you need.
    /// </summary>
    /// <param name="hosting">The hosting manager.</param>
    /// <param name="secure">When true, requires an encrypted/authenticated channel (Android API 29+).</param>
    /// <param name="onRequest">
    /// Handler for each request. It must answer with one of the accept methods or
    /// <see cref="L2CapFileRequest.Reject"/> before returning; requests are served one at a time per peer.
    /// </param>
    /// <param name="options">Optional buffer/interval/timeout tuning passed to each transfer.</param>
    /// <param name="onError">Optional error hook. Called with a null request when the failure was not tied to one.</param>
    /// <returns>A handle carrying the assigned PSM. Dispose to unpublish and drop connected peers.</returns>
    public static async Task<L2CapInstance> HandleL2CapRequests(
        this IBleHostingManager hosting,
        bool secure,
        Func<L2CapFileRequest, CancellationToken, Task> onRequest,
        L2CapTransferOptions? options = null,
        Action<L2CapFileRequest?, Exception>? onError = null
    )
    {
        if (hosting == null) throw new ArgumentNullException(nameof(hosting));
        if (onRequest == null) throw new ArgumentNullException(nameof(onRequest));

        var transfer = options ?? L2CapTransferOptions.Default;
        var cts = new CancellationTokenSource();

        var instance = await hosting
            .OpenL2Cap(secure, channel => _ = Serve(channel, onRequest, transfer, onError, cts.Token))
            .ConfigureAwait(false);

        return new L2CapInstance(
            instance.Psm,
            () =>
            {
                cts.Cancel();
                instance.Dispose();
            }
        );
    }


    static async Task Serve(
        L2CapChannel channel,
        Func<L2CapFileRequest, CancellationToken, Task> onRequest,
        L2CapTransferOptions options,
        Action<L2CapFileRequest?, Exception>? onError,
        CancellationToken cancellationToken
    )
    {
        L2CapFileRequest? current = null;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                current = await channel.ReadFileRequest(options, cancellationToken).ConfigureAwait(false);
                if (current == null)
                    break; // peer closed the channel

                await onRequest(current, cancellationToken).ConfigureAwait(false);

                if (!current.IsAnswered)
                {
                    // an unanswered request leaves the peer hanging and the channel unusable
                    await current.Reject(L2CapTransferError.Unknown, "Request was not handled", cancellationToken).ConfigureAwait(false);
                }
                current = null;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            onError?.Invoke(current, ex);
        }
        finally
        {
            channel.Dispose();
        }
    }


    static async Task Handle(L2CapFileRequest request, L2CapFileServerOptions options, CancellationToken cancellationToken)
    {
        var path = Resolve(options.RootDirectory, request.FileName);
        if (path == null)
        {
            await request.Reject(L2CapTransferError.NotPermitted, "Invalid file name", cancellationToken).ConfigureAwait(false);
            return;
        }

        var upload = request.Type == L2CapTransferType.Upload;

        if (upload && !options.AllowUploads)
        {
            await request.Reject(L2CapTransferError.NotPermitted, "Uploads are disabled", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!upload && !options.AllowDownloads)
        {
            await request.Reject(L2CapTransferError.NotPermitted, "Downloads are disabled", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (upload && options.MaxUploadSize != null && request.Size > options.MaxUploadSize.Value)
        {
            await request.Reject(L2CapTransferError.TooLarge, $"Uploads are limited to {options.MaxUploadSize.Value} bytes", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (upload && !options.OverwriteExistingUploads && File.Exists(path))
        {
            await request.Reject(L2CapTransferError.NotPermitted, "File already exists", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!upload && !File.Exists(path))
        {
            await request.Reject(L2CapTransferError.NotFound, "File not found", cancellationToken).ConfigureAwait(false);
            return;
        }

        if (options.Authorize?.Invoke(request) == false)
        {
            await request.Reject(L2CapTransferError.NotPermitted, "Request was not authorized", cancellationToken).ConfigureAwait(false);
            return;
        }

        void OnProgress(TransferProgress progress) => options.OnProgress?.Invoke(
            new L2CapFileTransferEvent(request.PeerIdentifier, request.Type, request.FileName, progress)
        );

        try
        {
            var result = upload
                ? await request.AcceptUpload(path, OnProgress, cancellationToken).ConfigureAwait(false)
                : await request.AcceptDownload(path, OnProgress, cancellationToken).ConfigureAwait(false);

            options.OnCompleted?.Invoke(new L2CapFileServerResult(request.PeerIdentifier, path, result));
        }
        catch (Exception ex) when (!request.IsAnswered)
        {
            // failed before we committed to the transfer (couldn't open the file, etc) - the channel
            // is still in sync, so refuse this request and keep serving the peer
            options.OnError?.Invoke(request, ex);
            await request.Reject(L2CapTransferError.IoError, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Maps a peer supplied name onto a path inside the root, or null when it escapes the root.
    /// </summary>
    static string? Resolve(string rootDirectory, string fileName)
    {
        if (String.IsNullOrWhiteSpace(fileName))
            return null;

        if (fileName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            return null;

        string root, full;
        try
        {
            root = Path.GetFullPath(rootDirectory);
            full = Path.GetFullPath(Path.Combine(root, fileName));
        }
        catch (Exception)
        {
            return null;
        }

        // Path.Combine hands back a rooted second argument verbatim, and ".." can climb out -
        // both land outside the root and are caught here
        var prefix = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        return full.StartsWith(prefix, StringComparison.Ordinal) ? full : null;
    }
}

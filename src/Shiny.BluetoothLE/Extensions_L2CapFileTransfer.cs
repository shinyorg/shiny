using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// One-shot file transfers to/from a peripheral that has published an L2CAP file server PSM
/// (see <c>IBleHostingManager.OpenL2CapFileServer</c>).
/// </summary>
/// <remarks>
/// Each of these opens a channel, runs the transfer, and closes the channel again. To move several
/// files over one channel, open it yourself with <see cref="FeatureL2Cap.TryOpenL2CapChannel"/> and
/// use the <see cref="L2CapFileTransferExtensions"/> methods directly.
/// </remarks>
public static class PeripheralL2CapFileTransferExtensions
{
    /// <summary>
    /// Uploads a local file to the peripheral over a freshly opened L2CAP channel.
    /// </summary>
    /// <param name="peripheral">A connected peripheral whose platform supports L2CAP.</param>
    /// <param name="psm">The PSM the peripheral published its file server on.</param>
    /// <param name="localFilePath">Path of the file to send.</param>
    /// <param name="remoteFileName">Name to store it under on the peripheral. Defaults to the local file name.</param>
    /// <param name="secure">On Android, requests an encrypted/authenticated channel (API 29+). Ignored on Apple platforms.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="NotSupportedException">The platform does not support L2CAP.</exception>
    /// <exception cref="L2CapTransferException">The peripheral refused the file or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> UploadFile(
        this IPeripheral peripheral,
        ushort psm,
        string localFilePath,
        string? remoteFileName = null,
        bool secure = false,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        using var channel = await peripheral
            .OpenL2CapChannelAsync(psm, secure, cancellationToken)
            .ConfigureAwait(false);

        return await channel
            .UploadFile(localFilePath, remoteFileName, onProgress, options, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Downloads a file from the peripheral over a freshly opened L2CAP channel.
    /// </summary>
    /// <param name="peripheral">A connected peripheral whose platform supports L2CAP.</param>
    /// <param name="psm">The PSM the peripheral published its file server on.</param>
    /// <param name="remoteFileName">Name of the file to fetch, as the peripheral knows it.</param>
    /// <param name="localFilePath">Path to write to. Created (or overwritten) and truncated to the received length.</param>
    /// <param name="secure">On Android, requests an encrypted/authenticated channel (API 29+). Ignored on Apple platforms.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="NotSupportedException">The platform does not support L2CAP.</exception>
    /// <exception cref="L2CapTransferException">The peripheral does not have the file, refused it, or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> DownloadFile(
        this IPeripheral peripheral,
        ushort psm,
        string remoteFileName,
        string localFilePath,
        bool secure = false,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        using var channel = await peripheral
            .OpenL2CapChannelAsync(psm, secure, cancellationToken)
            .ConfigureAwait(false);

        return await channel
            .DownloadFile(remoteFileName, localFilePath, onProgress, options, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Rx flavour of <see cref="UploadFile"/>. Emits progress as the transfer runs and a final 100% event
    /// before completing. Disposing the subscription cancels the transfer and closes the channel.
    /// </summary>
    /// <param name="peripheral">A connected peripheral whose platform supports L2CAP.</param>
    /// <param name="psm">The PSM the peripheral published its file server on.</param>
    /// <param name="localFilePath">Path of the file to send.</param>
    /// <param name="remoteFileName">Name to store it under on the peripheral. Defaults to the local file name.</param>
    /// <param name="secure">On Android, requests an encrypted/authenticated channel (API 29+). Ignored on Apple platforms.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    public static IObservable<TransferProgress> UploadFileWithProgress(
        this IPeripheral peripheral,
        ushort psm,
        string localFilePath,
        string? remoteFileName = null,
        bool secure = false,
        L2CapTransferOptions? options = null
    ) => Observable.Create<TransferProgress>(async (ob, ct) =>
    {
        await peripheral
            .UploadFile(psm, localFilePath, remoteFileName, secure, ob.OnNext, options, ct)
            .ConfigureAwait(false);
    });


    /// <summary>
    /// Rx flavour of <see cref="DownloadFile"/>. Emits progress as the transfer runs and a final 100% event
    /// before completing. Disposing the subscription cancels the transfer and closes the channel.
    /// </summary>
    /// <param name="peripheral">A connected peripheral whose platform supports L2CAP.</param>
    /// <param name="psm">The PSM the peripheral published its file server on.</param>
    /// <param name="remoteFileName">Name of the file to fetch, as the peripheral knows it.</param>
    /// <param name="localFilePath">Path to write to.</param>
    /// <param name="secure">On Android, requests an encrypted/authenticated channel (API 29+). Ignored on Apple platforms.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    public static IObservable<TransferProgress> DownloadFileWithProgress(
        this IPeripheral peripheral,
        ushort psm,
        string remoteFileName,
        string localFilePath,
        bool secure = false,
        L2CapTransferOptions? options = null
    ) => Observable.Create<TransferProgress>(async (ob, ct) =>
    {
        await peripheral
            .DownloadFile(psm, remoteFileName, localFilePath, secure, ob.OnNext, options, ct)
            .ConfigureAwait(false);
    });


    /// <summary>
    /// Awaitable form of <see cref="ICanL2Cap.OpenL2CapChannel"/> that fails loudly on platforms without L2CAP.
    /// </summary>
    /// <param name="peripheral">A connected peripheral whose platform supports L2CAP.</param>
    /// <param name="psm">The PSM advertised by the peripheral.</param>
    /// <param name="secure">On Android, requests an encrypted/authenticated channel (API 29+). Ignored on Apple platforms.</param>
    /// <param name="cancellationToken">Token used to cancel the open.</param>
    /// <exception cref="NotSupportedException">The platform does not support L2CAP.</exception>
    public static Task<L2CapChannel> OpenL2CapChannelAsync(
        this IPeripheral peripheral,
        ushort psm,
        bool secure = false,
        CancellationToken cancellationToken = default
    )
    {
        if (peripheral == null)
            throw new ArgumentNullException(nameof(peripheral));

        if (peripheral is not ICanL2Cap l2cap)
            throw new NotSupportedException($"{peripheral.GetType().Name} does not support L2CAP channels on this platform");

        return l2cap
            .OpenL2CapChannel(psm, secure)
            .Take(1)
            .ToTask(cancellationToken);
    }
}

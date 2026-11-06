namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// How far a firmware upgrade goes after the upload.
/// </summary>
public enum FirmwareUpgradeMode
{
    /// <summary>
    /// Upload, mark for test, reset, reconnect, then confirm the new image once it runs. If the new firmware never
    /// comes up, MCUboot reverts to the old one on the next reset. The safe default.
    /// </summary>
    TestAndConfirm,

    /// <summary>
    /// Upload, mark for test and reset. The new firmware must confirm itself (or the app calls
    /// <see cref="McuMgrImages.ConfirmRunningAsync"/>), otherwise MCUboot reverts on the next reset.
    /// </summary>
    TestOnly,

    /// <summary>
    /// Upload, mark as permanent and reset. There is no way back if the new firmware cannot run.
    /// </summary>
    ConfirmOnly,

    /// <summary>
    /// Upload only - nothing is swapped.
    /// </summary>
    UploadOnly
}


/// <summary>
/// The stage a firmware upgrade is in.
/// </summary>
public enum FirmwareUpgradeStage
{
    /// <summary>Reading the device's image state.</summary>
    Validating,
    /// <summary>Uploading an image.</summary>
    Uploading,
    /// <summary>Marking the uploaded images for test (or confirming them in <see cref="FirmwareUpgradeMode.ConfirmOnly"/>).</summary>
    Testing,
    /// <summary>Resetting the device.</summary>
    Resetting,
    /// <summary>Waiting for the swap and reconnecting.</summary>
    Reconnecting,
    /// <summary>Confirming the new image.</summary>
    Confirming,
    /// <summary>Done.</summary>
    Completed
}


/// <summary>
/// Options for <see cref="McuMgrClient.UpgradeFirmwareAsync(IReadOnlyList{FirmwareImage}, FirmwareUpgradeOptions?, IProgress{FirmwareUpgradeProgress}?, CancellationToken)"/>.
/// </summary>
public sealed class FirmwareUpgradeOptions : McuMgrUploadOptions
{
    /// <summary>The upgrade mode. Default <see cref="FirmwareUpgradeMode.TestAndConfirm"/>.</summary>
    public FirmwareUpgradeMode Mode { get; set; } = FirmwareUpgradeMode.TestAndConfirm;

    /// <summary>
    /// How long to wait after the reset before reconnecting, while MCUboot swaps the images. Large images on slow
    /// flash can take 20 seconds or more. Default 10 seconds.
    /// </summary>
    public TimeSpan EstimatedSwapTime { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How long to keep trying to reconnect after the swap. Default 60 seconds.</summary>
    public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Ask the device to reject an image that is not newer than the one it runs. Default false.</summary>
    public bool UpgradeOnlyIfNewer { get; set; }
}


/// <summary>
/// Progress of a firmware upgrade.
/// </summary>
/// <param name="Stage">The current stage.</param>
/// <param name="ImageIndex">The image being uploaded during <see cref="FirmwareUpgradeStage.Uploading"/>.</param>
/// <param name="Upload">Upload progress during <see cref="FirmwareUpgradeStage.Uploading"/>.</param>
public record FirmwareUpgradeProgress(
    FirmwareUpgradeStage Stage,
    int? ImageIndex = null,
    TransferProgress? Upload = null
);


/// <summary>
/// The outcome of a firmware upgrade.
/// </summary>
/// <param name="UploadedImages">The image numbers that were uploaded.</param>
/// <param name="SkippedImages">The image numbers that were skipped because the device already runs (or holds) them.</param>
/// <param name="DeviceReset">True when the device was reset to swap the images in.</param>
public record FirmwareUpgradeResult(
    IReadOnlyList<int> UploadedImages,
    IReadOnlyList<int> SkippedImages,
    bool DeviceReset
)
{
    /// <summary>True when every image was already running, so nothing changed.</summary>
    public bool AlreadyUpToDate => this.UploadedImages.Count == 0 && !this.DeviceReset;
}


sealed class FirmwareUpgrader(McuMgrClient client, FirmwareUpgradeOptions options, IProgress<FirmwareUpgradeProgress>? progress)
{
    public async Task<FirmwareUpgradeResult> RunAsync(IReadOnlyList<FirmwareImage> images, CancellationToken cancellationToken)
    {
        if (images.Count == 0)
            throw new ArgumentException("No images given", nameof(images));

        if (images.Select(x => x.ImageIndex).Distinct().Count() != images.Count)
            throw new ArgumentException("Each image index may only appear once", nameof(images));

        this.Report(FirmwareUpgradeStage.Validating);
        var state = await client.Images.GetStateAsync(cancellationToken).ConfigureAwait(false);

        var uploaded = new List<int>();
        var skipped = new List<int>();
        var toSwap = new List<FirmwareImage>();
        var toConfirmInPlace = new List<FirmwareImage>();

        foreach (var image in images)
        {
            var slot = Find(state, image);
            if (slot == null)
            {
                await this.UploadAsync(image, cancellationToken).ConfigureAwait(false);
                uploaded.Add(image.ImageIndex);
                toSwap.Add(image);
            }
            else if (slot.Active)
            {
                // already running - only an unconfirmed test image still needs work
                skipped.Add(image.ImageIndex);
                if (!slot.Confirmed && options.Mode is FirmwareUpgradeMode.TestAndConfirm or FirmwareUpgradeMode.ConfirmOnly)
                    toConfirmInPlace.Add(image);
            }
            else
            {
                // already uploaded to the secondary slot by an earlier attempt
                skipped.Add(image.ImageIndex);
                toSwap.Add(image);
            }
        }

        if (toConfirmInPlace.Count > 0)
        {
            this.Report(FirmwareUpgradeStage.Confirming);
            foreach (var image in toConfirmInPlace)
                await client.Images.SetStateAsync(image.Header.Hash, true, cancellationToken).ConfigureAwait(false);
        }

        var reset = false;
        if (options.Mode != FirmwareUpgradeMode.UploadOnly && toSwap.Count > 0)
        {
            this.Report(FirmwareUpgradeStage.Testing);
            var permanent = options.Mode == FirmwareUpgradeMode.ConfirmOnly;
            foreach (var image in toSwap)
                await client.Images.SetStateAsync(image.Header.Hash, permanent, cancellationToken).ConfigureAwait(false);

            this.Report(FirmwareUpgradeStage.Resetting);
            await this.ResetAsync(cancellationToken).ConfigureAwait(false);
            reset = true;

            this.Report(FirmwareUpgradeStage.Reconnecting);
            await client.Transport
                .ReconnectAsync(options.EstimatedSwapTime, options.ReconnectTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (options.Mode == FirmwareUpgradeMode.TestAndConfirm)
                await this.ConfirmAsync(toSwap, cancellationToken).ConfigureAwait(false);
        }

        this.Report(FirmwareUpgradeStage.Completed);
        return new FirmwareUpgradeResult(uploaded, skipped, reset);
    }


    async Task UploadAsync(FirmwareImage image, CancellationToken cancellationToken)
    {
        this.Report(FirmwareUpgradeStage.Uploading, image.ImageIndex, new TransferProgress(0, image.Data.Length, 0));
        var uploadProgress = progress == null
            ? null
            : new InlineProgress<TransferProgress>(x => this.Report(FirmwareUpgradeStage.Uploading, image.ImageIndex, x));

        await client.Images
            .UploadAsync(image.Data, image.ImageIndex, options.UpgradeOnlyIfNewer, options, uploadProgress, cancellationToken)
            .ConfigureAwait(false);
    }


    async Task ResetAsync(CancellationToken cancellationToken)
    {
        try
        {
            await client.Os.ResetAsync(false, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is TimeoutException || (ex is McuMgrException me && me.Error == null && me.GroupErrorCode == null))
        {
            // the device can reboot before its answer gets out - the reconnect below proves whether it did
        }
    }


    async Task ConfirmAsync(IReadOnlyList<FirmwareImage> images, CancellationToken cancellationToken)
    {
        this.Report(FirmwareUpgradeStage.Confirming);
        var state = await client.Images.GetStateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var image in images)
        {
            var slot = Find(state, image);

            // a network core image is copied over by the bootloader and is not always listed afterwards
            if (slot != null)
            {
                if (!slot.Active)
                    throw new McuMgrException($"Image {image.ImageIndex} ({image.Header.Version}) is not running after the reset - the bootloader rejected or reverted it");

                if (!slot.Confirmed)
                    await client.Images.SetStateAsync(image.Header.Hash, true, cancellationToken).ConfigureAwait(false);
            }
        }
    }


    static McuMgrImageSlot? Find(IReadOnlyList<McuMgrImageSlot> state, FirmwareImage image) => state.FirstOrDefault(x =>
        x.Image == image.ImageIndex &&
        x.Hash.AsSpan().SequenceEqual(image.Header.Hash)
    );


    void Report(FirmwareUpgradeStage stage, int? imageIndex = null, TransferProgress? upload = null)
        => progress?.Report(new FirmwareUpgradeProgress(stage, imageIndex, upload));


    // Progress<T> posts to the captured sync context; this reports inline so stages stay in order
    sealed class InlineProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }
}

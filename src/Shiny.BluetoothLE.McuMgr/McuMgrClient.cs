namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// The well-known MCUmgr management group ids.
/// </summary>
public static class McuMgrGroups
{
    /// <summary>OS management - echo, reset, parameters, app/bootloader info.</summary>
    public const ushort Os = 0;
    /// <summary>Image management - state, upload, erase.</summary>
    public const ushort Image = 1;
    /// <summary>Statistics.</summary>
    public const ushort Stats = 2;
    /// <summary>Settings (config).</summary>
    public const ushort Settings = 3;
    /// <summary>Logs.</summary>
    public const ushort Log = 4;
    /// <summary>Crash test.</summary>
    public const ushort Crash = 5;
    /// <summary>File system.</summary>
    public const ushort FileSystem = 8;
    /// <summary>Shell.</summary>
    public const ushort Shell = 9;
    /// <summary>Group enumeration.</summary>
    public const ushort Enumeration = 10;
    /// <summary>Zephyr basic management - erase storage.</summary>
    public const ushort ZephyrBasic = 63;
    /// <summary>The first id available for application-defined groups.</summary>
    public const ushort PerUser = 64;
}


/// <summary>
/// Options for <see cref="McuMgrClient"/>.
/// </summary>
public sealed class McuMgrClientOptions
{
    /// <summary>The time to wait for a response. Default 10 seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The protocol version to start with. Default <see cref="SmpVersion.V2"/>; the client drops to
    /// <see cref="SmpVersion.V1"/> on its own when the device rejects version 2.
    /// </summary>
    public SmpVersion Version { get; set; } = SmpVersion.V2;
}


/// <summary>
/// An MCUmgr (SMP) client - the management protocol of Zephyr RTOS, the nRF Connect SDK, Apache Mynewt and
/// NuttX devices that boot through MCUboot. Commands are grouped by <see cref="Os"/>, <see cref="Images"/>,
/// <see cref="Files"/>, <see cref="Shell"/> and <see cref="Stats"/>, <see cref="UpgradeFirmwareAsync(IReadOnlyList{FirmwareImage}, FirmwareUpgradeOptions?, IProgress{FirmwareUpgradeProgress}?, CancellationToken)"/>
/// runs a complete firmware update, and <see cref="SendAsync"/> reaches any other (or custom) group.
/// </summary>
/// <remarks>Requests are sent one at a time; concurrent calls queue.</remarks>
public sealed class McuMgrClient : IDisposable
{
    readonly SemaphoreSlim requestLock = new(1, 1);
    readonly bool ownsTransport;
    byte sequence;


    /// <summary>
    /// Creates a client over any SMP transport.
    /// </summary>
    /// <param name="transport">The transport.</param>
    /// <param name="options">Client options.</param>
    /// <param name="ownsTransport">When true, disposing the client disposes the transport (if it is disposable).</param>
    public McuMgrClient(ISmpTransport transport, McuMgrClientOptions? options = null, bool ownsTransport = false)
    {
        this.Transport = transport ?? throw new ArgumentNullException(nameof(transport));
        this.Options = options ?? new McuMgrClientOptions();
        this.Version = this.Options.Version;
        this.ownsTransport = ownsTransport;

        this.Os = new McuMgrOs(this);
        this.Images = new McuMgrImages(this);
        this.Files = new McuMgrFileSystem(this);
        this.Shell = new McuMgrShell(this);
        this.Stats = new McuMgrStats(this);
    }


    /// <summary>The transport.</summary>
    public ISmpTransport Transport { get; }

    /// <summary>The client options.</summary>
    public McuMgrClientOptions Options { get; }

    /// <summary>The protocol version in use - drops to V1 when the device does not speak V2.</summary>
    public SmpVersion Version { get; private set; }

    /// <summary>OS management (group 0).</summary>
    public McuMgrOs Os { get; }

    /// <summary>Image management (group 1).</summary>
    public McuMgrImages Images { get; }

    /// <summary>File system management (group 8).</summary>
    public McuMgrFileSystem Files { get; }

    /// <summary>Shell management (group 9).</summary>
    public McuMgrShell Shell { get; }

    /// <summary>Statistics management (group 2).</summary>
    public McuMgrStats Stats { get; }


    /// <summary>
    /// Sends a request and returns the decoded response payload. Use this for groups or commands without a typed
    /// wrapper, including application-defined groups (<see cref="McuMgrGroups.PerUser"/> and above).
    /// </summary>
    /// <param name="operation"><see cref="SmpOperation.Read"/> or <see cref="SmpOperation.Write"/>.</param>
    /// <param name="group">The group id.</param>
    /// <param name="command">The command id.</param>
    /// <param name="payload">The request map, or null for an empty map.</param>
    /// <param name="timeout">Overrides <see cref="McuMgrClientOptions.RequestTimeout"/> for this request.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <exception cref="McuMgrException">The device returned an error.</exception>
    /// <exception cref="TimeoutException">The device did not answer in time.</exception>
    public async Task<IReadOnlyDictionary<string, object?>> SendAsync(
        SmpOperation operation,
        ushort group,
        byte command,
        IReadOnlyDictionary<string, object?>? payload = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default
    )
    {
        if (operation is not (SmpOperation.Read or SmpOperation.Write))
            throw new ArgumentException("Only Read and Write requests can be sent", nameof(operation));

        var body = SmpCbor.Encode(payload);
        if (body.Length > ushort.MaxValue)
            throw new ArgumentException("The payload is larger than an SMP frame can carry", nameof(payload));

        await this.requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var (header, response) = await this.Exchange(operation, group, command, body, timeout, cancellationToken).ConfigureAwait(false);

            var rc = (McuMgrError?)response.GetInt64("rc");
            var downgrade = this.Version == SmpVersion.V2 &&
                (rc == McuMgrError.UnsupportedTooNew || (rc == McuMgrError.NotSupported && header.Version == SmpVersion.V1));

            if (downgrade)
            {
                this.Version = SmpVersion.V1;
                (header, response) = await this.Exchange(operation, group, command, body, timeout, cancellationToken).ConfigureAwait(false);
            }

            ThrowIfError(response, group, command);
            return response;
        }
        finally
        {
            this.requestLock.Release();
        }
    }


    /// <summary>
    /// Runs a firmware update for a single image - see
    /// <see cref="UpgradeFirmwareAsync(IReadOnlyList{FirmwareImage}, FirmwareUpgradeOptions?, IProgress{FirmwareUpgradeProgress}?, CancellationToken)"/>.
    /// </summary>
    public Task<FirmwareUpgradeResult> UpgradeFirmwareAsync(
        FirmwareImage image,
        FirmwareUpgradeOptions? options = null,
        IProgress<FirmwareUpgradeProgress>? progress = null,
        CancellationToken cancellationToken = default
    ) => this.UpgradeFirmwareAsync([image], options, progress, cancellationToken);


    /// <summary>
    /// Runs a complete MCUboot firmware update: checks what the device is already running, uploads each image that
    /// is not there yet, marks it for test (or confirms it), resets the device, waits for the swap, reconnects and,
    /// in <see cref="FirmwareUpgradeMode.TestAndConfirm"/>, confirms the new image so it stays.
    /// </summary>
    /// <param name="images">The images - more than one for multi-core parts such as the nRF5340 (app + network core).</param>
    /// <param name="options">Upgrade options.</param>
    /// <param name="progress">Receives stage changes and upload progress.</param>
    /// <param name="cancellationToken">Cancels the upgrade. Cancelling during upload leaves a partial image that the
    /// next upload of the same file resumes.</param>
    public Task<FirmwareUpgradeResult> UpgradeFirmwareAsync(
        IReadOnlyList<FirmwareImage> images,
        FirmwareUpgradeOptions? options = null,
        IProgress<FirmwareUpgradeProgress>? progress = null,
        CancellationToken cancellationToken = default
    ) => new FirmwareUpgrader(this, options ?? new FirmwareUpgradeOptions(), progress).RunAsync(images, cancellationToken);


    /// <summary>
    /// Disposes the transport when the client owns it.
    /// </summary>
    public void Dispose()
    {
        if (this.ownsTransport && this.Transport is IDisposable disposable)
            disposable.Dispose();

        this.requestLock.Dispose();
    }


    /// <summary>
    /// Works out the largest frame to send for bulk transfers (image and file uploads).
    /// </summary>
    internal async Task<int> ResolveMaxFrameSizeAsync(int? overrideSize, bool useDeviceBufferSize, CancellationToken cancellationToken)
    {
        if (overrideSize != null)
            return overrideSize.Value;

        if (useDeviceBufferSize)
        {
            try
            {
                var parameters = await this.Os.GetParametersAsync(cancellationToken).ConfigureAwait(false);
                if (parameters.BufferSize > 0)
                    return parameters.BufferSize;
            }
            catch (McuMgrException)
            {
                // older firmware without the params command: fall back to what fits in one transport unit
            }
        }
        return this.Transport.MaxFrameSize;
    }


    async Task<(SmpHeader Header, IReadOnlyDictionary<string, object?> Payload)> Exchange(
        SmpOperation operation,
        ushort group,
        byte command,
        byte[] body,
        TimeSpan? timeout,
        CancellationToken cancellationToken
    )
    {
        var seq = unchecked(this.sequence++);
        var frame = new byte[SmpHeader.Size + body.Length];
        new SmpHeader(operation, this.Version, 0, (ushort)body.Length, group, seq, command).Write(frame);
        body.CopyTo(frame, SmpHeader.Size);

        var wait = timeout ?? this.Options.RequestTimeout;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(wait);

        byte[] responseFrame;
        try
        {
            responseFrame = await this.Transport.SendAsync(frame, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"No MCUmgr response (group {group}, command {command}) within {wait}");
        }

        var header = SmpHeader.Read(responseFrame);
        if (header.Sequence != seq || header.Group != group || header.Command != command || header.Operation != operation + 1)
            throw new McuMgrException($"Unexpected MCUmgr response: {header} for request group {group}, command {command}, sequence {seq}");

        if (responseFrame.Length < SmpHeader.Size + header.Length)
            throw new McuMgrException("The MCUmgr response frame is truncated");

        var payload = SmpCbor.Decode(responseFrame.AsMemory(SmpHeader.Size, header.Length));
        return (header, payload);
    }


    static void ThrowIfError(IReadOnlyDictionary<string, object?> response, ushort group, byte command)
    {
        var rc = response.GetInt64("rc");
        if (rc is > 0)
            throw new McuMgrException((McuMgrError)rc.Value, group, command);

        var err = response.GetMap("err");
        if (err != null)
        {
            var groupRc = err.GetInt64("rc") ?? 0;
            if (groupRc != 0)
                throw new McuMgrException((ushort)(err.GetInt64("group") ?? group), command, (int)groupRc);
        }
    }
}

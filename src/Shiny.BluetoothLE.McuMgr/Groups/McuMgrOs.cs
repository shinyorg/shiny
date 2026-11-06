namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// The device's SMP buffer configuration.
/// </summary>
/// <param name="BufferSize">The size of one SMP buffer - the largest frame the device accepts.</param>
/// <param name="BufferCount">The number of SMP buffers.</param>
public record McuMgrParameters(int BufferSize, int BufferCount);


/// <summary>
/// Which OS / application details <see cref="McuMgrOs.GetApplicationInfoAsync"/> returns.
/// </summary>
[Flags]
public enum McuMgrAppInfo
{
    /// <summary>Kernel name (<c>s</c>).</summary>
    KernelName = 1,
    /// <summary>Node name (<c>n</c>).</summary>
    NodeName = 2,
    /// <summary>Kernel release (<c>r</c>).</summary>
    KernelRelease = 4,
    /// <summary>Kernel version (<c>v</c>).</summary>
    KernelVersion = 8,
    /// <summary>Build date and time (<c>b</c>).</summary>
    BuildDateTime = 16,
    /// <summary>Machine (<c>m</c>).</summary>
    Machine = 32,
    /// <summary>Processor (<c>p</c>).</summary>
    Processor = 64,
    /// <summary>Hardware platform (<c>i</c>).</summary>
    HardwarePlatform = 128,
    /// <summary>Operating system (<c>o</c>).</summary>
    OperatingSystem = 256,
    /// <summary>Everything (<c>a</c>).</summary>
    All = 511
}


/// <summary>
/// OS management - group 0.
/// </summary>
public sealed class McuMgrOs
{
    readonly McuMgrClient client;
    internal McuMgrOs(McuMgrClient client) => this.client = client;


    /// <summary>
    /// Sends text to the device and returns its echo - a cheap round trip to test the link.
    /// </summary>
    public async Task<string> EchoAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Write, McuMgrGroups.Os, 0, new Dictionary<string, object?> { ["d"] = text }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.GetString("r") ?? String.Empty;
    }


    /// <summary>
    /// Resets (reboots) the device. The device answers, then drops the connection shortly after.
    /// </summary>
    /// <param name="force">Reset even if the application vetoes it (SMP v2 firmware).</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public Task ResetAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>();
        if (force)
            payload["force"] = true;

        return this.client.SendAsync(SmpOperation.Write, McuMgrGroups.Os, 5, payload, cancellationToken: cancellationToken);
    }


    /// <summary>
    /// Reads the device's SMP buffer size and count. Firmware without this command throws
    /// <see cref="McuMgrException"/> with <see cref="McuMgrError.NotSupported"/>.
    /// </summary>
    public async Task<McuMgrParameters> GetParametersAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Os, 6, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new McuMgrParameters(
            (int)(response.GetInt64("buf_size") ?? 0),
            (int)(response.GetInt64("buf_count") ?? 0)
        );
    }


    /// <summary>
    /// Returns OS / application information as a single space separated string, like <c>uname</c>.
    /// </summary>
    public async Task<string> GetApplicationInfoAsync(McuMgrAppInfo info = McuMgrAppInfo.All, CancellationToken cancellationToken = default)
    {
        var format = info == McuMgrAppInfo.All ? "a" : String.Concat(
            new (McuMgrAppInfo Flag, char Code)[]
            {
                (McuMgrAppInfo.KernelName, 's'),
                (McuMgrAppInfo.NodeName, 'n'),
                (McuMgrAppInfo.KernelRelease, 'r'),
                (McuMgrAppInfo.KernelVersion, 'v'),
                (McuMgrAppInfo.BuildDateTime, 'b'),
                (McuMgrAppInfo.Machine, 'm'),
                (McuMgrAppInfo.Processor, 'p'),
                (McuMgrAppInfo.HardwarePlatform, 'i'),
                (McuMgrAppInfo.OperatingSystem, 'o')
            }
            .Where(x => info.HasFlag(x.Flag))
            .Select(x => x.Code)
        );

        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Os, 7, new Dictionary<string, object?> { ["format"] = format }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.GetString("output") ?? String.Empty;
    }


    /// <summary>
    /// Returns the bootloader name (for example <c>MCUboot</c>).
    /// </summary>
    public async Task<string> GetBootloaderNameAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Os, 8, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.GetString("bootloader") ?? String.Empty;
    }


    /// <summary>
    /// Erases the application's storage partition (settings, file system) - Zephyr basic management, group 63.
    /// The device usually needs a reset afterwards.
    /// </summary>
    public Task EraseStorageAsync(CancellationToken cancellationToken = default)
        => this.client.SendAsync(SmpOperation.Write, McuMgrGroups.ZephyrBasic, 0, cancellationToken: cancellationToken);
}

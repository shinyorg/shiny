namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// The result of a shell command.
/// </summary>
/// <param name="Output">What the command printed.</param>
/// <param name="ReturnCode">The command's return code.</param>
public record McuMgrShellResult(string Output, int ReturnCode);


/// <summary>
/// Shell management - group 9. Needs <c>CONFIG_MCUMGR_GRP_SHELL</c> in the firmware.
/// </summary>
public sealed class McuMgrShell
{
    readonly McuMgrClient client;
    internal McuMgrShell(McuMgrClient client) => this.client = client;


    /// <summary>
    /// Runs a shell command, e.g. <c>ExecuteAsync("kernel", "uptime")</c>.
    /// </summary>
    public Task<McuMgrShellResult> ExecuteAsync(params string[] argv) => this.ExecuteAsync(argv, default);


    /// <summary>
    /// Runs a shell command given as separate arguments.
    /// </summary>
    public async Task<McuMgrShellResult> ExecuteAsync(IReadOnlyList<string> argv, CancellationToken cancellationToken)
    {
        if (argv.Count == 0)
            throw new ArgumentException("No command given", nameof(argv));

        var response = await this.client
            .SendAsync(
                SmpOperation.Write,
                McuMgrGroups.Shell,
                0,
                new Dictionary<string, object?> { ["argv"] = argv.Cast<object?>().ToList() },
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return new McuMgrShellResult(
            response.GetString("o") ?? String.Empty,
            (int)(response.GetInt64("ret") ?? 0)
        );
    }
}


/// <summary>
/// Statistics management - group 2.
/// </summary>
public sealed class McuMgrStats
{
    readonly McuMgrClient client;
    internal McuMgrStats(McuMgrClient client) => this.client = client;


    /// <summary>
    /// Lists the statistic groups the firmware registered.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListGroupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Stats, 1, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.GetList("stat_list").OfType<string>().ToList();
    }


    /// <summary>
    /// Reads the counters of one statistic group.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, long>> ReadAsync(string group, CancellationToken cancellationToken = default)
    {
        var response = await this.client
            .SendAsync(SmpOperation.Read, McuMgrGroups.Stats, 0, new Dictionary<string, object?> { ["name"] = group }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var fields = response.GetMap("fields");
        if (fields == null)
            return new Dictionary<string, long>();

        return fields
            .Where(x => x.Value is long)
            .ToDictionary(x => x.Key, x => (long)x.Value!);
    }
}

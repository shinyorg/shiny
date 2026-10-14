namespace Shiny.DeviceTests.Framework;


/// <summary>
/// Something the other device can ask this one to do mid-test (e.g. "start hosting the BLE test profile").
/// The runner relays it to this app's DevFlow extension; tests call it through TestContext.Host.PeerAsync.
/// </summary>
public interface IPeerCommand
{
    string Name { get; }
    Task<Dictionary<string, string>?> ExecuteAsync(Dictionary<string, string> args, CancellationToken cancellationToken);
}


public sealed class PeerCommandDispatcher(IEnumerable<IPeerCommand> commands)
{
    public async Task<PeerCommandResponse> ExecuteAsync(PeerCommandRequest request, CancellationToken cancellationToken)
    {
        var command = commands.FirstOrDefault(x => x.Name.Equals(request.Command, StringComparison.OrdinalIgnoreCase));
        if (command == null)
            return new PeerCommandResponse(false, $"Unknown peer command '{request.Command}'", null);

        try
        {
            var values = await command
                .ExecuteAsync(request.Args ?? [], cancellationToken)
                .ConfigureAwait(false);

            return new PeerCommandResponse(true, null, values);
        }
        catch (Exception ex)
        {
            return new PeerCommandResponse(false, ex.ToString(), null);
        }
    }
}

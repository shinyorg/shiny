namespace Shiny.DeviceTests.Runner;


// what the environment endpoints and fixtures need to know about this run
public sealed class RunnerState(TargetDevice target, AgentClient? peer)
{
    public TargetDevice Target => target;
    public AgentClient? Peer => peer;

    public HostCapability Capabilities
    {
        get
        {
            var caps = HostCapability.Server;
            if (target.CanSetLocation)
                caps |= HostCapability.Location;
            if (target.CanPush)
                caps |= HostCapability.Push;
            if (peer != null)
                caps |= HostCapability.Peer;
            if (target.CanPlaceCall)
                caps |= HostCapability.PhoneCall;
            return caps;
        }
    }
}

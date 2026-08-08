namespace Shiny.Net.Discovery;


/// <summary>
/// A live service advertisement. Dispose it to send a goodbye and stop advertising.
/// </summary>
public interface IMdnsPublication : IAsyncDisposable
{
    /// <summary>
    /// The instance name the service is actually advertised under. This can differ from the
    /// requested name when the link already had a service with that name and the responder
    /// renamed it to resolve the conflict.
    /// </summary>
    string InstanceName { get; }

    /// <summary>
    /// The service type being advertised, eg "_myapp._tcp".
    /// </summary>
    string ServiceType { get; }

    /// <summary>
    /// The domain being advertised in, normally "local".
    /// </summary>
    string Domain { get; }

    /// <summary>
    /// The port being advertised.
    /// </summary>
    int Port { get; }
}

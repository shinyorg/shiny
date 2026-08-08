using System.Xml;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// The ordering information a responder stamps on every message it sends.
/// </summary>
/// <param name="InstanceId">
/// Increases every time the device restarts. A message carrying a lower value than the one we
/// already hold is from a previous life and must be discarded.
/// </param>
/// <param name="SequenceId">Optional sequence identifier within an instance.</param>
/// <param name="MessageNumber">Increments per message within a sequence.</param>
readonly record struct WsdAppSequence(uint InstanceId, string? SequenceId, uint MessageNumber);


/// <summary>
/// One discovered target, as carried by Hello, Bye, ProbeMatch and ResolveMatch.
/// </summary>
sealed record WsdMatch(
    string EndpointReference,
    IReadOnlyList<XmlQualifiedName> Types,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> XAddrs,
    uint MetadataVersion
);


/// <summary>
/// What a Probe is asking for.
/// </summary>
sealed record WsdProbeCriteria(
    IReadOnlyList<XmlQualifiedName> Types,
    IReadOnlyList<string> Scopes,
    string? MatchBy
);


/// <summary>
/// A parsed WS-Discovery envelope.
/// </summary>
sealed class WsdMessage
{
    public required WsdVersionProfile Profile { get; init; }

    /// <summary>The message name from the action URI - Probe, Hello, ProbeMatches and so on.</summary>
    public required string MessageName { get; init; }

    public string? MessageId { get; init; }

    /// <summary>The MessageID of the Probe or Resolve this answers.</summary>
    public string? RelatesTo { get; init; }

    public WsdAppSequence? AppSequence { get; init; }

    /// <summary>Targets carried by Hello, Bye, ProbeMatches and ResolveMatches.</summary>
    public IReadOnlyList<WsdMatch> Matches { get; init; } = Array.Empty<WsdMatch>();

    /// <summary>Present on a Probe.</summary>
    public WsdProbeCriteria? Probe { get; init; }

    /// <summary>The endpoint reference a Resolve is asking about.</summary>
    public string? ResolveTarget { get; init; }


    public override string ToString()
        => $"{this.MessageName} ({this.Profile.Profile}) matches={this.Matches.Count}";
}

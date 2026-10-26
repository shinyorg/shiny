namespace Shiny.AgeSignals;


/// <summary>Windows, Linux, plain .NET and Blazor - no store shares an age range there.</summary>
public class NotSupportedAgeSignals : IAgeSignals
{
    public Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default)
        => Task.FromResult(AgeSignalsEligibility.NotSupported);

    public Task<AgeSignal> RequestAgeRange(int gate1, int? gate2 = null, int? gate3 = null, CancellationToken cancellationToken = default)
    {
        Infrastructure.AgeSignalMapper.ValidateGates(gate1, gate2, gate3);
        return Task.FromResult(AgeSignal.NotSupported);
    }

    public Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

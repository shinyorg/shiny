using Shiny.AgeSignals.Infrastructure;

namespace Shiny.AgeSignals;


/// <summary>
/// Apple Declared Age Range through the ShinyAgeRange Swift shim. The framework answers on iOS / macOS 26+; the shim
/// weak-links it, so earlier versions report <see cref="AgeSignalStatus.NotSupported"/>.
/// </summary>
public class AppleAgeSignals : IAgeSignals
{
    public async Task<AgeSignalsEligibility> GetEligibility(CancellationToken cancellationToken = default)
    {
        if (!AgeRangeBridge.IsSupported())
            return AgeSignalsEligibility.NotSupported;

        var result = await AgeRangeBridge.EligibilityAsync(cancellationToken).ConfigureAwait(false);
        if (result.ErrorJson != null)
        {
            // a status-type error (declined, unsupported) has no meaning here - anything that isn't thrown is "no answer"
            AgeSignalMapper.FromAppleError(result.ErrorJson);
            return new AgeSignalsEligibility(true);
        }
        return AgeSignalMapper.FromAppleEligibility(result.Json!);
    }


    public async Task<AgeSignal> RequestAgeRange(int gate1, int? gate2 = null, int? gate3 = null, CancellationToken cancellationToken = default)
    {
        AgeSignalMapper.ValidateGates(gate1, gate2, gate3);
        if (!AgeRangeBridge.IsSupported())
            return AgeSignal.NotSupported;

        var result = await AgeRangeBridge.RequestAsync(gate1, gate2, gate3, cancellationToken).ConfigureAwait(false);
        return result.ErrorJson == null
            ? AgeSignalMapper.FromAppleRange(result.Json!)
            : AgeSignalMapper.FromAppleError(result.ErrorJson);
    }


    public async Task<bool> ShowSignificantUpdate(string updateDescription, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(updateDescription);
        if (!AgeRangeBridge.IsSupported())
            return false;

        var result = await AgeRangeBridge.SignificantUpdateAsync(updateDescription, cancellationToken).ConfigureAwait(false);
        if (result.ErrorJson != null)
        {
            AgeSignalMapper.FromAppleError(result.ErrorJson);
            return false;
        }
        return result.Json == "true";
    }
}

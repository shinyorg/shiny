using Shiny.Biometrics.Infrastructure;

namespace Shiny.Biometrics;


/// <summary>Linux, plain .NET and Blazor - there is no local biometric prompt to call.</summary>
public class NotSupportedBiometrics : IBiometrics
{
    public Task<BiometricAvailability> GetAvailability(BiometricStrength strength = BiometricStrength.Strong, CancellationToken cancellationToken = default)
        => Task.FromResult(BiometricAvailability.NotSupported);

    public Task<BiometricResult> Authenticate(BiometricRequest request, CancellationToken cancellationToken = default)
    {
        BiometricMapper.Validate(request);
        return Task.FromResult(BiometricResult.NotAvailable);
    }
}

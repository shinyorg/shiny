using Shiny.Biometrics;

namespace Sample.Shared.Maui.Pages.Security;

[ShellMap<BiometricsPage>("biometrics")]
public partial class BiometricsViewModel(IBiometrics biometrics) : ObservableObject
{
    [ObservableProperty] string availability = "Not checked";
    [ObservableProperty] bool weak;
    [ObservableProperty] string reason = "Unlock the Shiny sample";
    [ObservableProperty] bool allowDeviceCredential = true;
    [ObservableProperty] string result = "Not authenticated";

    BiometricStrength Strength => this.Weak ? BiometricStrength.Weak : BiometricStrength.Strong;


    [RelayCommand]
    async Task Check()
    {
        try
        {
            var a = await biometrics.GetAvailability(this.Strength);
            this.Availability =
                $"Status: {a.Status}\n" +
                $"Hardware: {Describe(a.Types)}\n" +
                $"Passcode / PIN set: {a.IsDeviceCredentialSet}";
        }
        catch (Exception ex)
        {
            this.Availability = $"Error: {ex.Message}";
        }
    }


    [RelayCommand]
    async Task Authenticate()
    {
        try
        {
            var r = await biometrics.Authenticate(new BiometricRequest
            {
                Reason = this.Reason,
                Title = "Shiny Biometrics",
                AllowDeviceCredential = this.AllowDeviceCredential,
                Strength = this.Strength
            });
            this.Result = r.ErrorMessage is null ? r.Status.ToString() : $"{r.Status}: {r.ErrorMessage}";
        }
        catch (Exception ex)
        {
            // empty reason, no FragmentActivity, or a missing NSFaceIDUsageDescription
            this.Result = $"Error: {ex.Message}";
        }
    }


    static string Describe(BiometricType types) => types switch
    {
        BiometricType.None => "none",
        BiometricType.Face when OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() => "Face ID",
        BiometricType.Fingerprint when OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS() => "Touch ID",
        BiometricType.Iris when OperatingSystem.IsIOS() => "Optic ID",
        BiometricType.Unknown when OperatingSystem.IsWindows() => "Windows Hello",
        _ => types.ToString()
    };
}

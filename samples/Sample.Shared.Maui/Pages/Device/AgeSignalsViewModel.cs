using Shiny.AgeSignals;

namespace Sample.Shared.Maui.Pages.Device;

[ShellMap<AgeSignalsPage>("agesignals")]
public partial class AgeSignalsViewModel(IAgeSignals ageSignals) : ObservableObject
{
    [ObservableProperty] string eligibility = "Not checked";
    [ObservableProperty] string gates = "13,16,18";
    [ObservableProperty] string result = "Not requested";
    [ObservableProperty] string updateDescription = "Adds chat with other players";

    [RelayCommand]
    async Task CheckEligibility()
    {
        try
        {
            var e = await ageSignals.GetEligibility();
            this.Eligibility = e.IsSupported
                ? $"Eligible: {Show(e.IsEligible)}\nAge range required: {Show(e.AgeRangeRequired)}\nChanges need parental consent: {Show(e.SignificantChangeRequiresParentalConsent)}\nChanges need adult notification: {Show(e.SignificantChangeRequiresAdultNotification)}"
                : "Not supported on this platform / OS version";
        }
        catch (Exception ex)
        {
            this.Eligibility = Describe(ex);
        }
    }


    [RelayCommand]
    async Task Request()
    {
        try
        {
            var parts = this.Gates
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

            if (parts.Count is < 1 or > 3)
            {
                this.Result = "Enter 1 to 3 ages, e.g. 13,16,18";
                return;
            }

            var signal = await ageSignals.RequestAgeRange(
                parts[0],
                parts.Count > 1 ? parts[1] : null,
                parts.Count > 2 ? parts[2] : null
            );
            this.Result =
                $"Status: {signal.Status}\n" +
                $"Range: {signal.LowerBound?.ToString() ?? "?"} - {signal.UpperBound?.ToString() ?? "?"}\n" +
                $"Source: {signal.Source}\n" +
                $"Adult: {Show(signal.IsAdult)}\n" +
                $"Communication limits: {signal.CommunicationLimits}\n" +
                $"Significant change: {signal.SignificantChange?.ToString() ?? "-"}\n" +
                $"Install id: {signal.InstallId ?? "-"}";
        }
        catch (Exception ex)
        {
            this.Result = Describe(ex);
        }
    }


    [RelayCommand]
    async Task ShowSignificantUpdate()
    {
        try
        {
            var shown = await ageSignals.ShowSignificantUpdate(this.UpdateDescription);
            this.Result = shown ? "Acknowledgment shown" : "Not available on this platform / OS version";
        }
        catch (Exception ex)
        {
            this.Result = Describe(ex);
        }
    }


    static string Show(bool? value) => value?.ToString() ?? "unknown";

    static string Describe(Exception ex) => ex is AgeSignalsException age
        ? $"Error ({age.Reason}{(age.NativeCode is { } code ? $", {code}" : "")}): {age.Message}"
        : $"Error: {ex.Message}";
}

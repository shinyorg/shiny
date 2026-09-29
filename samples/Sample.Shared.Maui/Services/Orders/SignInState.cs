using Microsoft.Maui.Storage;

namespace Sample.Shared.Maui.Services.Orders;

/// <summary>
/// Stands in for real authentication. Cancelling an order needs a signed-in user (see SignInDelegate in Sample.Maui).
/// Persisted, because Siri and Gemini can start the app cold.
/// </summary>
public class SignInState
{
    public bool IsSignedIn
    {
        get => Preferences.Get("appfunctions_signed_in", false);
        set
        {
            Preferences.Set("appfunctions_signed_in", value);
            this.Changed?.Invoke();
        }
    }

    public event Action? Changed;
}

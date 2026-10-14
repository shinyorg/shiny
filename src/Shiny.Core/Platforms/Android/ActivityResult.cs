using Android.App;
using Android.Content;

namespace Shiny;


/// <summary>
/// The result an activity started with <see cref="AndroidPlatform.StartActivityForResult"/> (or any
/// <c>StartActivityForResult</c> the host activity forwards to Shiny) returned.
/// </summary>
/// <param name="Activity">The activity that received the result.</param>
/// <param name="RequestCode">The request code the activity was started with.</param>
/// <param name="ResultCode">The result code the started activity set.</param>
/// <param name="Data">The result data, if any.</param>
public record ActivityResult(Activity Activity, int RequestCode, Result ResultCode, Intent? Data)
{
    /// <summary>True when the started activity returned <see cref="Result.Ok"/>.</summary>
    public bool IsOk => this.ResultCode == Result.Ok;
}

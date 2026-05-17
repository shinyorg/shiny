using System.Threading.Tasks;

namespace Shiny.Notifications;


/// <summary>
/// Optional capability implemented by notification managers that can read and write the app icon badge value.
/// </summary>
public interface ICanManageBadge : INotificationManager
{
    /// <summary>
    /// Sets the app icon badge number.
    /// </summary>
    /// <param name="value">The badge value to display. Pass 0 to clear the badge.</param>
    Task SetBadge(int value);

    /// <summary>
    /// Returns the current app icon badge value.
    /// </summary>
    /// <returns>The current badge number.</returns>
    Task<int> GetBadge();
}


/// <summary>
/// Convenience extensions for setting and reading the app icon badge through the notification manager when supported.
/// </summary>
public static class FeatureBadges
{
    /// <summary>
    /// Sets the app icon badge if the underlying notification manager supports badging.
    /// </summary>
    /// <param name="manager">The notification manager.</param>
    /// <param name="value">The badge value to set. Pass 0 to clear.</param>
    /// <returns>True if the manager supports badging and the value was applied; otherwise false.</returns>
    public static async Task<bool> TrySetBadge(this INotificationManager manager, int value)
    {
        if (manager is ICanManageBadge badges)
        {
            await badges.SetBadge(value).ConfigureAwait(false);
            return true;
        }
        return false;
    }


    /// <summary>
    /// Attempts to read the app icon badge if the underlying notification manager supports badging.
    /// </summary>
    /// <param name="manager">The notification manager.</param>
    /// <returns>A tuple where Success indicates whether badging is supported and Value contains the current badge (or null when unsupported).</returns>
    public static async Task<(bool Success, int? Value)> TryGetBadge(this INotificationManager manager)
    {
        if (manager is ICanManageBadge badges)
        {
            var value = await badges.GetBadge().ConfigureAwait(false);
            return (true, value);
        }
        return (false, null);
    }
}

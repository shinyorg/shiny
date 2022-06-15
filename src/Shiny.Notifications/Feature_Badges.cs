using System.Threading.Tasks;

namespace Shiny.Notifications;


public interface ICanManageBadge : INotificationManager
{
    /// <summary>
    /// Set the badge number
    /// </summary>
    /// <param name="value">0 to clear</param>
    /// <returns></returns>
    Task SetBadge(int value);

    /// <summary>
    /// Get the app icon badge
    /// </summary>
    Task<int> GetBadge();
}


public static class FeatureBadges
{
    /// <summary>
    /// It notification manager support badge setting, this will return true and set the value
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="value"></param>
    /// <returns></returns>
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
    /// If notification manager support badge setting, this will return (true, Value) otherwise (false, null)
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
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

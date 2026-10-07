namespace Shiny.GameCenter;


/// <summary>
/// The game service behind <see cref="IGameCenterManager"/> on this device.
/// </summary>
public enum GameServicePlatform
{
    /// <summary>No game service - unsupported platform. Everything is recorded locally and nothing is sent.</summary>
    None,

    /// <summary>Apple Game Center (GameKit) on iOS, Mac Catalyst and macOS</summary>
    AppleGameCenter,

    /// <summary>Google Play Games Services v2 on Android</summary>
    GooglePlayGames,

    /// <summary>A provider registered by the app or another package</summary>
    Custom
}


/// <summary>
/// The signed-in player.
/// </summary>
/// <param name="Id">
/// The service's player identifier - Apple <c>gamePlayerID</c> (stable for this game) or Google <c>playerId</c>.
/// </param>
/// <param name="DisplayName">The player's display name or alias</param>
/// <param name="Platform">The service the player signed in to</param>
public record GamePlayer(string Id, string DisplayName, GameServicePlatform Platform);


/// <summary>
/// An achievement as the service reports it, merged with progress recorded on this device that has not been sent yet.
/// </summary>
/// <param name="Key">Your logical key, or the platform id when the achievement was not registered with <see cref="GameCenterOptions.AddAchievement"/></param>
/// <param name="PlatformId">The Game Center / Play Games achievement id</param>
/// <param name="Title">Localized title (null when the service did not supply one)</param>
/// <param name="Description">Localized description (null when the service did not supply one)</param>
/// <param name="CurrentSteps">Progress in steps - never lower than what has been recorded on this device</param>
/// <param name="TotalSteps">Steps needed to unlock (1 for a plain unlock-once achievement)</param>
/// <param name="IsUnlocked">True when <paramref name="CurrentSteps"/> has reached <paramref name="TotalSteps"/></param>
/// <param name="IsHidden">True while the achievement is hidden from the player</param>
public record Achievement(
    string Key,
    string PlatformId,
    string? Title,
    string? Description,
    int CurrentSteps,
    int TotalSteps,
    bool IsUnlocked,
    bool IsHidden
)
{
    /// <summary>Progress as a percentage, 0-100</summary>
    public double PercentComplete => this.TotalSteps <= 0 ? 0 : Math.Min(100d, this.CurrentSteps * 100d / this.TotalSteps);
}


/// <summary>
/// An achievement registered with <see cref="GameCenterOptions.AddAchievement"/>.
/// </summary>
public record AchievementDefinition(string Key, string? AppleId, string? GoogleId, int TotalSteps);


/// <summary>
/// A leaderboard registered with <see cref="GameCenterOptions.AddLeaderboard"/>.
/// </summary>
public record LeaderboardDefinition(string Key, string? AppleId, string? GoogleId);


/// <summary>
/// An achievement as a provider loads it from the service, before it is merged with local progress.
/// </summary>
/// <param name="Id">The platform achievement id</param>
/// <param name="CurrentSteps">
/// Steps completed, when the service counts in steps (Google incremental achievements). Null when it does not.
/// </param>
/// <param name="TotalSteps">Steps needed, when the service counts in steps. Null when it does not.</param>
/// <param name="PercentComplete">Progress as a percentage, when the service counts that way (Apple). Null when it does not.</param>
public record PlatformAchievement(
    string Id,
    string? Title,
    string? Description,
    int? CurrentSteps,
    int? TotalSteps,
    double? PercentComplete,
    bool IsUnlocked,
    bool IsHidden
);


/// <summary>Whose scores to load from a leaderboard</summary>
public enum LeaderboardScope
{
    /// <summary>Everyone</summary>
    Global,

    /// <summary>The player's friends (and the player). Needs friends-list consent on both platforms.</summary>
    Friends
}


/// <summary>The window of scores to load from a leaderboard</summary>
public enum LeaderboardTimeScope
{
    Today,
    Week,
    AllTime
}


/// <summary>One row of a leaderboard</summary>
/// <param name="FormattedScore">The score formatted the way the leaderboard is set up to show it</param>
/// <param name="IsLocalPlayer">True for the signed-in player's own entry</param>
public record LeaderboardEntry(GamePlayer Player, long Score, string? FormattedScore, int Rank, bool IsLocalPlayer);


/// <summary>Whether the player has let this game see their friends</summary>
public enum FriendsAccessStatus
{
    /// <summary>Allowed - <see cref="FriendsResult.Friends"/> is populated</summary>
    Granted,

    /// <summary>The player said no (Apple: change it in Settings; Google: ask again later)</summary>
    Denied,

    /// <summary>Blocked by parental controls / device management (Apple)</summary>
    Restricted,

    /// <summary>
    /// Google: the consent dialog has been shown - call again once the player returns to the app.
    /// Play Games reports the decision through an activity result, not to the caller.
    /// </summary>
    ConsentRequested,

    /// <summary>The service has no friends list on this device</summary>
    Unavailable
}


/// <summary>The result of <see cref="IGameCenterManager.GetFriendsAsync"/></summary>
public record FriendsResult(FriendsAccessStatus Status, IReadOnlyList<GamePlayer> Friends);

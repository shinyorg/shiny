namespace Shiny.GameCenter;


/// <summary>
/// Achievements and leaderboards over Apple Game Center and Google Play Games Services.
/// </summary>
/// <remarks>
/// <para>Progress is recorded on the device first and then sent, so <see cref="UnlockAsync"/>,
/// <see cref="SetProgressAsync"/>, <see cref="IncrementAsync"/>, <see cref="RevealAsync"/> and
/// <see cref="SubmitScoreAsync"/> do not fail when the player is offline or signed out - the work is queued, kept
/// across restarts, and sent the next time the player is signed in and online.</para>
/// <para>Progress is sent as an absolute step count rather than a delta, so a retry never counts twice and calls that
/// would not move an achievement forward are skipped.</para>
/// </remarks>
public interface IGameCenterManager
{
    /// <summary>The service behind this manager on this device</summary>
    GameServicePlatform Platform { get; }

    /// <summary>The signed-in player, or null</summary>
    GamePlayer? Player { get; }

    /// <summary>True when a player is signed in</summary>
    bool IsAuthenticated => this.Player != null;

    /// <summary>Raised when a player signs in, signs out or switches accounts. Null means signed out.</summary>
    event EventHandler<GamePlayer?>? PlayerChanged;

    /// <summary>Queued operations not yet accepted by the service</summary>
    int PendingCount { get; }

    /// <summary>
    /// Signs the player in, showing the service's sign-in UI when it is needed. Returns null when the player declines,
    /// or when Apple has stopped offering the sheet after repeated cancels (the player must then sign in from Settings).
    /// Throws <see cref="GameCenterException"/> when the app is misconfigured or the service is unavailable.
    /// </summary>
    Task<GamePlayer?> SignInAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Loads every achievement from the service, merged with progress recorded on this device but not sent yet.
    /// Requires a signed-in player.
    /// </summary>
    Task<IReadOnlyList<Achievement>> GetAchievementsAsync(bool forceReload = false, CancellationToken cancelToken = default);

    /// <summary>Completes an achievement (sets it to its total steps)</summary>
    Task UnlockAsync(string achievementKey, CancellationToken cancelToken = default);

    /// <summary>
    /// Sets an incremental achievement to an absolute step count. Progress never goes backwards - a value at or below
    /// the current progress is ignored. Values above the total are clamped.
    /// </summary>
    Task SetProgressAsync(string achievementKey, int steps, CancellationToken cancelToken = default);

    /// <summary>
    /// Adds steps to an incremental achievement. Counted from the furthest progress known on this device (refreshed
    /// from the service once per sign-in), then sent as an absolute value.
    /// </summary>
    Task IncrementAsync(string achievementKey, int steps = 1, CancellationToken cancelToken = default);

    /// <summary>
    /// Makes a hidden achievement visible. Google only - Game Center has no reveal; a hidden Apple achievement
    /// appears once it has progress, so this is a no-op there.
    /// </summary>
    Task RevealAsync(string achievementKey, CancellationToken cancelToken = default);

    /// <summary>Submits a score. The service decides whether it is a new best.</summary>
    Task SubmitScoreAsync(string leaderboardKey, long score, CancellationToken cancelToken = default);

    /// <summary>
    /// Shows the service's achievements UI. Completes once the UI has been opened, not when the player closes it.
    /// Apple opens the Game Center dashboard through <c>GKAccessPoint</c>.
    /// </summary>
    Task ShowAchievementsAsync(CancellationToken cancelToken = default);

    /// <summary>Shows one leaderboard, or every leaderboard when <paramref name="leaderboardKey"/> is null</summary>
    Task ShowLeaderboardAsync(string? leaderboardKey = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Loads scores from a leaderboard - the top <paramref name="maxResults"/> for everyone, or the player's friends
    /// with <see cref="LeaderboardScope.Friends"/>. Requires a signed-in player.
    /// </summary>
    Task<IReadOnlyList<LeaderboardEntry>> GetScoresAsync(
        string leaderboardKey,
        LeaderboardScope scope = LeaderboardScope.Global,
        LeaderboardTimeScope timeScope = LeaderboardTimeScope.AllTime,
        int maxResults = 25,
        CancellationToken cancelToken = default
    );

    /// <summary>
    /// Loads the player's friends who are visible to this game, asking for consent the first time.
    /// Apple: needs <c>NSGKFriendListUsageDescription</c> in Info.plist and shows a one-time prompt.
    /// Google: shows a consent dialog and returns <see cref="FriendsAccessStatus.ConsentRequested"/> - call again
    /// afterwards. Requires a signed-in player.
    /// </summary>
    Task<FriendsResult> GetFriendsAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Sends queued work now. Runs automatically after every call above, on sign-in and when connectivity returns -
    /// call it yourself only to wait for the queue to drain. Never throws for service errors; check
    /// <see cref="PendingCount"/> afterwards.
    /// </summary>
    Task FlushAsync(CancellationToken cancelToken = default);
}

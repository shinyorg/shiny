namespace Shiny.GameCenter.Infrastructure;


/// <summary>
/// The platform half of <see cref="IGameCenterManager"/> - the raw calls to one game service. Key mapping, the offline
/// queue and de-duplication live in the manager, so a provider only translates. Register your own before calling
/// <c>AddGameCenter</c> to plug in another service (Steam, a test double).
/// </summary>
/// <remarks>
/// Failures are reported by throwing <see cref="GameCenterException"/> with the closest
/// <see cref="GameCenterErrorCode"/>: the manager drops queued work on <see cref="GameCenterErrorCode.InvalidId"/> and
/// retries it later on anything else.
/// </remarks>
public interface IGameServicesProvider
{
    GameServicePlatform Platform { get; }

    /// <summary>
    /// Picks this service's id out of a registration (<see cref="AchievementDefinition.AppleId"/> /
    /// <see cref="AchievementDefinition.GoogleId"/>, or your own mapping). Null when the achievement is not on this service.
    /// </summary>
    string? GetPlatformId(AchievementDefinition definition);

    /// <summary>As <see cref="GetPlatformId(AchievementDefinition)"/>, for a leaderboard</summary>
    string? GetPlatformId(LeaderboardDefinition definition);

    /// <summary>
    /// Raised when the service changes the signed-in player on its own (Game Center sign-in from Settings,
    /// account switch). Null means signed out.
    /// </summary>
    event EventHandler<GamePlayer?>? PlayerChanged;

    /// <summary>
    /// Signs in. When <paramref name="interactive"/> is false no UI may be shown - return null if the player is not
    /// already signed in.
    /// </summary>
    Task<GamePlayer?> Authenticate(bool interactive, CancellationToken cancelToken);

    Task<IReadOnlyList<PlatformAchievement>> LoadAchievements(bool forceReload, CancellationToken cancelToken);

    /// <summary>
    /// Sets an achievement to an absolute step count. <paramref name="steps"/> equal to <paramref name="totalSteps"/>
    /// unlocks it. Must be idempotent - the same call can be repeated after a crash.
    /// </summary>
    Task SetAchievementProgress(string platformId, int steps, int totalSteps, CancellationToken cancelToken);

    Task RevealAchievement(string platformId, CancellationToken cancelToken);

    Task SubmitScore(string platformId, long score, CancellationToken cancelToken);

    Task ShowAchievements(CancellationToken cancelToken);

    Task ShowLeaderboard(string? platformId, CancellationToken cancelToken);

    Task<IReadOnlyList<LeaderboardEntry>> LoadScores(string platformId, LeaderboardScope scope, LeaderboardTimeScope timeScope, int maxResults, CancellationToken cancelToken);

    Task<FriendsResult> LoadFriends(CancellationToken cancelToken);
}

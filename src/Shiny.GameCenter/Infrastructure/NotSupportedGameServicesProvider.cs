namespace Shiny.GameCenter.Infrastructure;


/// <summary>
/// The provider on platforms without a game service. Nobody ever signs in, so calls are recorded locally and never
/// sent; anything that needs the service throws <see cref="GameCenterErrorCode.Unavailable"/>.
/// </summary>
class NotSupportedGameServicesProvider : IGameServicesProvider
{
    public GameServicePlatform Platform => GameServicePlatform.None;
    public string? GetPlatformId(AchievementDefinition definition) => definition.Key;
    public string? GetPlatformId(LeaderboardDefinition definition) => definition.Key;

    public event EventHandler<GamePlayer?>? PlayerChanged { add { } remove { } }

    public Task<GamePlayer?> Authenticate(bool interactive, CancellationToken cancelToken) => Task.FromResult<GamePlayer?>(null);
    public Task<IReadOnlyList<PlatformAchievement>> LoadAchievements(bool forceReload, CancellationToken cancelToken) => throw Unavailable();
    public Task SetAchievementProgress(string platformId, int steps, int totalSteps, CancellationToken cancelToken) => throw Unavailable();
    public Task RevealAchievement(string platformId, CancellationToken cancelToken) => throw Unavailable();
    public Task SubmitScore(string platformId, long score, CancellationToken cancelToken) => throw Unavailable();
    public Task ShowAchievements(CancellationToken cancelToken) => throw Unavailable();
    public Task ShowLeaderboard(string? platformId, CancellationToken cancelToken) => throw Unavailable();
    public Task<IReadOnlyList<LeaderboardEntry>> LoadScores(string platformId, LeaderboardScope scope, LeaderboardTimeScope timeScope, int maxResults, CancellationToken cancelToken) => throw Unavailable();
    public Task<FriendsResult> LoadFriends(CancellationToken cancelToken) => Task.FromResult(new FriendsResult(FriendsAccessStatus.Unavailable, []));

    static GameCenterException Unavailable() => new(GameCenterErrorCode.Unavailable, "No game service is available on this platform");
}

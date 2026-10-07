using Shiny.GameCenter.Infrastructure;

namespace Shiny.GameCenter.Tests;


/// <summary>
/// Records every call. <see cref="Offline"/> fails every send with a network error until cleared; <see cref="FailNext"/>
/// scripts one-off failures. Sign-in starts a background flush, so tests that need a failure to survive any number of
/// flushes use <see cref="Offline"/>.
/// </summary>
class FakeProvider : IGameServicesProvider
{
    public GameServicePlatform Platform => GameServicePlatform.Custom;
    public string? GetPlatformId(AchievementDefinition definition) => definition.GoogleId;
    public string? GetPlatformId(LeaderboardDefinition definition) => definition.GoogleId;
    public event EventHandler<GamePlayer?>? PlayerChanged;

    public GamePlayer? SignedIn { get; set; }
    public List<PlatformAchievement> Remote { get; } = new();
    public List<(string Id, int Steps, int Total)> Progress { get; } = new();
    public List<string> Reveals { get; } = new();
    public List<(string Id, long Score)> Scores { get; } = new();
    public Queue<GameCenterException> FailNext { get; } = new();
    public bool Offline { get; set; }
    public HashSet<string> UnknownIds { get; } = new();
    public int LoadCount { get; private set; }


    public void ChangePlayer(GamePlayer? player)
    {
        this.SignedIn = player;
        this.PlayerChanged?.Invoke(this, player);
    }


    public Task<GamePlayer?> Authenticate(bool interactive, CancellationToken cancelToken) => Task.FromResult(this.SignedIn);

    public Task<IReadOnlyList<PlatformAchievement>> LoadAchievements(bool forceReload, CancellationToken cancelToken)
    {
        this.LoadCount++;
        return Task.FromResult<IReadOnlyList<PlatformAchievement>>(this.Remote.ToList());
    }

    public Task SetAchievementProgress(string platformId, int steps, int totalSteps, CancellationToken cancelToken)
    {
        this.ThrowIfScripted();
        if (this.UnknownIds.Contains(platformId))
            throw new GameCenterException(GameCenterErrorCode.InvalidId, "unknown");

        this.Progress.Add((platformId, steps, totalSteps));
        return Task.CompletedTask;
    }

    public Task RevealAchievement(string platformId, CancellationToken cancelToken)
    {
        this.ThrowIfScripted();
        this.Reveals.Add(platformId);
        return Task.CompletedTask;
    }

    public Task SubmitScore(string platformId, long score, CancellationToken cancelToken)
    {
        this.ThrowIfScripted();
        this.Scores.Add((platformId, score));
        return Task.CompletedTask;
    }

    public Task ShowAchievements(CancellationToken cancelToken) => Task.CompletedTask;
    public Task ShowLeaderboard(string? platformId, CancellationToken cancelToken) => Task.CompletedTask;
    public Task<IReadOnlyList<LeaderboardEntry>> LoadScores(string platformId, LeaderboardScope scope, LeaderboardTimeScope timeScope, int maxResults, CancellationToken cancelToken)
        => Task.FromResult<IReadOnlyList<LeaderboardEntry>>([]);
    public Task<FriendsResult> LoadFriends(CancellationToken cancelToken)
        => Task.FromResult(new FriendsResult(FriendsAccessStatus.Granted, []));


    void ThrowIfScripted()
    {
        if (this.Offline)
            throw new GameCenterException(GameCenterErrorCode.Network, "offline");

        if (this.FailNext.TryDequeue(out var ex))
            throw ex;
    }
}

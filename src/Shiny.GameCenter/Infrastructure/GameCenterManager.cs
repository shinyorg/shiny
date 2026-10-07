using Microsoft.Extensions.Logging;
using Shiny.Net;

namespace Shiny.GameCenter.Infrastructure;


/// <summary>
/// The platform-independent half of <see cref="IGameCenterManager"/>: key mapping, progress bookkeeping and the
/// durable queue. Everything that talks to a service goes through <see cref="IGameServicesProvider"/>.
/// </summary>
/// <remarks>
/// <para>Progress is held as an absolute step count per achievement key. A call records the new count (only if it
/// moves forward), queues it, saves, then tries to send everything queued. Sending an absolute value makes every
/// retry idempotent: a crash after the service accepted it but before the queue was saved just re-sends the same
/// count, which the service ignores.</para>
/// <para>The trade-off is with other devices. An increment made offline counts from this device's last known
/// progress; if another device moved the achievement further in the meantime, the service keeps the higher value and
/// this device's increment is absorbed rather than added. Signing in refreshes known progress before the first
/// increment to keep that window small.</para>
/// </remarks>
class GameCenterManager : IGameCenterManager, IShinyStartupTask
{
    readonly IGameServicesProvider provider;
    readonly GameCenterOptions options;
    readonly GameCenterStateStore store;
    readonly IConnectivity? connectivity;
    readonly ILogger logger;
    readonly Dictionary<string, AchievementDefinition> achievementsByPlatformId = new();
    readonly SemaphoreSlim flushLock = new(1, 1);
    readonly object sync = new();
    readonly GameCenterState state;

    string? syncedPlayerId;


    public GameCenterManager(
        IGameServicesProvider provider,
        GameCenterOptions options,
        GameCenterStateStore store,
        ILogger<GameCenterManager> logger,
        IConnectivity? connectivity = null
    )
    {
        this.provider = provider;
        this.options = options;
        this.store = store;
        this.logger = logger;
        this.connectivity = connectivity;
        this.state = store.Load();

        foreach (var def in options.Achievements.Values)
        {
            var id = provider.GetPlatformId(def);
            if (id != null)
                this.achievementsByPlatformId[id] = def;
        }

        provider.PlayerChanged += (_, player) => this.SetPlayer(player);
        if (connectivity != null)
            connectivity.Changed += (_, _) => this.OnConnectivityChanged();
    }


    public GameServicePlatform Platform => this.provider.Platform;
    public GamePlayer? Player { get; private set; }
    public event EventHandler<GamePlayer?>? PlayerChanged;


    public int PendingCount
    {
        get
        {
            lock (this.sync)
                return this.state.Players.Values.Sum(x => x.PendingCount);
        }
    }


    public void Start()
    {
        if (!this.options.SignInOnStartup)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var player = await this.provider
                    .Authenticate(this.options.PresentSignInOnStartup, CancellationToken.None)
                    .ConfigureAwait(false);

                this.SetPlayer(player);
            }
            catch (Exception ex)
            {
                this.logger.SilentSignInFailed(ex);
            }
        });
    }


    public async Task<GamePlayer?> SignInAsync(CancellationToken cancelToken = default)
    {
        var player = await this.provider.Authenticate(true, cancelToken).ConfigureAwait(false);
        this.SetPlayer(player);
        return player;
    }


    public async Task<IReadOnlyList<Achievement>> GetAchievementsAsync(bool forceReload = false, CancellationToken cancelToken = default)
    {
        var player = this.RequirePlayer();
        var remote = await this.provider.LoadAchievements(forceReload, cancelToken).ConfigureAwait(false);

        List<Achievement> result;
        lock (this.sync)
        {
            var ps = this.state.For(player.Id);
            this.Merge(ps, remote);

            result = remote
                .Select(x =>
                {
                    var def = this.achievementsByPlatformId.GetValueOrDefault(x.Id);
                    var key = def?.Key ?? x.Id;
                    var total = def?.TotalSteps ?? x.TotalSteps ?? 1;
                    var steps = Math.Min(total, Math.Max(ps.GetSteps(key), ToSteps(x, total)));
                    return new Achievement(key, x.Id, x.Title, x.Description, steps, total, steps >= total, x.IsHidden && steps == 0);
                })
                .ToList();

            this.Save();
        }
        this.syncedPlayerId = player.Id;
        return result;
    }


    public Task UnlockAsync(string achievementKey, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementKey);
        var def = this.GetAchievement(achievementKey);
        return this.RecordProgress(def, def.TotalSteps, cancelToken);
    }


    public Task SetProgressAsync(string achievementKey, int steps, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementKey);
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        return this.RecordProgress(this.GetAchievement(achievementKey, true), steps, cancelToken);
    }


    public async Task IncrementAsync(string achievementKey, int steps = 1, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(steps, 1);
        var def = this.GetAchievement(achievementKey, true);

        await this.EnsureSynced(cancelToken).ConfigureAwait(false);

        int target;
        lock (this.sync)
            target = this.state.For(this.Player?.Id).GetSteps(def.Key) + steps;

        await this.RecordProgress(def, target, cancelToken).ConfigureAwait(false);
    }


    public async Task RevealAsync(string achievementKey, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(achievementKey);
        lock (this.sync)
        {
            var ps = this.state.For(this.Player?.Id);

            // anything with progress is already visible
            if (ps.GetSteps(achievementKey) > 0 || !ps.PendingReveals.Add(achievementKey))
                return;

            this.Save();
        }
        await this.FlushAsync(cancelToken).ConfigureAwait(false);
    }


    public async Task SubmitScoreAsync(string leaderboardKey, long score, CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaderboardKey);
        lock (this.sync)
        {
            var scores = this.state.For(this.Player?.Id).PendingScores;
            scores.Add(new PendingScore(leaderboardKey, score, DateTimeOffset.UtcNow));

            var overflow = scores.Count - Math.Max(1, this.options.MaxQueuedScores);
            if (overflow > 0)
                scores.RemoveRange(0, overflow);

            this.Save();
        }
        await this.FlushAsync(cancelToken).ConfigureAwait(false);
    }


    public Task ShowAchievementsAsync(CancellationToken cancelToken = default)
        => this.provider.ShowAchievements(cancelToken);


    public Task ShowLeaderboardAsync(string? leaderboardKey = null, CancellationToken cancelToken = default)
    {
        var id = leaderboardKey == null ? null : this.GetLeaderboardId(leaderboardKey);
        return this.provider.ShowLeaderboard(id, cancelToken);
    }


    public Task<IReadOnlyList<LeaderboardEntry>> GetScoresAsync(
        string leaderboardKey,
        LeaderboardScope scope = LeaderboardScope.Global,
        LeaderboardTimeScope timeScope = LeaderboardTimeScope.AllTime,
        int maxResults = 25,
        CancellationToken cancelToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaderboardKey);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);
        this.RequirePlayer();

        var id = this.GetLeaderboardId(leaderboardKey);
        return this.provider.LoadScores(id, scope, timeScope, maxResults, cancelToken);
    }


    public Task<FriendsResult> GetFriendsAsync(CancellationToken cancelToken = default)
    {
        this.RequirePlayer();
        return this.provider.LoadFriends(cancelToken);
    }


    public async Task FlushAsync(CancellationToken cancelToken = default)
    {
        await this.flushLock.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            var player = this.Player;
            if (player == null)
                return;

            List<KeyValuePair<string, int>> progress;
            List<string> reveals;
            List<PendingScore> scores;
            PlayerState ps;

            lock (this.sync)
            {
                ps = this.state.For(player.Id);
                progress = ps.PendingProgress.ToList();
                reveals = ps.PendingReveals.ToList();
                scores = ps.PendingScores.ToList();
            }

            foreach (var (key, steps) in progress)
            {
                var def = this.GetAchievement(key);
                var id = this.provider.GetPlatformId(def);

                var ok = await this.TrySend(key, ps, id, async () =>
                {
                    await this.provider.SetAchievementProgress(id!, steps, def.TotalSteps, cancelToken).ConfigureAwait(false);
                }, () =>
                {
                    // a newer, higher value may have been queued while this one was in flight - keep that one
                    if (ps.PendingProgress.TryGetValue(key, out var current) && current <= steps)
                        ps.PendingProgress.Remove(key);
                }).ConfigureAwait(false);

                if (!ok)
                    return;
            }

            foreach (var key in reveals)
            {
                var id = this.provider.GetPlatformId(this.GetAchievement(key));
                var ok = await this.TrySend(
                    key,
                    ps,
                    id,
                    () => this.provider.RevealAchievement(id!, cancelToken),
                    () => ps.PendingReveals.Remove(key)
                ).ConfigureAwait(false);

                if (!ok)
                    return;
            }

            foreach (var score in scores)
            {
                var id = this.provider.GetPlatformId(this.GetLeaderboard(score.LeaderboardKey));
                var ok = await this.TrySend(
                    score.LeaderboardKey,
                    ps,
                    id,
                    () => this.provider.SubmitScore(id!, score.Score, cancelToken),
                    () => ps.PendingScores.Remove(score)
                ).ConfigureAwait(false);

                if (!ok)
                    return;
            }
        }
        finally
        {
            this.flushLock.Release();
        }
    }


    /// <summary>Sends one queued item. Returns false when the flush should stop and keep the rest for later.</summary>
    async Task<bool> TrySend(string key, PlayerState ps, string? platformId, Func<Task> send, Action remove)
    {
        if (platformId == null)
        {
            // registered, but not on this service (e.g. an Apple-only achievement on Android)
            this.logger.NotOnPlatform(key);
            this.Dequeue(remove);
            return true;
        }

        try
        {
            await send().ConfigureAwait(false);
            this.Dequeue(remove);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (GameCenterException ex) when (ex.ErrorCode == GameCenterErrorCode.InvalidId)
        {
            this.logger.QueuedItemDropped(key, ex);
            this.Dequeue(remove);
            return true;
        }
        catch (Exception ex)
        {
            int remaining;
            lock (this.sync)
                remaining = ps.PendingCount;

            this.logger.FlushStopped(remaining, ex);
            return false;
        }
    }


    void Dequeue(Action remove)
    {
        lock (this.sync)
        {
            remove();
            this.Save();
        }
    }


    async Task RecordProgress(AchievementDefinition def, int steps, CancellationToken cancelToken)
    {
        steps = Math.Min(steps, def.TotalSteps);
        lock (this.sync)
        {
            var ps = this.state.For(this.Player?.Id);
            if (!ps.RecordSteps(def.Key, steps))
                return;

            ps.PendingProgress[def.Key] = Math.Max(steps, ps.PendingProgress.GetValueOrDefault(def.Key));
            ps.PendingReveals.Remove(def.Key);
            this.Save();
        }
        await this.FlushAsync(cancelToken).ConfigureAwait(false);
    }


    /// <summary>Refreshes known progress from the service once per signed-in player, before the first increment.</summary>
    async Task EnsureSynced(CancellationToken cancelToken)
    {
        var player = this.Player;
        if (player == null || this.syncedPlayerId == player.Id)
            return;

        try
        {
            await this.GetAchievementsAsync(false, cancelToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.SyncFailed(ex);
        }
    }


    void Merge(PlayerState ps, IReadOnlyList<PlatformAchievement> remote)
    {
        foreach (var x in remote)
        {
            var def = this.achievementsByPlatformId.GetValueOrDefault(x.Id);
            var key = def?.Key ?? x.Id;
            var total = def?.TotalSteps ?? x.TotalSteps ?? 1;
            ps.RecordSteps(key, Math.Min(total, ToSteps(x, total)));
        }
    }


    static int ToSteps(PlatformAchievement x, int totalSteps)
    {
        if (x.IsUnlocked)
            return totalSteps;

        if (x.CurrentSteps is int steps)
            return steps;

        if (x.PercentComplete is double percent)
            return (int)Math.Floor(percent * totalSteps / 100d);

        return 0;
    }


    void SetPlayer(GamePlayer? player)
    {
        lock (this.sync)
        {
            if (this.Player?.Id == player?.Id)
            {
                // same account - pick up a changed display name without announcing a new player
                this.Player = player;
                return;
            }

            this.Player = player;
            this.syncedPlayerId = null;
            if (player != null && this.state.AdoptAnonymous(player.Id))
                this.Save();
        }

        this.logger.PlayerChanged(player?.Id);
        this.PlayerChanged?.Invoke(this, player);

        if (player != null)
            this.FlushInBackground();
    }


    void OnConnectivityChanged()
    {
        if (this.connectivity?.Access is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet)
            this.FlushInBackground();
    }


    void FlushInBackground() => _ = Task.Run(async () =>
    {
        try
        {
            await this.FlushAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.FlushStopped(this.PendingCount, ex);
        }
    });


    GamePlayer RequirePlayer()
        => this.Player ?? throw new GameCenterException(GameCenterErrorCode.NotAuthenticated, "No player is signed in - call SignInAsync first");


    AchievementDefinition GetAchievement(string key, bool requireRegistered = false)
    {
        if (this.options.Achievements.TryGetValue(key, out var def))
            return def;

        if (requireRegistered)
            throw new ArgumentException($"Achievement '{key}' must be registered with GameCenterOptions.AddAchievement (with its total steps) to track progress", nameof(key));

        // unregistered keys are the platform id itself
        return new AchievementDefinition(key, key, key, 1);
    }


    LeaderboardDefinition GetLeaderboard(string key)
        => this.options.Leaderboards.GetValueOrDefault(key) ?? new LeaderboardDefinition(key, key, key);


    string GetLeaderboardId(string key)
        => this.provider.GetPlatformId(this.GetLeaderboard(key))
           ?? throw new GameCenterException(GameCenterErrorCode.InvalidId, $"Leaderboard '{key}' is not set up for {this.Platform}");


    // called under this.sync
    void Save()
    {
        try
        {
            this.store.Save(this.state);
        }
        catch (Exception ex)
        {
            // the in-memory queue still sends - only a restart before then would lose it
            this.logger.SaveFailed(this.store.FilePath, ex);
        }
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Shiny.GameCenter.Infrastructure;
using Xunit;

namespace Shiny.GameCenter.Tests;


public class ManagerTests : IDisposable
{
    static readonly GamePlayer Alice = new("alice", "Alice", GameServicePlatform.Custom);
    static readonly GamePlayer Bob = new("bob", "Bob", GameServicePlatform.Custom);

    readonly string dir = Path.Combine(Path.GetTempPath(), "shiny-gc-" + Guid.NewGuid().ToString("N"));
    readonly FakeProvider provider = new();
    readonly GameCenterOptions options = new GameCenterOptions()
        .AddAchievement("first_win", appleId: "apple.first", googleId: "g_first")
        .AddAchievement("ten_games", appleId: "apple.ten", googleId: "g_ten", totalSteps: 10)
        .AddAchievement("apple_only", appleId: "apple.only")
        .AddLeaderboard("high", appleId: "apple.high", googleId: "g_high");


    public void Dispose()
    {
        if (Directory.Exists(this.dir))
            Directory.Delete(this.dir, true);
    }


    GameCenterManager Create() => new(
        this.provider,
        this.options,
        new GameCenterStateStore(Path.Combine(this.dir, "state.json")),
        NullLogger<GameCenterManager>.Instance
    );


    static async Task<GameCenterManager> SignedIn(GameCenterManager manager, FakeProvider provider, GamePlayer player)
    {
        provider.SignedIn = player;
        await manager.SignInAsync();
        await manager.FlushAsync();
        return manager;
    }


    [Fact]
    public async Task Unlock_WhileSignedOut_IsQueuedAndSentOnSignIn()
    {
        var manager = this.Create();
        await manager.UnlockAsync("first_win");

        Assert.Empty(this.provider.Progress);
        Assert.Equal(1, manager.PendingCount);

        await SignedIn(manager, this.provider, Alice);

        Assert.Equal(("g_first", 1, 1), Assert.Single(this.provider.Progress));
        Assert.Equal(0, manager.PendingCount);
    }


    [Fact]
    public async Task Unlock_Twice_SendsOnce()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        await manager.UnlockAsync("first_win");
        await manager.UnlockAsync("first_win");

        Assert.Single(this.provider.Progress);
    }


    [Fact]
    public async Task SetProgress_NeverGoesBackwards_AndClampsToTotal()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        await manager.SetProgressAsync("ten_games", 5);
        await manager.SetProgressAsync("ten_games", 3);
        await manager.SetProgressAsync("ten_games", 50);

        Assert.Equal([("g_ten", 5, 10), ("g_ten", 10, 10)], this.provider.Progress);
    }


    [Fact]
    public async Task Increment_CountsFromRemoteProgress_AndSendsAbsolute()
    {
        this.provider.Remote.Add(new PlatformAchievement("g_ten", null, null, 4, 10, null, false, false));
        var manager = await SignedIn(this.Create(), this.provider, Alice);

        await manager.IncrementAsync("ten_games");
        await manager.IncrementAsync("ten_games", 2);

        Assert.Equal([("g_ten", 5, 10), ("g_ten", 7, 10)], this.provider.Progress);
        Assert.Equal(1, this.provider.LoadCount); // synced once per sign-in
    }


    [Fact]
    public async Task Increment_FromPercent_ConvertsToSteps()
    {
        // Apple reports percentages
        this.provider.Remote.Add(new PlatformAchievement("g_ten", null, null, null, null, 30, false, false));
        var manager = await SignedIn(this.Create(), this.provider, Alice);

        await manager.IncrementAsync("ten_games");

        Assert.Equal(("g_ten", 4, 10), Assert.Single(this.provider.Progress));
    }


    [Fact]
    public async Task Increment_Unregistered_Throws()
    {
        var manager = this.Create();
        await Assert.ThrowsAsync<ArgumentException>(() => manager.IncrementAsync("nope"));
    }


    [Fact]
    public async Task NetworkFailure_KeepsQueue_AcrossRestart()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        this.provider.Offline = true;

        await manager.UnlockAsync("first_win");
        await manager.SubmitScoreAsync("high", 42);

        Assert.Empty(this.provider.Progress);
        Assert.Equal(2, manager.PendingCount);

        // a fresh manager over the same file - the app was restarted, back online
        this.provider.Offline = false;
        var restarted = await SignedIn(this.Create(), this.provider, Alice);

        Assert.Equal(("g_first", 1, 1), Assert.Single(this.provider.Progress));
        Assert.Equal(("g_high", 42L), Assert.Single(this.provider.Scores));
        Assert.Equal(0, restarted.PendingCount);
    }


    [Fact]
    public async Task InvalidId_IsDropped_AndTheRestIsSent()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        this.provider.UnknownIds.Add("bogus_id");

        await manager.UnlockAsync("bogus_id");
        await manager.UnlockAsync("first_win");

        Assert.Equal(("g_first", 1, 1), Assert.Single(this.provider.Progress));
        Assert.Equal(0, manager.PendingCount);
    }


    [Fact]
    public async Task AchievementNotOnThisService_IsSkipped()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        await manager.UnlockAsync("apple_only");

        Assert.Empty(this.provider.Progress);
        Assert.Equal(0, manager.PendingCount);
    }


    [Fact]
    public async Task Reveal_AfterProgress_IsSkipped()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        await manager.SetProgressAsync("ten_games", 1);
        await manager.RevealAsync("ten_games");
        await manager.RevealAsync("first_win");

        Assert.Equal(["g_first"], this.provider.Reveals);
    }


    [Fact]
    public async Task ScoreQueue_DropsOldest_WhenFull()
    {
        this.options.MaxQueuedScores = 2;
        var manager = this.Create();
        await manager.SubmitScoreAsync("high", 1);
        await manager.SubmitScoreAsync("high", 2);
        await manager.SubmitScoreAsync("high", 3);

        await SignedIn(manager, this.provider, Alice);

        Assert.Equal([("g_high", 2L), ("g_high", 3L)], this.provider.Scores);
    }


    [Fact]
    public async Task PendingWork_StaysWithThePlayerWhoEarnedIt()
    {
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        this.provider.Offline = true;
        await manager.UnlockAsync("first_win");

        // Bob signs in on the same device - Alice's unlock is not his
        this.provider.Offline = false;
        this.provider.ChangePlayer(Bob);
        await manager.FlushAsync();
        Assert.Empty(this.provider.Progress);

        this.provider.ChangePlayer(Alice);
        await manager.FlushAsync();
        Assert.Equal(("g_first", 1, 1), Assert.Single(this.provider.Progress));
    }


    [Fact]
    public async Task GetAchievements_MergesUnsentLocalProgress()
    {
        this.provider.Remote.Add(new PlatformAchievement("g_ten", "Ten", "Play ten", 2, 10, null, false, false));
        var manager = await SignedIn(this.Create(), this.provider, Alice);
        this.provider.Offline = true;
        await manager.SetProgressAsync("ten_games", 6);

        var a = Assert.Single(await manager.GetAchievementsAsync());

        Assert.Equal("ten_games", a.Key);
        Assert.Equal(6, a.CurrentSteps);
        Assert.Equal(60, a.PercentComplete);
        Assert.False(a.IsUnlocked);
    }


    [Fact]
    public async Task CorruptStateFile_StartsClean()
    {
        Directory.CreateDirectory(this.dir);
        await File.WriteAllTextAsync(Path.Combine(this.dir, "state.json"), "{ not json");

        var manager = this.Create();
        Assert.Equal(0, manager.PendingCount);
    }
}

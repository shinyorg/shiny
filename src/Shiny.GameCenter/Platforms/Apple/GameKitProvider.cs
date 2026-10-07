using Foundation;
using GameKit;
using Microsoft.Extensions.Logging;
using Shiny.GameCenter.Infrastructure;
#if MACOS
using AppKit;
using PlatformViewController = AppKit.NSViewController;
#else
using UIKit;
using PlatformViewController = UIKit.UIViewController;
#endif

namespace Shiny.GameCenter;


/// <summary>
/// Apple Game Center through GameKit, on iOS, Mac Catalyst and macOS.
/// </summary>
/// <remarks>
/// <para>GameKit signs in through one handler that it calls whenever the player's state changes - at launch, after
/// the sign-in sheet closes, and when the player signs in or out in Settings. The handler is installed on the first
/// <see cref="Authenticate"/> and kept for the life of the app. When GameKit hands it a sign-in view controller, that
/// controller is presented straight away for an interactive sign-in, or held until one is asked for.</para>
/// <para>Game Center counts progress as a percentage; the manager converts from steps before calling in.</para>
/// </remarks>
public class GameKitProvider(GameCenterOptions options, ILogger<GameKitProvider> logger) : IGameServicesProvider
{
    readonly object sync = new();
    bool handlerInstalled;
    bool presentRequested;
    PlatformViewController? heldSignInUi;
    TaskCompletionSource<GamePlayer?>? authWaiter;
    string? lastPlayerId;


    public GameServicePlatform Platform => GameServicePlatform.AppleGameCenter;
    public string? GetPlatformId(AchievementDefinition definition) => definition.AppleId;
    public string? GetPlatformId(LeaderboardDefinition definition) => definition.AppleId;
    public event EventHandler<GamePlayer?>? PlayerChanged;


    public Task<GamePlayer?> Authenticate(bool interactive, CancellationToken cancelToken)
    {
        PlatformViewController? present = null;
        TaskCompletionSource<GamePlayer?> waiter;

        lock (this.sync)
        {
            if (this.handlerInstalled && GKLocalPlayer.Local.Authenticated)
                return Task.FromResult<GamePlayer?>(ToPlayer());

            if (this.heldSignInUi != null)
            {
                if (!interactive)
                    return Task.FromResult<GamePlayer?>(null);

                // GameKit already gave us its sheet - show it; the handler fires again when it closes
                present = this.heldSignInUi;
                this.heldSignInUi = null;
            }

            if (this.handlerInstalled && present == null && this.authWaiter == null && !interactive)
            {
                // the handler has reported and nothing is pending - the player is signed out with no sheet on offer
                return Task.FromResult<GamePlayer?>(null);
            }

            this.presentRequested |= interactive;
            waiter = this.authWaiter ??= new TaskCompletionSource<GamePlayer?>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        if (present != null)
        {
            Present(present);
        }
        else if (!this.handlerInstalled || interactive)
        {
            // first call installs the handler; assigning it again asks GameKit to re-run authentication, which is how
            // an interactive call after an earlier cancel gets a new sheet
            lock (this.sync)
                this.handlerInstalled = true;

            NSRunLoop.Main.BeginInvokeOnMainThread(() => GKLocalPlayer.Local.AuthenticateHandler = this.OnAuthenticate);
        }
        return waiter.Task.WaitAsync(cancelToken);
    }


#if MACOS
    void OnAuthenticate(NSViewController? viewController, NSError? error)
#else
    void OnAuthenticate(UIViewController? viewController, NSError? error)
#endif
    {
        if (viewController != null)
        {
            bool present;
            lock (this.sync)
            {
                present = this.presentRequested;
                if (!present)
                    this.heldSignInUi = viewController;
            }

            if (present)
            {
                // keep the waiter pending - the handler fires again with the outcome once the sheet closes
                Present(viewController);
                return;
            }

            this.Complete(null);
            return;
        }

        if (error != null)
            logger.LogInformation("Game Center sign-in did not complete - {Code}: {Message}", error.Code, error.LocalizedDescription);

        var player = GKLocalPlayer.Local.Authenticated ? ToPlayer() : null;
        this.Complete(player);
    }


    void Complete(GamePlayer? player)
    {
        TaskCompletionSource<GamePlayer?>? waiter;
        bool changed;
        lock (this.sync)
        {
            waiter = this.authWaiter;
            this.authWaiter = null;
            this.presentRequested = false;

            changed = this.lastPlayerId != player?.Id;
            this.lastPlayerId = player?.Id;
        }

        waiter?.TrySetResult(player);
        if (changed)
            this.PlayerChanged?.Invoke(this, player);
    }


    public async Task<IReadOnlyList<PlatformAchievement>> LoadAchievements(bool forceReload, CancellationToken cancelToken)
    {
        // GameKit caches these itself and has no reload switch
        var descriptions = await Call(() => GKAchievementDescription.LoadAchievementDescriptionsAsync()).WaitAsync(cancelToken).ConfigureAwait(false);
        var progress = await Call(() => GKAchievement.LoadAchievementsAsync()).WaitAsync(cancelToken).ConfigureAwait(false);

        var byId = (progress ?? [])
            .Where(x => x.Identifier != null)
            .ToDictionary(x => x.Identifier!, x => x);

        return (descriptions ?? [])
            .Where(d => d.Identifier != null)
            .Select(d =>
            {
                byId.TryGetValue(d.Identifier!, out var a);
                var completed = a?.Completed ?? false;
                return new PlatformAchievement(
                    d.Identifier!,
                    d.Title,
                    completed ? d.AchievedDescription : d.UnachievedDescription,
                    null,
                    null,
                    a?.PercentComplete ?? 0,
                    completed,
                    d.Hidden
                );
            })
            .ToList();
    }


    public Task SetAchievementProgress(string platformId, int steps, int totalSteps, CancellationToken cancelToken)
    {
        var percent = totalSteps <= 0 ? 100d : Math.Min(100d, steps * 100d / totalSteps);
        var achievement = new GKAchievement(platformId)
        {
            PercentComplete = percent,
            ShowsCompletionBanner = options.ShowCompletionBanner
        };
        return Call(() => GKAchievement.ReportAchievementsAsync([achievement])).WaitAsync(cancelToken);
    }


    // Game Center has no reveal - a hidden achievement appears once it has progress
    public Task RevealAchievement(string platformId, CancellationToken cancelToken) => Task.CompletedTask;


    public Task SubmitScore(string platformId, long score, CancellationToken cancelToken)
        => Call(() => GKLeaderboard.SubmitScoreAsync((nint)score, 0, GKLocalPlayer.Local, [platformId])).WaitAsync(cancelToken);


    public Task ShowAchievements(CancellationToken cancelToken)
        => TriggerAccessPoint(done => GKAccessPoint.Shared.TriggerAccessPoint(GKGameCenterViewControllerState.Achievements, done), cancelToken);


    public Task ShowLeaderboard(string? platformId, CancellationToken cancelToken)
    {
        if (platformId == null)
            return TriggerAccessPoint(done => GKAccessPoint.Shared.TriggerAccessPoint(GKGameCenterViewControllerState.Leaderboards, done), cancelToken);

#if MACOS
        return TriggerAccessPoint(done => GKAccessPoint.Shared.TriggerAccessPoint(platformId, GKLeaderboardPlayerScope.Global, GKLeaderboardTimeScope.AllTime, done), cancelToken);
#else
#if MACCATALYST
        if (OperatingSystem.IsMacCatalystVersionAtLeast(18))
#else
        if (OperatingSystem.IsIOSVersionAtLeast(18))
#endif
            return ShowLeaderboardThroughAccessPoint(platformId, cancelToken);

        // opening one leaderboard through the access point arrived in 18; GKGameCenterViewController (deprecated in 26) covers 15-17
        return ShowLegacyLeaderboard(platformId, cancelToken);
#endif
    }


    public async Task<IReadOnlyList<LeaderboardEntry>> LoadScores(string platformId, LeaderboardScope scope, LeaderboardTimeScope timeScope, int maxResults, CancellationToken cancelToken)
    {
        var boards = await Call(() => GKLeaderboard.LoadLeaderboardsAsync([platformId])).WaitAsync(cancelToken).ConfigureAwait(false);
        var board = boards?.FirstOrDefault()
            ?? throw new GameCenterException(GameCenterErrorCode.InvalidId, $"Game Center has no leaderboard '{platformId}'");

        var playerScope = scope == LeaderboardScope.Friends ? GKLeaderboardPlayerScope.FriendsOnly : GKLeaderboardPlayerScope.Global;
        var time = timeScope switch
        {
            LeaderboardTimeScope.Today => GKLeaderboardTimeScope.Today,
            LeaderboardTimeScope.Week => GKLeaderboardTimeScope.Week,
            _ => GKLeaderboardTimeScope.AllTime
        };

        var result = await Call(() => board.LoadEntriesAsync(playerScope, time, new NSRange(1, maxResults))).WaitAsync(cancelToken).ConfigureAwait(false);
        var me = GKLocalPlayer.Local.GamePlayerId;

        return (result.Entries ?? [])
            .Select(e => new LeaderboardEntry(
                ToPlayer(e.Player),
                (long)e.Score,
                e.FormattedScore,
                (int)e.Rank,
                e.Player.GamePlayerId == me
            ))
            .ToList();
    }


    public async Task<FriendsResult> LoadFriends(CancellationToken cancelToken)
    {
        // GameKit terminates the app if the usage string is missing - fail like a misconfiguration instead
        if (NSBundle.MainBundle.ObjectForInfoDictionary("NSGKFriendListUsageDescription") == null)
            throw new GameCenterException(GameCenterErrorCode.NotConfigured, "Add NSGKFriendListUsageDescription to Info.plist to read the friends list");

        var status = await Call(() => GKLocalPlayer.Local.LoadFriendsAuthorizationStatusAsync()).WaitAsync(cancelToken).ConfigureAwait(false);
        switch (status)
        {
            case GKFriendsAuthorizationStatus.Denied:
                return new FriendsResult(FriendsAccessStatus.Denied, []);

            case GKFriendsAuthorizationStatus.Restricted:
                return new FriendsResult(FriendsAccessStatus.Restricted, []);
        }

        try
        {
            // prompts the player the first time (NotDetermined)
            var friends = await Call(() => GKLocalPlayer.Local.LoadFriendsListAsync()).WaitAsync(cancelToken).ConfigureAwait(false);
            return new FriendsResult(FriendsAccessStatus.Granted, (friends ?? []).Select(x => ToPlayer(x)).ToList());
        }
        catch (GameCenterException ex) when (ex.NativeErrorCode == ((long)GKError.FriendListDenied).ToString())
        {
            return new FriendsResult(FriendsAccessStatus.Denied, []);
        }
        catch (GameCenterException ex) when (ex.NativeErrorCode == ((long)GKError.FriendListRestricted).ToString())
        {
            return new FriendsResult(FriendsAccessStatus.Restricted, []);
        }
    }


    static GamePlayer ToPlayer() => ToPlayer(GKLocalPlayer.Local);
    static GamePlayer ToPlayer(GKPlayer player) => new(player.GamePlayerId, player.DisplayName ?? player.Alias ?? "", GameServicePlatform.AppleGameCenter);


    /// <summary>
    /// Opens the Game Center dashboard through the access point - Apple's replacement for GKGameCenterViewController
    /// (deprecated in the 26 SDKs). Completes when GameKit calls back once its UI is up.
    /// </summary>
    static Task TriggerAccessPoint(Action<Action> trigger, CancellationToken cancelToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NSRunLoop.Main.BeginInvokeOnMainThread(() =>
        {
            try
            {
                trigger(() => tcs.TrySetResult());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task.WaitAsync(cancelToken);
    }


#if !MACOS
    [System.Runtime.Versioning.SupportedOSPlatform("ios18.0")]
    [System.Runtime.Versioning.SupportedOSPlatform("maccatalyst18.0")]
    static Task ShowLeaderboardThroughAccessPoint(string platformId, CancellationToken cancelToken)
        => TriggerAccessPoint(done => GKAccessPoint.Shared.TriggerAccessPoint(platformId, GKLeaderboardPlayerScope.Global, GKLeaderboardTimeScope.AllTime, done), cancelToken);


    [System.Runtime.Versioning.UnsupportedOSPlatform("ios18.0")]
    [System.Runtime.Versioning.UnsupportedOSPlatform("maccatalyst18.0")]
    static Task ShowLegacyLeaderboard(string platformId, CancellationToken cancelToken)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        NSRunLoop.Main.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var vc = new GKGameCenterViewController(platformId, GKLeaderboardPlayerScope.Global, GKLeaderboardTimeScope.AllTime);
                vc.Finished += (_, _) =>
                {
                    vc.DismissViewController(true, null);
                    tcs.TrySetResult();
                };
                GetTopViewController().PresentViewController(vc, true, null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task.WaitAsync(cancelToken);
    }
#endif


    static void Present(PlatformViewController vc) => NSRunLoop.Main.BeginInvokeOnMainThread(() =>
    {
#if MACOS
        GetWindow().ContentViewController?.PresentViewControllerAsSheet(vc);
#else
        GetTopViewController().PresentViewController(vc, true, null);
#endif
    });


#if MACOS
    static NSWindow GetWindow()
        => NSApplication.SharedApplication.KeyWindow
           ?? NSApplication.SharedApplication.MainWindow
           ?? throw new GameCenterException(GameCenterErrorCode.NoUserInterface, "No window is available to present Game Center");
#else
    static UIViewController GetTopViewController()
    {
        var window = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .OrderByDescending(x => x.ActivationState == UISceneActivationState.ForegroundActive)
            .SelectMany(x => x.Windows)
            .FirstOrDefault(x => x.IsKeyWindow);

        var vc = window?.RootViewController
            ?? throw new GameCenterException(GameCenterErrorCode.NoUserInterface, "No window is available to present Game Center");

        while (vc.PresentedViewController != null)
            vc = vc.PresentedViewController;

        return vc;
    }
#endif


    /// <summary>Runs a GameKit call, translating its NSError into <see cref="GameCenterException"/></summary>
    static async Task<T> Call<T>(Func<Task<T>> call)
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (NSErrorException ex)
        {
            throw ToException(ex);
        }
    }


    static async Task Call(Func<Task> call)
    {
        try
        {
            await call().ConfigureAwait(false);
        }
        catch (NSErrorException ex)
        {
            throw ToException(ex);
        }
    }


    static GameCenterException ToException(NSErrorException ex)
    {
        var error = ex.Error;
        var code = error.Domain == GKErrorExtensions.GetDomain(GKError.Unknown)
            ? (GKError)(long)error.Code switch
            {
                GKError.NotAuthenticated or GKError.NotAuthorized => GameCenterErrorCode.NotAuthenticated,
                GKError.CommunicationsFailure or GKError.ConnectionTimeout => GameCenterErrorCode.Network,
                GKError.GameUnrecognized or GKError.ApiNotAvailable => GameCenterErrorCode.NotConfigured,
                GKError.NotSupported or GKError.Underage or GKError.ParentalControlsBlocked => GameCenterErrorCode.Unavailable,
                GKError.InvalidParameter or GKError.InvalidPlayer => GameCenterErrorCode.InvalidId,
                _ => GameCenterErrorCode.Unknown
            }
            : GameCenterErrorCode.Unknown;

        return new GameCenterException(code, $"Game Center request failed ({error.Code}): {error.LocalizedDescription}", ex)
        {
            NativeErrorCode = error.Code.ToString()
        };
    }
}

using Android.App;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Android.Gms.Games;
using Android.Gms.Games.Achievement;
using Android.Runtime;
using Android.Gms.Games.Leaderboard;
using Microsoft.Extensions.Logging;
using Shiny.GameCenter.Infrastructure;

namespace Shiny.GameCenter;


/// <summary>
/// Google Play Games Services v2.
/// </summary>
/// <remarks>
/// <para>v2 signs the player in automatically at launch, so a silent <see cref="Authenticate"/> only asks whether that
/// worked. The <c>*Immediate</c> client calls are used throughout: the plain ones queue inside Play Games and report
/// success before the server has the data, which would let the manager's queue drop work that was never sent.</para>
/// <para>Every client is bound to an activity, so each call resolves the current one (waiting briefly while the app
/// is between activities).</para>
/// </remarks>
public class PlayGamesProvider(AndroidPlatform platform, ILogger<PlayGamesProvider> logger) : IGameServicesProvider
{
    const int RequestCodeUi = 9101;
    const int RequestCodeFriendsConsent = 9102;
    static readonly TimeSpan ActivityWaitTimeout = TimeSpan.FromSeconds(5);

    // Achievement.TYPE_INCREMENTAL / STATE_UNLOCKED / STATE_HIDDEN - the interface carrying them is not bound
    const int AchievementTypeIncremental = 1;
    const int AchievementStateUnlocked = 0;
    const int AchievementStateHidden = 2;

    // GamesClientStatusCodes - not surfaced by the binding as constants
    const int StatusAchievementUnknown = 3001;
    const int StatusAchievementNotIncremental = 3002;

    readonly object initSync = new();
    bool initialized;


    public GameServicePlatform Platform => GameServicePlatform.GooglePlayGames;
    public string? GetPlatformId(AchievementDefinition definition) => definition.GoogleId;
    public string? GetPlatformId(LeaderboardDefinition definition) => definition.GoogleId;

    // Play Games v2 has no listener for account changes - the manager learns of them through Authenticate
    public event EventHandler<GamePlayer?>? PlayerChanged { add { } remove { } }


    public async Task<GamePlayer?> Authenticate(bool interactive, CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        var client = PlayGames.GetGamesSignInClient(activity);

        var result = await Run(() => client.IsAuthenticated(), cancelToken).ConfigureAwait(false) as AuthenticationResult;
        if (result?.IsAuthenticated != true && interactive)
            result = await Run(() => client.SignIn(), cancelToken).ConfigureAwait(false) as AuthenticationResult;

        if (result?.IsAuthenticated != true)
            return null;

        var player = await Run(() => PlayGamesJni.GetCurrentPlayer(PlayGamesJni.GetPlayersClient(activity)), cancelToken).ConfigureAwait(false);
        return player == null ? null : ToPlayer(player);
    }


    public async Task<IReadOnlyList<PlatformAchievement>> LoadAchievements(bool forceReload, CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        var data = await Run(() => PlayGames.GetAchievementsClient(activity).Load(forceReload), cancelToken).ConfigureAwait(false) as AnnotatedData;

        var buffer = data?.Get() as AchievementBuffer;
        if (buffer == null)
            return [];

        try
        {
            var list = new List<PlatformAchievement>(buffer.Count);
            for (var i = 0; i < buffer.Count; i++)
            {
                // the Achievement interface is not bound; the buffer hands out AchievementRef
                var a = buffer.Get(i)?.JavaCast<AchievementRef>();
                if (a == null)
                    continue;

                var incremental = a.Type == AchievementTypeIncremental;
                list.Add(new PlatformAchievement(
                    a.AchievementId,
                    a.Name,
                    a.Description,
                    incremental ? a.CurrentSteps : null,
                    incremental ? a.TotalSteps : null,
                    null,
                    a.State == AchievementStateUnlocked,
                    a.State == AchievementStateHidden
                ));
            }
            return list;
        }
        finally
        {
            buffer.Release();
        }
    }


    public async Task SetAchievementProgress(string platformId, int steps, int totalSteps, CancellationToken cancelToken)
    {
        var client = PlayGames.GetAchievementsClient(await this.GetActivity(cancelToken).ConfigureAwait(false));
        if (totalSteps <= 1)
        {
            if (steps >= 1)
                await Run(() => client.UnlockImmediate(platformId), cancelToken).ConfigureAwait(false);
        }
        else
        {
            // setSteps only ever raises progress, so a repeated or stale value is harmless
            await Run(() => client.SetStepsImmediate(platformId, steps), cancelToken).ConfigureAwait(false);
        }
    }


    public async Task RevealAchievement(string platformId, CancellationToken cancelToken)
    {
        var client = PlayGames.GetAchievementsClient(await this.GetActivity(cancelToken).ConfigureAwait(false));
        await Run(() => client.RevealImmediate(platformId), cancelToken).ConfigureAwait(false);
    }


    public async Task SubmitScore(string platformId, long score, CancellationToken cancelToken)
    {
        var client = PlayGames.GetLeaderboardsClient(await this.GetActivity(cancelToken).ConfigureAwait(false));
        await Run(() => client.SubmitScoreImmediate(platformId, score), cancelToken).ConfigureAwait(false);
    }


    public async Task ShowAchievements(CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        var intent = await Run(() => PlayGames.GetAchievementsClient(activity).GetAchievementsIntent(), cancelToken).ConfigureAwait(false);
        this.StartUi(activity, intent);
    }


    public async Task ShowLeaderboard(string? platformId, CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        var client = PlayGames.GetLeaderboardsClient(activity);
        var intent = await Run(
            () => platformId == null ? client.GetAllLeaderboardsIntent() : client.GetLeaderboardIntent(platformId),
            cancelToken
        ).ConfigureAwait(false);

        this.StartUi(activity, intent);
    }


    public async Task<IReadOnlyList<LeaderboardEntry>> LoadScores(string platformId, LeaderboardScope scope, LeaderboardTimeScope timeScope, int maxResults, CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        var client = PlayGames.GetLeaderboardsClient(activity);

        var span = timeScope switch
        {
            LeaderboardTimeScope.Today => LeaderboardVariant.TimeSpanDaily,
            LeaderboardTimeScope.Week => LeaderboardVariant.TimeSpanWeekly,
            _ => LeaderboardVariant.TimeSpanAllTime
        };
        var collection = scope == LeaderboardScope.Friends
            ? LeaderboardVariant.CollectionFriends
            : LeaderboardVariant.CollectionPublic;

        // Play Games caps a page at 25
        var data = await Run(
            () => client.LoadTopScores(platformId, span, collection, Math.Min(maxResults, 25)),
            cancelToken
        ).ConfigureAwait(false) as AnnotatedData;

        var scores = data?.Get() as LeaderboardsClientLeaderboardScores;
        var buffer = scores?.Scores;
        if (buffer == null)
        {
            scores?.Release();
            return [];
        }

        try
        {
            var me = await this.GetCurrentPlayerId(activity, cancelToken).ConfigureAwait(false);
            var list = new List<LeaderboardEntry>(buffer.Count);
            for (var i = 0; i < buffer.Count; i++)
            {
                var raw = buffer.Get(i);
                var s = raw?.JavaCast<LeaderboardScoreRef>();
                if (raw == null || s == null)
                    continue;

                var holder = PlayGamesJni.GetScoreHolder(raw);
                var player = holder == null
                    ? new GamePlayer("", s.ScoreHolderDisplayName ?? "", GameServicePlatform.GooglePlayGames)
                    : ToPlayer(holder);

                list.Add(new LeaderboardEntry(player, s.RawScore, s.DisplayScore, (int)s.Rank, me != null && player.Id == me));
            }
            return list;
        }
        finally
        {
            scores!.Release();
        }
    }


    public async Task<FriendsResult> LoadFriends(CancellationToken cancelToken)
    {
        var activity = await this.GetActivity(cancelToken).ConfigureAwait(false);
        try
        {
            var data = await Run(
                () => PlayGamesJni.LoadFriends(PlayGamesJni.GetPlayersClient(activity), 200, false),
                cancelToken
            ).ConfigureAwait(false) as AnnotatedData;

            var buffer = data?.Get() as PlayerBuffer;
            var friends = new List<GamePlayer>(buffer?.Count ?? 0);
            if (buffer != null)
            {
                try
                {
                    for (var i = 0; i < buffer.Count; i++)
                    {
                        if (buffer.Get(i) is { } p)
                            friends.Add(ToPlayer(p));
                    }
                }
                finally
                {
                    buffer.Release();
                }
            }
            return new FriendsResult(FriendsAccessStatus.Granted, friends);
        }
        catch (GameCenterException ex) when (ex.InnerException is ResolvableApiException resolvable)
        {
            // FriendsResolutionRequiredException - the player has not been asked yet; Play Games reports their answer
            // through an activity result, so ask and let the caller come back
            logger.LogDebug("Play Games needs friends-list consent - showing the consent dialog");
            platform.InvokeOnMainThread(() => resolvable.StartResolutionForResult(activity, RequestCodeFriendsConsent));
            return new FriendsResult(FriendsAccessStatus.ConsentRequested, []);
        }
    }


    async Task<string?> GetCurrentPlayerId(Activity activity, CancellationToken cancelToken)
    {
        try
        {
            var player = await Run(() => PlayGamesJni.GetCurrentPlayer(PlayGamesJni.GetPlayersClient(activity)), cancelToken).ConfigureAwait(false);
            return player == null ? null : PlayGamesJni.GetPlayerId(player);
        }
        catch (GameCenterException)
        {
            return null;
        }
    }


    void StartUi(Activity activity, Java.Lang.Object? intent)
    {
        if (intent is not Android.Content.Intent i)
            throw new GameCenterException(GameCenterErrorCode.Unknown, "Play Games did not return an intent for its UI");

        platform.InvokeOnMainThread(() => activity.StartActivityForResult(i, RequestCodeUi));
    }


    static GamePlayer ToPlayer(Java.Lang.Object player) => new(
        PlayGamesJni.GetPlayerId(player) ?? "",
        PlayGamesJni.GetDisplayName(player) ?? "",
        GameServicePlatform.GooglePlayGames
    );


    async Task<Activity> GetActivity(CancellationToken cancelToken)
    {
        this.EnsureInitialized();
        if (platform.CurrentActivity is { IsFinishing: false } current)
            return current;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(ActivityWaitTimeout);
        try
        {
            var changed = await platform.WaitForActivity(cancellationToken: cts.Token).ConfigureAwait(false);
            return changed.Activity;
        }
        catch (OperationCanceledException) when (!cancelToken.IsCancellationRequested)
        {
            throw new GameCenterException(GameCenterErrorCode.NoUserInterface, "No foreground activity is available for Play Games");
        }
    }


    void EnsureInitialized()
    {
        lock (this.initSync)
        {
            if (this.initialized)
                return;

            // Google asks for this in Application.onCreate; Shiny's startup runs at the same point
            PlayGamesSdk.Initialize(platform.AppContext);
            this.initialized = true;
        }
    }


    /// <summary>Awaits a Play Services task, translating its failures into <see cref="GameCenterException"/></summary>
    static async Task<Java.Lang.Object?> Run(Func<Android.Gms.Tasks.Task> start, CancellationToken cancelToken)
    {
        try
        {
            return await start().AsAsync<Java.Lang.Object>().WaitAsync(cancelToken).ConfigureAwait(false);
        }
        catch (ApiException ex)
        {
            var code = ex.StatusCode switch
            {
                CommonStatusCodes.SignInRequired => GameCenterErrorCode.NotAuthenticated,
                CommonStatusCodes.NetworkError or CommonStatusCodes.Timeout => GameCenterErrorCode.Network,
                CommonStatusCodes.DeveloperError => GameCenterErrorCode.NotConfigured,
                CommonStatusCodes.ApiNotConnected => GameCenterErrorCode.Unavailable,
                StatusAchievementUnknown or StatusAchievementNotIncremental => GameCenterErrorCode.InvalidId,
                _ => GameCenterErrorCode.Unknown
            };
            throw new GameCenterException(code, $"Play Games request failed ({ex.StatusCode}): {ex.Message}", ex)
            {
                NativeErrorCode = ex.StatusCode.ToString()
            };
        }
    }
}

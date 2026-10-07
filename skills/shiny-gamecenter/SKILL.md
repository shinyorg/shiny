---
name: shiny-gamecenter
description: Generate code using Shiny.GameCenter for cross-platform achievements, leaderboards and friends over Apple Game Center (iOS, Mac Catalyst, macOS) and Google Play Games Services v2 (Android) - unlocking and incrementing achievements, submitting and loading scores, friends-only leaderboards, the friends list, sign-in, and the durable offline queue
auto_invoke: true
triggers:
  - game center
  - gamecenter
  - gamekit
  - play games
  - play games services
  - google play games
  - achievements
  - unlock achievement
  - incremental achievement
  - leaderboard
  - leaderboards
  - submit score
  - high score
  - friends leaderboard
  - friends list
  - GKLocalPlayer
  - GKAchievement
  - GKLeaderboard
  - GKAccessPoint
  - PlayGamesSdk
  - IGameCenterManager
  - GameCenterOptions
  - AddGameCenter
  - IGameServicesProvider
  - LeaderboardEntry
  - FriendsResult
---

# Shiny.GameCenter

Achievements, leaderboards and friends over **Apple Game Center** (GameKit - iOS 15+, Mac Catalyst 15+, macOS) and
**Google Play Games Services v2** (Android API 26+), behind one `IGameCenterManager`. The `net10.0` target registers a
manager that records locally and never sends (no service there).

Namespace `Shiny.GameCenter`; `AddGameCenter` lives in `Shiny`.

## Rules - always follow

1. **Use logical keys.** Register every achievement and leaderboard once with both platform ids, then use the key
   everywhere. An unregistered key is passed through as the platform id itself.
2. **Incremental achievements must be registered with `totalSteps`**, matching the step count in the Play Console.
   Apple counts in percent, so the total has to be known on the device. `IncrementAsync`/`SetProgressAsync` on an
   unregistered key throws `ArgumentException`.
3. **Do not wrap progress calls in try/catch or retry loops.** `UnlockAsync`, `SetProgressAsync`, `IncrementAsync`,
   `RevealAsync` and `SubmitScoreAsync` record to a persisted queue and do not throw for offline / signed-out. The
   queue flushes on sign-in, on connectivity returning, and after every call.
4. **Progress is absolute and only moves forward.** Calls that would not raise progress are skipped. Increments made
   offline count from this device's last known progress (refreshed from the service once per sign-in).
5. `GetAchievementsAsync`, `GetScoresAsync` and `GetFriendsAsync` need a signed-in player - they throw
   `GameCenterException(NotAuthenticated)` otherwise. Check `manager.IsAuthenticated` or call `SignInAsync()` first.
6. Library code uses Shiny.Core only - no MAUI Essentials APIs.

## Registration

```csharp
using Shiny;

builder.Services.AddGameCenter(opts =>
{
    opts.AddAchievement("first_win", appleId: "com.mygame.firstwin", googleId: "CgkIxxxxxxxxEAIQAQ");
    opts.AddAchievement("play_100", appleId: "com.mygame.play100", googleId: "CgkIxxxxxxxxEAIQAg", totalSteps: 100);
    opts.AddLeaderboard("high_score", appleId: "com.mygame.highscore", googleId: "CgkIxxxxxxxxEAIQAw");

    opts.SignInOnStartup = true;          // default - silent sign-in at launch
    opts.PresentSignInOnStartup = false;  // Apple: show the Game Center sheet at launch (Apple's recommendation for games)
    opts.ShowCompletionBanner = true;     // Apple completion banner
    opts.MaxQueuedScores = 100;
});
```

Registers an `IShinyStartupTask`, so on MAUI it needs `.UseShiny()`.

## IGameCenterManager

```csharp
public interface IGameCenterManager
{
    GameServicePlatform Platform { get; }          // None | AppleGameCenter | GooglePlayGames | Custom
    GamePlayer? Player { get; }                    // Id, DisplayName, Platform
    bool IsAuthenticated { get; }
    event EventHandler<GamePlayer?>? PlayerChanged;
    int PendingCount { get; }

    Task<GamePlayer?> SignInAsync(CancellationToken ct = default);   // null = declined
    Task<IReadOnlyList<Achievement>> GetAchievementsAsync(bool forceReload = false, CancellationToken ct = default);
    Task UnlockAsync(string achievementKey, CancellationToken ct = default);
    Task SetProgressAsync(string achievementKey, int steps, CancellationToken ct = default);
    Task IncrementAsync(string achievementKey, int steps = 1, CancellationToken ct = default);
    Task RevealAsync(string achievementKey, CancellationToken ct = default);          // Google only; no-op on Apple
    Task SubmitScoreAsync(string leaderboardKey, long score, CancellationToken ct = default);
    Task ShowAchievementsAsync(CancellationToken ct = default);
    Task ShowLeaderboardAsync(string? leaderboardKey = null, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardEntry>> GetScoresAsync(string leaderboardKey,
        LeaderboardScope scope = LeaderboardScope.Global,          // Global | Friends
        LeaderboardTimeScope timeScope = LeaderboardTimeScope.AllTime,  // Today | Week | AllTime
        int maxResults = 25, CancellationToken ct = default);      // Google caps at 25
    Task<FriendsResult> GetFriendsAsync(CancellationToken ct = default);
    Task FlushAsync(CancellationToken ct = default);
}
```

`Achievement`: `Key`, `PlatformId`, `Title`, `Description`, `CurrentSteps`, `TotalSteps`, `PercentComplete`,
`IsUnlocked`, `IsHidden`. `LeaderboardEntry`: `Player`, `Score`, `FormattedScore`, `Rank`, `IsLocalPlayer`.

## Usage

```csharp
public class GameOverViewModel(IGameCenterManager gameCenter)
{
    public async Task OnGameOver(int score, bool won)
    {
        await gameCenter.SubmitScoreAsync("high_score", score);
        await gameCenter.IncrementAsync("play_100");
        if (won)
            await gameCenter.UnlockAsync("first_win");
    }
}
```

### Friends and friends' scores

```csharp
if (!gameCenter.IsAuthenticated && await gameCenter.SignInAsync() == null)
    return;

var friends = await gameCenter.GetFriendsAsync();
switch (friends.Status)
{
    case FriendsAccessStatus.Granted:          // friends.Friends populated
        break;
    case FriendsAccessStatus.ConsentRequested: // Google showed its consent dialog - call again when the app resumes
        break;
    case FriendsAccessStatus.Denied:           // Apple: player must change it in Settings
    case FriendsAccessStatus.Restricted:
        break;
}

var friendScores = await gameCenter.GetScoresAsync("high_score", LeaderboardScope.Friends, LeaderboardTimeScope.Week);
```

## Errors

`GameCenterException.ErrorCode`: `NotAuthenticated`, `Network`, `InvalidId`, `NotConfigured`, `Unavailable`,
`NoUserInterface`, `Unknown` (+ `NativeErrorCode`). Queued work that fails with `InvalidId` is dropped (logged as an
error); anything else stays queued.

## Platform setup

**Apple**
- Entitlement `com.apple.developer.game-center` = true (`Platforms/iOS/Entitlements.plist`); Game Center capability on the App ID.
- App Store Connect: create achievements/leaderboards under Features -> Game Center, and tick Game Center on the app version.
- `NSGKFriendListUsageDescription` in Info.plist for `GetFriendsAsync` (it throws `NotConfigured` without it, rather than GameKit terminating the app).
- Sandboxed macOS: `com.apple.security.network.client`.
- UI opens through `GKAccessPoint` (GKGameCenterViewController is deprecated in the 26 SDKs).

**Android**
```xml
<!-- AndroidManifest.xml, inside <application> -->
<meta-data android:name="com.google.android.gms.games.APP_ID" android:value="@string/game_services_project_id" />
```
```xml
<!-- Platforms/Android/Resources/values/strings.xml -->
<string name="game_services_project_id" translatable="false">123456789012</string>
```
- Register the SHA-1 of the debug, upload and Play App Signing keys as OAuth Android credentials. A missing one fails sign-in (status 10 -> `NotConfigured`).
- Until Play Games Services is published, only accounts on its Testers tab can sign in.

## Custom providers

Register an `IGameServicesProvider` before `AddGameCenter()` to plug in another service (Steam, a test double). The
manager keeps the key mapping, queue and de-duplication; the provider only translates calls and throws
`GameCenterException` with the closest code.

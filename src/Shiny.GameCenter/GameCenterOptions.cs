namespace Shiny.GameCenter;


/// <summary>
/// Maps your logical achievement and leaderboard keys to the ids configured in App Store Connect and the Play Console.
/// </summary>
/// <remarks>
/// Keys that are not registered are passed to the service untouched, so the platform id itself works as a key for
/// unlocks, reveals and scores. Incremental achievements must be registered - Apple counts progress as a percentage,
/// so the total step count has to be known on the device to convert.
/// </remarks>
public class GameCenterOptions
{
    readonly Dictionary<string, AchievementDefinition> achievements = new();
    readonly Dictionary<string, LeaderboardDefinition> leaderboards = new();


    /// <summary>
    /// Try to sign the player in silently at app launch so queued progress is sent as early as possible.
    /// Apple's sign-in sheet is never shown by this - it is held until <see cref="IGameCenterManager.SignInAsync"/>
    /// unless <see cref="PresentSignInOnStartup"/> is set. Defaults to true.
    /// </summary>
    public bool SignInOnStartup { get; set; } = true;

    /// <summary>
    /// Apple: present the Game Center sign-in sheet at launch when the player is not signed in, which is what Apple
    /// recommends for games. Google Play Games v2 signs in automatically and ignores this. Defaults to false.
    /// </summary>
    public bool PresentSignInOnStartup { get; set; }

    /// <summary>
    /// Apple: show the system banner when an achievement completes. Google always shows its own popup. Defaults to true.
    /// </summary>
    public bool ShowCompletionBanner { get; set; } = true;

    /// <summary>
    /// Score submissions kept while they cannot be sent. When the queue is full the oldest is dropped. Defaults to 100.
    /// </summary>
    public int MaxQueuedScores { get; set; } = 100;


    public IReadOnlyDictionary<string, AchievementDefinition> Achievements => this.achievements;
    public IReadOnlyDictionary<string, LeaderboardDefinition> Leaderboards => this.leaderboards;


    /// <summary>
    /// Registers an achievement under a logical key.
    /// </summary>
    /// <param name="key">The key your code uses</param>
    /// <param name="appleId">The achievement ID from App Store Connect (null if it is not on Game Center)</param>
    /// <param name="googleId">The achievement ID from the Play Console - <c>CgkI...</c> (null if it is not on Play Games)</param>
    /// <param name="totalSteps">
    /// 1 for an unlock-once achievement. For an incremental one this must match the step count set in the Play Console.
    /// </param>
    public GameCenterOptions AddAchievement(string key, string? appleId = null, string? googleId = null, int totalSteps = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalSteps, 1);
        this.achievements[key] = new AchievementDefinition(key, appleId, googleId, totalSteps);
        return this;
    }


    /// <summary>
    /// Registers a leaderboard under a logical key.
    /// </summary>
    public GameCenterOptions AddLeaderboard(string key, string? appleId = null, string? googleId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        this.leaderboards[key] = new LeaderboardDefinition(key, appleId, googleId);
        return this;
    }
}

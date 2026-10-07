using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shiny.GameCenter.Infrastructure;


/// <summary>
/// Everything the manager keeps across restarts, per player. Progress recorded while nobody is signed in goes under
/// <see cref="AnonymousKey"/> and is handed to whoever signs in next.
/// </summary>
class GameCenterState
{
    public const string AnonymousKey = "";

    public Dictionary<string, PlayerState> Players { get; set; } = new();


    public PlayerState For(string? playerId)
    {
        var key = playerId ?? AnonymousKey;
        if (!this.Players.TryGetValue(key, out var state))
        {
            state = new PlayerState();
            this.Players[key] = state;
        }
        return state;
    }


    /// <summary>Moves anonymous progress onto a player. Returns true if anything moved.</summary>
    public bool AdoptAnonymous(string playerId)
    {
        if (!this.Players.Remove(AnonymousKey, out var anon))
            return false;

        var target = this.For(playerId);
        foreach (var (key, steps) in anon.KnownSteps)
            target.RecordSteps(key, steps);

        foreach (var (key, steps) in anon.PendingProgress)
            target.PendingProgress[key] = Math.Max(steps, target.PendingProgress.GetValueOrDefault(key));

        foreach (var key in anon.PendingReveals)
            target.PendingReveals.Add(key);

        target.PendingScores.AddRange(anon.PendingScores);
        return true;
    }
}


class PlayerState
{
    /// <summary>The furthest progress known for each achievement key - from this device or the service</summary>
    public Dictionary<string, int> KnownSteps { get; set; } = new();

    /// <summary>Absolute progress not yet accepted by the service, by achievement key</summary>
    public Dictionary<string, int> PendingProgress { get; set; } = new();

    public HashSet<string> PendingReveals { get; set; } = new();

    public List<PendingScore> PendingScores { get; set; } = new();


    public int PendingCount => this.PendingProgress.Count + this.PendingReveals.Count + this.PendingScores.Count;

    public int GetSteps(string key) => this.KnownSteps.GetValueOrDefault(key);


    /// <summary>Raises the known steps for a key; never lowers them. Returns true when they went up.</summary>
    public bool RecordSteps(string key, int steps)
    {
        if (steps <= this.GetSteps(key))
            return false;

        this.KnownSteps[key] = steps;
        return true;
    }
}


record PendingScore(string LeaderboardKey, long Score, DateTimeOffset Timestamp);


[JsonSerializable(typeof(GameCenterState))]
[JsonSourceGenerationOptions(WriteIndented = false)]
partial class GameCenterStateContext : JsonSerializerContext;


/// <summary>
/// Reads and writes <see cref="GameCenterState"/> as one JSON file. Writes go to a temp file that is then moved over
/// the real one, so a crash mid-write leaves the previous state rather than a truncated file.
/// </summary>
class GameCenterStateStore(string filePath)
{
    public string FilePath => filePath;


    public GameCenterState Load()
    {
        try
        {
            if (!File.Exists(filePath))
                return new GameCenterState();

            using var stream = File.OpenRead(filePath);
            return JsonSerializer.Deserialize(stream, GameCenterStateContext.Default.GameCenterState) ?? new GameCenterState();
        }
        catch (JsonException)
        {
            // a corrupt file must not stop the game - start clean
            return new GameCenterState();
        }
    }


    public void Save(GameCenterState state)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!String.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var temp = filePath + ".tmp";
        using (var stream = File.Create(temp))
            JsonSerializer.Serialize(stream, state, GameCenterStateContext.Default.GameCenterState);

        File.Move(temp, filePath, true);
    }
}

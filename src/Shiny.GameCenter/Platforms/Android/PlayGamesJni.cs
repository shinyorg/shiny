using Android.App;
using Android.Runtime;
using GmsTask = Android.Gms.Tasks.Task;

namespace Shiny.GameCenter;


/// <summary>
/// The parts of Play Games v2 that <c>Xamarin.GooglePlayServices.Games.V2</c> does not bind - <c>PlayersClient</c>,
/// the <c>Player</c> interface and <c>LeaderboardScore.getScoreHolder()</c> - called through JNI.
/// </summary>
static class PlayGamesJni
{
    const string PlayGamesClass = "com/google/android/gms/games/PlayGames";
    const string PlayersClientClass = "com/google/android/gms/games/PlayersClient";
    const string TaskSig = "Lcom/google/android/gms/tasks/Task;";


    public static Java.Lang.Object GetPlayersClient(Activity activity)
    {
        var cls = JNIEnv.FindClass(PlayGamesClass);
        try
        {
            var method = JNIEnv.GetStaticMethodID(cls, "getPlayersClient", $"(Landroid/app/Activity;)L{PlayersClientClass};");
            var handle = JNIEnv.CallStaticObjectMethod(cls, method, new JValue(activity));
            return Java.Lang.Object.GetObject<Java.Lang.Object>(handle, JniHandleOwnership.TransferLocalRef)
                ?? throw new GameCenterException(GameCenterErrorCode.Unavailable, "Play Games returned no PlayersClient");
        }
        finally
        {
            JNIEnv.DeleteGlobalRef(cls);
        }
    }


    /// <summary><c>PlayersClient.getCurrentPlayer()</c></summary>
    public static GmsTask GetCurrentPlayer(Java.Lang.Object playersClient)
        => CallTask(playersClient, "getCurrentPlayer", "()" + TaskSig);


    /// <summary><c>PlayersClient.loadFriends(int pageSize, boolean forceReload)</c></summary>
    public static GmsTask LoadFriends(Java.Lang.Object playersClient, int pageSize, bool forceReload)
        => CallTask(playersClient, "loadFriends", "(IZ)" + TaskSig, new JValue(pageSize), new JValue(forceReload));


    /// <summary><c>LeaderboardScore.getScoreHolder()</c> - null for scores whose holder is not visible</summary>
    public static Java.Lang.Object? GetScoreHolder(Java.Lang.Object score)
        => CallObject(score, "getScoreHolder", "()Lcom/google/android/gms/games/Player;");


    /// <summary><c>Player.getPlayerId()</c></summary>
    public static string? GetPlayerId(Java.Lang.Object player) => CallString(player, "getPlayerId");


    /// <summary><c>Player.getDisplayName()</c></summary>
    public static string? GetDisplayName(Java.Lang.Object player) => CallString(player, "getDisplayName");


    static GmsTask CallTask(Java.Lang.Object target, string name, string signature, params JValue[] args)
    {
        var handle = Call(target, name, signature, args);
        return Java.Lang.Object.GetObject<GmsTask>(handle, JniHandleOwnership.TransferLocalRef)
            ?? throw new GameCenterException(GameCenterErrorCode.Unknown, $"Play Games {name} returned no task");
    }


    static Java.Lang.Object? CallObject(Java.Lang.Object target, string name, string signature)
        => Java.Lang.Object.GetObject<Java.Lang.Object>(Call(target, name, signature), JniHandleOwnership.TransferLocalRef);


    static string? CallString(Java.Lang.Object target, string name)
        => JNIEnv.GetString(Call(target, name, "()Ljava/lang/String;"), JniHandleOwnership.TransferLocalRef);


    static IntPtr Call(Java.Lang.Object target, string name, string signature, params JValue[] args)
    {
        // resolved against the runtime class so it works for every implementation (PlayerRef, PlayerEntity, ...)
        var cls = JNIEnv.GetObjectClass(target.Handle);
        try
        {
            var method = JNIEnv.GetMethodID(cls, name, signature);
            return JNIEnv.CallObjectMethod(target.Handle, method, args);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(cls);
        }
    }
}

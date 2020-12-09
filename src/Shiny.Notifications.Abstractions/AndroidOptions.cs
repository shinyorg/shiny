using System;


namespace Shiny.Notifications
{
    public class AndroidOptions
    {
        public static string? DefaultSmallIconResourceName { get; set; }
        public static string? DefaultLargeIconResourceName { get; set; }
        public static string? DefaultColorResourceName { get; set; }
        public static bool DefaultUseBigTextStyle { get; set; }
        public static bool? DefaultShowWhen { get; set; }
        //public static bool DefaultVibrate { get; set; }
        public static Type? DefaultLaunchActivityType { get; set; }
        public static AndroidActivityFlags DefaultLaunchActivityFlags { get; set; } = AndroidActivityFlags.NewTask | AndroidActivityFlags.ClearTask;

        public Type? LaunchActivityType { get; set; }
        public AndroidActivityFlags LaunchActivityFlags { get; set; } = DefaultLaunchActivityFlags;
        //public bool Vibrate { get; set; } = DefaultVibrate;
        //public int? Priority { get; set; }
        public string? ContentInfo { get; set; }
        public string? Ticker { get; set; }
        public string? Category { get; set; }
        public string? SmallIconResourceName { get; set; } = DefaultSmallIconResourceName;
        public string? LargeIconResourceName { get; set; } = DefaultLargeIconResourceName;
        public string? ColorResourceName { get; set; } = DefaultColorResourceName;
        public bool OnGoing { get; set; }
        public bool UseBigTextStyle { get; set; } = DefaultUseBigTextStyle;
        public bool AutoCancel { get; set; } = true;
        public bool? ShowWhen { get; set; } = DefaultShowWhen;
        public DateTimeOffset? When { get; set; }
    }


    [Flags]
    public enum AndroidActivityFlags
    {
        BroughtToFront = 4194304,
        ClearTask = 32768,
        ClearTop = 67108864,
        ClearWhenTaskReset = 524288,
        ExcludeFromRecents = 8388608,
        ForwardResult = 33554432,
        LaunchAdjacent = 4096,
        LaunchedFromHistory = 1048576,
        MultipleTask = 134217728,
        NewTask = 268435456,
        NoAnimation = 65536,
        NoHistory = 1073741824,
        NoUserAction = 262144,
        PreviousIsTop = 16777216,
        ReorderToFront = 131072,
        ResetTaskIfNeeded = 2097152,
        RetainInRecents = 8192,
        SingleTop = 536870912,
        TaskOnHome = 16384,
        DebugLogResolution = 8,
        ExcludeStoppedPackages = 16,
        FromBackground = 4,
        GrantPersistableUriPermission = 64,
        GrantPrefixUriPermission = 128,
        GrantReadUriPermission = 1,
        GrantWriteUriPermission = 2,
        IncludeStoppedPackages = 32
    }
}

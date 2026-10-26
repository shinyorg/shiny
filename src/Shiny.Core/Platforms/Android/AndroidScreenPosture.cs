using System;
using System.Collections.Generic;
using Android.App;
using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Window.Java.Layout;
using AndroidX.Window.Layout;
using Microsoft.Extensions.Logging;

namespace Shiny;


/// <summary>
/// The Android <see cref="IScreenPosture"/>, from Jetpack WindowManager's <c>FoldingFeature</c> for the current
/// Activity. Bounds arrive in window pixels and are reported in dp.
/// </summary>
/// <remarks>
/// <para>
/// Layout info belongs to an Activity, not the process: the listener follows the resumed Activity and is removed
/// when that Activity stops or is destroyed.
/// </para>
/// <para>
/// Posture only streams live while the Activity handles <c>ScreenSize | SmallestScreenSize | ScreenLayout |
/// Orientation</c> config changes (MAUI's default <c>MainActivity</c> does). Without them a fold recreates the Activity,
/// and the new one is read when it resumes.
/// </para>
/// <para>
/// TODO: WindowManager 1.6 deprecates <c>WindowInfoTrackerCallbackAdapter</c> in favour of callbacks on
/// <c>WindowInfoTracker</c>. Revisit when <c>Xamarin.AndroidX.Window</c> 1.6 is bound (1.5.1.3 as of 2026-10). All of
/// the WindowManager use is in this file.
/// </para>
/// </remarks>
public class AndroidScreenPosture(AndroidPlatform platform, ILogger<AndroidScreenPosture> logger) : AbstractScreenPosture
{
    readonly object syncLock = new();
    readonly HashSet<FoldOrientation> seen = [];
    WindowInfoTrackerCallbackAdapter? tracker;
    Attachment? attached;
    bool? tabletop;


    public override bool IsSupported => true;


    public override IReadOnlyList<FoldOrientation> SupportedPostures
    {
        get
        {
            this.tabletop ??= this.ReadTabletopSupport();
            lock (this.syncLock)
            {
                var list = new List<FoldOrientation>();
                if (this.tabletop == true || this.seen.Contains(FoldOrientation.Horizontal))
                    list.Add(FoldOrientation.Horizontal);
                if (this.seen.Contains(FoldOrientation.Vertical))
                    list.Add(FoldOrientation.Vertical);

                return list;
            }
        }
    }


    protected override ScreenPosture ReadPosture()
    {
        lock (this.syncLock)
        {
            // the listener's last delivery is authoritative while it is attached to the current activity
            if (this.attached?.Latest is { } latest && this.attached.Activity == platform.CurrentActivity)
                return latest;
        }

        var activity = platform.CurrentActivity;
        if (activity == null)
            return ScreenPosture.None;

        try
        {
            var info = this.GetTracker().GetCurrentWindowLayoutInfo(activity);
            return this.ToPosture(info, activity);
        }
        catch (Exception ex)
        {
            // getCurrentWindowLayoutInfo needs a recent WindowManager extension on the device
            logger.LogDebug(ex, "Unable to read the current window layout");
            return ScreenPosture.None;
        }
    }


    protected override IDisposable Watch(Action refresh)
    {
        void OnActivityChanged(object? sender, ActivityChanged e)
        {
            switch (e.State)
            {
                case ActivityState.Resumed:
                    this.Attach(e.Activity, refresh);
                    break;

                case ActivityState.Stopped:
                case ActivityState.Destroyed:
                    this.Detach(e.Activity, refresh);
                    break;
            }
        }

        platform.ActivityChanged += OnActivityChanged;
        var current = platform.CurrentActivity;
        if (current != null)
            platform.InvokeOnMainThread(() => this.Attach(current, refresh));

        return new ActionDisposable(() =>
        {
            platform.ActivityChanged -= OnActivityChanged;
            platform.InvokeOnMainThread(() => this.Detach(null, null));
        });
    }


    void Attach(Activity activity, Action refresh)
    {
        lock (this.syncLock)
        {
            if (this.attached?.Activity == activity)
                return;
        }
        this.Detach(null, null);

        var attachment = new Attachment(activity);
        attachment.Consumer = new LayoutConsumer(value =>
        {
            if (value is not WindowLayoutInfo info)
                return;

            var posture = this.ToPosture(info, activity);
            lock (this.syncLock)
            {
                if (this.attached != attachment)
                    return;

                attachment.Latest = posture;
            }
            refresh();
        });

        try
        {
            this.GetTracker().AddWindowLayoutInfoListener(activity, ContextCompat.GetMainExecutor(activity)!, attachment.Consumer);
            lock (this.syncLock)
                this.attached = attachment;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to listen for window layout changes");
            attachment.Consumer.Dispose();
        }
    }


    // activity null detaches whatever is attached; otherwise only that activity's listener
    void Detach(Activity? activity, Action? refresh)
    {
        Attachment? old;
        lock (this.syncLock)
        {
            old = this.attached;
            if (old == null || (activity != null && old.Activity != activity))
                return;

            this.attached = null;
        }

        try
        {
            this.GetTracker().RemoveWindowLayoutInfoListener(old.Consumer!);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to remove the window layout listener");
        }
        old.Consumer!.Dispose();
        refresh?.Invoke();
    }


    ScreenPosture ToPosture(WindowLayoutInfo info, Activity activity)
    {
        var density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
        if (density <= 0)
            density = 1f;

        foreach (var feature in info.DisplayFeatures)
        {
            if (feature is not Java.Lang.Object obj || !FoldingFeatureClass.IsInstance(obj))
                continue;

            var fold = Java.Interop.JavaObjectExtensions.JavaCast<IFoldingFeature>(obj)!;
            var halfOpen = fold.State.Equals(FoldingFeatureState.HalfOpened);
            var orientation = fold.Orientation.Equals(FoldingFeatureOrientation.Vertical)
                ? FoldOrientation.Vertical
                : FoldOrientation.Horizontal;

            lock (this.syncLock)
                this.seen.Add(orientation);

            var bounds = fold.Bounds;
            ScreenRect? rect = halfOpen || fold.IsSeparating
                ? new ScreenRect(bounds.Left / density, bounds.Top / density, bounds.Width() / density, bounds.Height() / density)
                : null;

            return new ScreenPosture(
                halfOpen ? FoldState.HalfOpen : FoldState.Flat,
                orientation,
                rect,
                fold.IsSeparating,
                halfOpen ? HingeStatus.PartiallyOpen : HingeStatus.FullyOpen,
                null,
                []
            );
        }
        return ScreenPosture.None;
    }


    bool ReadTabletopSupport()
    {
        try
        {
            foreach (var posture in this.GetTracker().SupportedPostures)
            {
                if (posture.Equals(SupportedPosture.Tabletop))
                    return true;
            }
        }
        catch (Exception ex)
        {
            // supported postures need WindowManager extension level 6 on the device
            logger.LogDebug(ex, "Unable to read the supported postures");
        }
        return false;
    }


    WindowInfoTrackerCallbackAdapter GetTracker()
        => this.tracker ??= new WindowInfoTrackerCallbackAdapter(WindowInfoTracker.GetOrCreate(platform.AppContext));


    static Java.Lang.Class FoldingFeatureClass => field ??= Java.Lang.Class.FromType(typeof(IFoldingFeature));


    sealed class Attachment(Activity activity)
    {
        public Activity Activity { get; } = activity;
        public LayoutConsumer? Consumer { get; set; }
        public ScreenPosture? Latest { get; set; }
    }


    sealed class LayoutConsumer(Action<Java.Lang.Object?> accept) : Java.Lang.Object, IConsumer
    {
        public void Accept(Java.Lang.Object? value) => accept(value);
    }
}

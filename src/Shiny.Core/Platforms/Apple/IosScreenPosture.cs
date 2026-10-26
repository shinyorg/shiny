#if IOS
using System;
using System.Collections.Generic;
using System.Linq;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Shiny;


/// <summary>
/// The iOS <see cref="IScreenPosture"/> for iPhone Duo (iOS 27.1+): the hinge from a <c>UIHingeInteraction</c>
/// on the key window, and the fold and camera housings from the window's reserved regions. Before iOS 27.1 it is
/// unsupported and reports <see cref="ScreenPosture.None"/>.
/// </summary>
/// <remarks>
/// The hinge is <see cref="HingeStatus.Unknown"/> until UIKit delivers the first hinge update after the interaction is
/// attached - which happens on the first read or subscription. iPhone Duo has no spanned mode: closed means the app is
/// on the outer screen, so it reads as <see cref="FoldState.None"/>.
/// </remarks>
public class IosScreenPosture : AbstractScreenPosture
{
    readonly object syncLock = new();
    Action? refresh;
    HingeState? state;


    // UIKit27.IsAvailable also checks the classes exist - an OS version alone is not trusted for APIs this new
    public override bool IsSupported => OperatingSystem.IsIOSVersionAtLeast(27, 1) && UIKit27.IsAvailable;


    public override IReadOnlyList<FoldOrientation> SupportedPostures
    {
        get
        {
            if (!this.IsSupported)
                return [];

            // the device has a hinge once UIKit has reported one in any state but unknown
            var status = OnMainThread(() => this.EnsureAttached().Status);
            return status == HingeStatus.Unknown ? [] : [FoldOrientation.Horizontal, FoldOrientation.Vertical];
        }
    }


    protected override ScreenPosture ReadPosture()
    {
        if (!this.IsSupported)
            return ScreenPosture.None;

        return OnMainThread(this.Read);
    }


    protected override IDisposable Watch(Action refresh)
    {
        lock (this.syncLock)
            this.refresh = refresh;

        if (this.IsSupported)
            DispatchQueue.MainQueue.DispatchAsync(() => this.EnsureAttached());

        var center = NSNotificationCenter.DefaultCenter;
        var observers = new List<NSObject>
        {
            // a different window taking key moves the interaction to it
            center.AddObserver(UIWindow.DidBecomeKeyNotification, _ => this.OnNativeChange()),
            center.AddObserver(UIScreen.ModeDidChangeNotification, _ => this.OnNativeChange()),
            center.AddObserver(UIApplication.DidBecomeActiveNotification, _ => this.OnNativeChange())
        };

        return new ActionDisposable(() =>
        {
            lock (this.syncLock)
                this.refresh = null;

            foreach (var observer in observers)
                center.RemoveObserver(observer);
        });
    }


    void OnNativeChange()
    {
        Action? current;
        lock (this.syncLock)
            current = this.refresh;

        current?.Invoke();
    }


    ScreenPosture Read()
    {
        var hinge = this.EnsureAttached();
        var window = hinge.Window;
        if (window == null)
            return ScreenPosture.None;

        // the window is the view queried, so the frames are already in window coordinates
        ScreenRect? fold = null;
        var orientation = FoldOrientation.None;
        var division = UIKit27.GetActiveReservedRegions(window, UIKit27.DivisionKind);
        if (division.Length > 0)
        {
            var frame = division[0];
            fold = ToRect(frame);
            orientation = frame.Height > frame.Width ? FoldOrientation.Vertical : FoldOrientation.Horizontal;
        }

        var occlusions = UIKit27
            .GetActiveReservedRegions(window, UIKit27.OcclusionKind)
            .Select(ToRect)
            .ToArray();

        // only a part-open hinge with an active division splits the window - fully open is one flat surface with no
        // division to avoid, and closed puts the app on the outer screen
        var state = hinge.Status switch
        {
            HingeStatus.PartiallyOpen when fold != null => FoldState.HalfOpen,
            HingeStatus.FullyOpen => FoldState.Flat,
            _ => FoldState.None
        };
        if (state != FoldState.HalfOpen)
        {
            fold = null;
            orientation = FoldOrientation.None;
        }

        return new ScreenPosture(
            state,
            orientation,
            fold,
            state == FoldState.HalfOpen,
            hinge.Status,
            hinge.AngleDegrees,
            occlusions
        );
    }


    // main thread only
    HingeState EnsureAttached()
    {
        var hinge = this.state ??= new HingeState(this.OnNativeChange);
        hinge.AttachTo(FindKeyWindow());
        return hinge;
    }


    static UIWindow? FindKeyWindow()
    {
        var windows = UIApplication.SharedApplication.ConnectedScenes
            .ToArray()
            .OfType<UIWindowScene>()
            .Where(x => x.Session.Role == UIWindowSceneSessionRole.Application)
            .OrderByDescending(x => x.ActivationState == UISceneActivationState.ForegroundActive)
            .SelectMany(x => x.Windows)
            .ToArray();

        return windows.FirstOrDefault(x => x.IsKeyWindow) ?? windows.FirstOrDefault();
    }


    static ScreenRect ToRect(CGRect frame) => new(frame.X, frame.Y, frame.Width, frame.Height);


    static T OnMainThread<T>(Func<T> func)
    {
        if (NSThread.IsMain)
            return func();

        T result = default!;
        DispatchQueue.MainQueue.DispatchSync(() => result = func());
        return result;
    }


    sealed class HingeState(Action changed)
    {
        NSObject? interaction;
        LayoutProbe? probe;

        public UIWindow? Window { get; private set; }
        public HingeStatus Status { get; private set; } = HingeStatus.Unknown;
        public double? AngleDegrees { get; private set; }


        public void AttachTo(UIWindow? window)
        {
            if (window == this.Window)
                return;

            if (this.Window != null)
            {
                if (this.interaction != null)
                    UIKit27.RemoveInteraction(this.Window, this.interaction);
                this.probe?.RemoveFromSuperview();
            }
            this.Window = window;
            if (window == null)
                return;

            this.interaction ??= UIKit27.CreateHingeInteraction(this.OnHinge);
            if (this.interaction != null)
                UIKit27.AddInteraction(window, this.interaction);

            // reserved regions move with the layout, not only with the hinge - a hidden view that fills the window is
            // told whenever the window's bounds change
            this.probe ??= new LayoutProbe(changed)
            {
                Hidden = true,
                UserInteractionEnabled = false,
                AutoresizingMask = UIViewAutoresizing.FlexibleDimensions
            };
            this.probe.Frame = window.Bounds;
            window.InsertSubview(this.probe, 0);
        }


        void OnHinge(HingeStatus status, double? angleDegrees)
        {
            this.Status = status;
            this.AngleDegrees = angleDegrees;
            changed();
        }
    }


    sealed class LayoutProbe(Action changed) : UIView
    {
        CGSize size;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            if (this.Bounds.Size == this.size)
                return;

            this.size = this.Bounds.Size;
            changed();
        }
    }
}
#endif

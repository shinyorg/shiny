#if IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Shiny;


/// <summary>
/// The iOS 27.1 hinge and reserved-region APIs, called through the Objective-C runtime.
/// </summary>
/// <remarks>
/// A library's <c>net10.0-ios</c> compiles against the oldest supported iOS SDK pack (26.0), which has no binding for
/// <c>UIHingeInteraction</c> or <c>UIViewReservedRegion</c>. Targeting <c>net10.0-ios27.1</c> instead would stop every
/// <c>net10.0-ios</c> library that references Shiny.Core from restoring. So these few calls go through
/// <c>objc_msgSend</c> and key-value coding, with blittable signatures only (trim and AOT safe). Replace this file with
/// the bound types once the library's default iOS pack is 27.1 or later.
/// </remarks>
static unsafe class UIKit27
{
    // void (^)(UIHingeInteraction *, UIHingeInteractionUpdate *)
    const string UpdateHandlerSignature = "v@?@@";

    static readonly NativeHandle HingeInteractionClass = Class.GetHandle("UIHingeInteraction");
    static readonly NativeHandle RegionKindClass = Class.GetHandle("UIViewReservedRegionKind");
    static readonly IntPtr ReservedRegionsSelector = Selector.GetHandle("reservedRegionsOfKind:");


    public static bool IsAvailable =>
        HingeInteractionClass != NativeHandle.Zero &&
        RegionKindClass != NativeHandle.Zero &&
        RespondsToSelector(Class.GetHandle("UIView"), ReservedRegionsSelector) != 0;


    // the class methods return autoreleased objects - held as NSObject so they stay retained once the pool drains
    public static NSObject? DivisionKind => field ??= GetKind("divisionRegionKind");
    public static NSObject? OcclusionKind => field ??= GetKind("occlusionRegionKind");


    static NSObject? GetKind(string selector)
        => RegionKindClass == NativeHandle.Zero ? null : Runtime.GetNSObject(SendIntPtr(RegionKindClass, Selector.GetHandle(selector)));


    /// <summary>
    /// The frames of the view's active reserved regions of <paramref name="kind"/>, in the view's coordinates.
    /// </summary>
    public static CGRect[] GetActiveReservedRegions(UIView view, NSObject? kind)
    {
        if (kind == null)
            return [];

        var handle = SendIntPtr(view.Handle, ReservedRegionsSelector, kind.Handle);
        GC.KeepAlive(view);
        GC.KeepAlive(kind);
        if (handle == IntPtr.Zero)
            return [];

        var regions = NSArray.ArrayFromHandle<NSObject>(handle);
        var frames = new List<CGRect>(regions.Length);
        foreach (var region in regions)
        {
            if (region.ValueForKey(new NSString("active")) is NSNumber { BoolValue: true } &&
                region.ValueForKey(new NSString("frame")) is NSValue frame)
            {
                frames.Add(frame.CGRectValue);
            }
        }
        return frames.ToArray();
    }


    /// <summary>
    /// A <c>UIHingeInteraction</c> that reports every hinge update to <paramref name="update"/> (status, then the angle
    /// in whole degrees or null), or null where the class does not exist.
    /// </summary>
    public static NSObject? CreateHingeInteraction(Action<HingeStatus, double?> update)
    {
        if (HingeInteractionClass == NativeHandle.Zero)
            return null;

        Action<IntPtr> handler = updateHandle => update(ReadStatus(updateHandle, out var angle), angle);
        var alloc = SendIntPtr(HingeInteractionClass, Selector.GetHandle("alloc"));

        // the initializer copies the block, and the copy keeps the handler alive
        using var block = new BlockLiteral((delegate* unmanaged<IntPtr, IntPtr, IntPtr, void>)&OnHingeUpdate, handler, UpdateHandlerSignature);
        var handle = SendIntPtr(alloc, Selector.GetHandle("initWithUpdateHandler:"), (IntPtr)(&block));

        return handle == IntPtr.Zero ? null : Runtime.GetNSObject(handle, true);
    }


    public static void AddInteraction(UIView view, NSObject interaction)
    {
        SendVoid(view.Handle, Selector.GetHandle("addInteraction:"), interaction.Handle);
        GC.KeepAlive(view);
        GC.KeepAlive(interaction);
    }


    public static void RemoveInteraction(UIView view, NSObject interaction)
    {
        SendVoid(view.Handle, Selector.GetHandle("removeInteraction:"), interaction.Handle);
        GC.KeepAlive(view);
        GC.KeepAlive(interaction);
    }


    static HingeStatus ReadStatus(IntPtr updateHandle, out double? angleDegrees)
    {
        angleDegrees = null;
        var hinge = Runtime.GetNSObject(updateHandle)?.ValueForKey(new NSString("hinge"));
        if (hinge == null)
            return HingeStatus.Unknown;

        var status = (hinge.ValueForKey(new NSString("status")) as NSNumber)?.Int64Value switch
        {
            1 => HingeStatus.Closed,
            2 => HingeStatus.PartiallyOpen,
            3 => HingeStatus.FullyOpen,
            _ => HingeStatus.Unknown
        };

        // whole degrees: the sensor is noisy, and the angle is for effects, not layout
        if (status != HingeStatus.Unknown && hinge.ValueForKey(new NSString("angle")) is NSNumber radians)
            angleDegrees = Math.Round(radians.DoubleValue * 180.0 / Math.PI);

        return status;
    }


    [UnmanagedCallersOnly]
    static void OnHingeUpdate(IntPtr block, IntPtr interaction, IntPtr update)
    {
        try
        {
            BlockLiteral.GetTarget<Action<IntPtr>>(block)?.Invoke(update);
        }
        catch
        {
            // an exception must not unwind into UIKit
        }
    }


    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "class_respondsToSelector")]
    static extern byte RespondsToSelector(IntPtr cls, IntPtr selector);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern IntPtr SendIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr arg);
}
#endif

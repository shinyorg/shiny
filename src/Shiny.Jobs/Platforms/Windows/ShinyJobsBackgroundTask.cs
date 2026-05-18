using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
using Windows.ApplicationModel.Background;

namespace Shiny.Jobs;


/// <summary>
/// COM-activated background task that the OS invokes for every Shiny job category trigger.
/// The class GUID is stable and shared with the <c>windows.comServer</c> manifest entry the
/// app declares; <see cref="RegisterComServer"/> wires the class factory at app startup so
/// the in-proc activation lands here.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid(ClsidString)]
[ComSourceInterfaces(typeof(IBackgroundTask))]
public sealed class ShinyJobsBackgroundTask : IBackgroundTask
{
    public const string ClsidString = "F3A7B2C4-9E1D-4B8A-A6C5-7F2D9E8C1B4A";
    public static readonly Guid ClsidGuid = new(ClsidString);


    [MTAThread]
    public void Run(IBackgroundTaskInstance taskInstance)
    {
        var deferral = taskInstance.GetDeferral();
        var cancelSrc = new CancellationTokenSource();
        taskInstance.Canceled += (_, _) => cancelSrc.Cancel();

        _ = Task.Run(async () =>
        {
            try
            {
                if (!Host.IsInitialized)
                    return;

                var manager = Host.GetService<IJobManager>() as JobManager;
                if (manager == null)
                    return;

                var jobs = manager.GetJobsByCategory(taskInstance.Task.Name);
                foreach (var reg in jobs)
                    await manager.RunJob(reg, cancelSrc.Token).ConfigureAwait(false);
            }
            finally
            {
                cancelSrc.Dispose();
                deferral.Complete();
            }
        });
    }


    static uint registrationToken;

    /// <summary>
    /// Registers the COM class factory so the OS can activate this background task in-proc.
    /// Call once during application startup (typically from the <c>App</c> constructor in
    /// <c>Platforms/Windows/App.xaml.cs</c>, before any background trigger registration).
    /// </summary>
    public static void RegisterComServer()
    {
        if (registrationToken != 0)
            return;

        var classId = ClsidGuid;
        var hr = CoRegisterClassObject(
            ref classId,
            new BackgroundTaskFactory(),
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out registrationToken
        );
        if (hr != 0)
            throw Marshal.GetExceptionForHR(hr) ?? new InvalidOperationException($"CoRegisterClassObject failed: 0x{hr:X8}");
    }


    /// <summary>
    /// Revokes the COM class factory registered by <see cref="RegisterComServer"/>.
    /// Typically not required — the OS reclaims the registration on process exit.
    /// </summary>
    public static void UnregisterComServer()
    {
        if (registrationToken == 0)
            return;

        CoRevokeClassObject(registrationToken);
        registrationToken = 0;
    }


    const uint CLSCTX_LOCAL_SERVER = 4;
    const uint REGCLS_MULTIPLEUSE = 1;

    [LibraryImport("ole32.dll")]
    static partial int CoRegisterClassObject(
        ref Guid classId,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory factory,
        uint executionContext,
        uint flags,
        out uint registrationToken
    );

    [LibraryImport("ole32.dll")]
    static partial int CoRevokeClassObject(uint registrationToken);


    const string IID_IUnknown = "00000000-0000-0000-C000-000000000046";
    const string IID_IClassFactory = "00000001-0000-0000-C000-000000000046";

    [GeneratedComInterface]
    [Guid(IID_IClassFactory)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    partial interface IClassFactory
    {
        [PreserveSig] uint CreateInstance(IntPtr outer, in Guid iid, out IntPtr objectPtr);
        [PreserveSig] uint LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }


    [GeneratedComClass]
    sealed partial class BackgroundTaskFactory : IClassFactory
    {
        const uint S_OK = 0x00000000;
        const uint CLASS_E_NOAGGREGATION = 0x80040110;
        const uint E_NOINTERFACE = 0x80004002;

        public uint CreateInstance(IntPtr outer, in Guid iid, out IntPtr objectPtr)
        {
            if (outer != IntPtr.Zero)
            {
                objectPtr = IntPtr.Zero;
                return CLASS_E_NOAGGREGATION;
            }
            if (iid != typeof(ShinyJobsBackgroundTask).GUID && iid != new Guid(IID_IUnknown))
            {
                objectPtr = IntPtr.Zero;
                return E_NOINTERFACE;
            }
            objectPtr = MarshalInterface<IBackgroundTask>.ConvertToUnmanaged(new ShinyJobsBackgroundTask());
            return S_OK;
        }

        public uint LockServer(bool fLock) => 0;
    }
}

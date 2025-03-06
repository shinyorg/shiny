using Android;
using Android.App;
using Android.Content;
using Android.OS;

namespace Shiny;


// public class AndroidLifecycleExecutor : Java.Lang.Object, IShinyStartupTask, ILifecycleObserver, IDisposable
//
// public AndroidLifecycleExecutor(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }
public static class AndroidShinyHost
{
    // void InvokeOnMainThread(Action action);
    
    // lifecycle hooks
    // public void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
    //     => this.permissionSubject.OnNext(new PermissionRequestResult(requestCode, permissions, grantResults));
    //
    // public void Handle(Activity activity, int requestCode, Result resultCode, Intent data)
    //     => this.activityResultSubject.OnNext((requestCode, resultCode, data));
    
    
    // public void Start()
    // {
    //     // this is really only need for unit tests - it will passthrough under normal circumstances
    //     this.platform.InvokeOnMainThread(() =>
    //     {
    //         try
    //         {
    //             ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
    //         }
    //         catch (Exception ex)
    //         {
    //             this.logger.LogWarning(ex, "Could not attach lifecycle observer");
    //         }
    //     });
    // }
    //
    //
    // [Lifecycle.Event.OnResume]
    // [Export]
    // public void OnResume() => this.Execute(this.appHandlers, x => x.OnForeground());
    //
    //
    // [Lifecycle.Event.OnPause]
    // [Export]
    // public void OnPause() => this.Execute(this.appHandlers, x => x.OnBackground());
    
    
    public static void OnActivityOnCreate(Activity activity, Bundle? savedInstanceState) {}
        // => this.Execute(this.onCreateHandlers, x => x.ActivityOnCreate(activity, savedInstanceState));

    public static void OnRequestPermissionsResult(Activity activity, int requestCode, string[] permissions, Manifest.Permission[] grantResults) {}
        // => this.Execute(this.permissionHandlers, x => x.Handle(activity, requestCode, permissions, grantResults));

    public static void OnNewIntent(Activity activity, Intent? intent) {}
        // => this.Execute(this.newIntentHandlers, x => x.Handle(activity, intent));

    public static void OnActivityResult(Activity activity, int requestCode, Result result, Intent? intent) {}
        // => this.Execute(this.activityResultHandlers, x => x.Handle(activity, requestCode, result, intent));
        
        
        // void Execute<T>(IEnumerable<T> services, Action<T> action)
        // {
        //     foreach (var handler in services)
        //     {
        //         try
        //         {
        //             action(handler);
        //         }
        //         catch (Exception ex)
        //         {
        //             this.logger.LogError(ex, "Failed to execute lifecycle call");
        //         }
        //     }
        // }
}

/*
public AndroidPlatform()
   {
       var app = (Application)Application.Context;
       activityLifecycle ??= new(app);
       this.AppContext = app;
       this.AppData = new DirectoryInfo(this.AppContext.FilesDir.AbsolutePath);
       this.Cache = new DirectoryInfo(this.AppContext.CacheDir.AbsolutePath);
       var publicDir = this.AppContext.GetExternalFilesDir(null);
       if (publicDir != null)
           this.Public = new DirectoryInfo(publicDir.AbsolutePath);

       this.store = new(this, new DefaultSerializer());
       this.requestedPermissions = this.store.Get<List<string>>(PermissionsKey) ?? new List<string>();
   }


   public AccessState GetCurrentPermissionStatus(string androidPermission)
   {
       var self = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
       if (self == Permission.Granted)
           return AccessState.Available;

       if (!this.HasRequestedPermission(androidPermission))
           return AccessState.Unknown;

       //var showRequest = ActivityCompat.ShouldShowRequestPermissionRationale(this.CurrentActivity!, androidPermission);
       //if (showRequest)
       //    return AccessState.Unknown;

       return AccessState.Denied;
   }

   


   public Application AppContext { get; }
   public DirectoryInfo AppData { get; }
   public DirectoryInfo Cache { get; }
   public DirectoryInfo Public { get; }


   public Activity? CurrentActivity => activityLifecycle.Activity;
   public IObservable<ActivityChanged> WhenActivityChanged() => activityLifecycle.ActivitySubject;


   readonly Handler handler = new Handler(Looper.MainLooper);
   public void InvokeOnMainThread(Action action)
   {
       if (Looper.MainLooper.IsCurrentThread)
           action();
       else
           this.handler.Post(action);
   }


   public IObservable<ActivityChanged> WhenActivityStatusChanged() => Observable.Create<ActivityChanged>(ob =>
   {
       if (this.CurrentActivity != null)
           ob.Respond(new ActivityChanged(this.CurrentActivity, ActivityState.Created, null));

       return activityLifecycle
           .ActivitySubject
           .Subscribe(x => ob.Respond(x));
   });


   public async Task<AccessState> RequestForegroundServicePermissions()
   {
       if (OperatingSystem.IsAndroidVersionAtLeast(33))
       {
           var results = await this.RequestPermissions(
               Manifest.Permission.ForegroundService,
               Manifest.Permission.PostNotifications
           );
           if (results.IsSuccess())
               return AccessState.Available;

           if (!results.IsGranted(Manifest.Permission.ForegroundService))
               return AccessState.NotSetup;

           return AccessState.Restricted; // no post_notifications
       }
       else if (OperatingSystem.IsAndroidVersionAtLeast(31))
       {
           var results = await this.RequestPermissions(Manifest.Permission.ForegroundService);
           if (results.IsSuccess())
               return AccessState.Available;

           return AccessState.NotSetup;
       }

       return AccessState.Available;
   }

   public const string ActionServiceStart = "ACTION_START_FOREGROUND_SERVICE";
   public const string ActionServiceStop = "ACTION_STOP_FOREGROUND_SERVICE";
   public const string IntentActionStopWithTask = "StopWithTask";

   public void StartService(Type serviceType, bool stopWithTask = true)
   {
       var intent = new Intent(this.AppContext, serviceType);
       intent.SetAction(ActionServiceStart);
       intent.PutExtra(IntentActionStopWithTask, stopWithTask);

       if (OperatingSystem.IsAndroidVersionAtLeast(31))
           this.AppContext.StartForegroundService(intent);
       else
           this.AppContext.StartService(intent);
   }


   public void StopService(Type serviceType)
   {
       var intent = new Intent(this.AppContext, serviceType);
       intent.SetAction(ActionServiceStop);
       this.AppContext.StartService(intent);
       //this.AppContext.StopService(intent);
   }


   //public AccessState GetCurrentAccessState(string androidPermission)
   //{
   //    var result = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
   //    return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
   //}

   public int GetDrawableByName(string name) => this
       .AppContext
       .Resources!
       .GetIdentifier(
           name,
           "drawable",
           this.AppContext.PackageName
       );

   public IObservable<AccessState> RequestAccess(string androidPermissions)
       => this.RequestPermissions(new[] { androidPermissions }).Select(x => x.IsSuccess() ? AccessState.Available : AccessState.Denied);


   public IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions) => Observable.Create<PermissionRequestResult>(ob =>
   {
       var comp = new CompositeDisposable();

       //https://developer.android.com/training/permissions/requesting
       var allGood = androidPermissions.All(p => ContextCompat.CheckSelfPermission(this.AppContext, p) == Permission.Granted);
       if (allGood)
       {
           // everything is already good
           var grants = Enumerable.Repeat(Permission.Granted, androidPermissions.Length).ToArray();
           ob.Respond(new PermissionRequestResult(0, androidPermissions, grants));
       }
       else
       {
           //if (this.Status == PlatformState.Background)
           //    throw new ApplicationException("You cannot make permission requests while your application is in the background.  Please call RequestAccess in the Shiny library you are using while your app is in the foreground so your user can respond.  You are getting this message because your user has either not granted these permissions or has removed them.");
           this.SetRequestedPermissions(androidPermissions);
           var current = Interlocked.Increment(ref this.requestCode);
           comp.Add(this
               .permissionSubject
               .Where(x => x.RequestCode == current)
               .Subscribe(x => ob.Respond(x))
           );

           comp.Add(this
               .WhenActivityStatusChanged()
               .Take(1)
               .Timeout(TimeSpan.FromSeconds(5))
               .Subscribe(
                   x => ActivityCompat.RequestPermissions(
                       x.Activity,
                       androidPermissions,
                       current
                   ),
                   ex => ob.OnError(new TimeoutException(
                       "A current activity was not detected to be able to request permissions",
                       ex
                   ))
               )
           );
       }

       return comp;
   });

   void SetRequestedPermissions(string[] androidPermissions)
   {
       lock (this.requestedPermissions)
       {
           var count = this.requestedPermissions.Count;
           foreach (var p in androidPermissions)
           {
               if (!this.requestedPermissions.Contains(p, StringComparer.InvariantCultureIgnoreCase))
                   this.requestedPermissions.Add(p);
           }
           if (count != this.requestedPermissions.Count)
               this.store.Set(PermissionsKey, this.requestedPermissions);
       }
   }


   bool HasRequestedPermission(string androidPermission)
   {
       lock (this.requestedPermissions)
       {
           return this.requestedPermissions.Contains(
               androidPermission,
               StringComparer.InvariantCultureIgnoreCase
           );
       }
   }
   
   public int GetNotificationIconResource()
   {
       var id = this.GetResourceIdByName("notification");
       if (id > 0)
           return id;

       id = this.AppContext.ApplicationInfo?.Icon ?? 0;
       if (id > 0)
           return id;

       throw new InvalidOperationException("Unable to find notification icon - ensure you have your application icon set or a drawable resource named notification");
   }


   public TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object
   {
       using var type = this.GetSystemService<TSysType>(systemTypeName);
       return func(type);
   }


   public Intent CreateIntent<T>(params string[] actions)
   {
       var intent = new Intent(this.AppContext, typeof(T));
       foreach (var action in actions)
           intent.SetAction(action);

       return intent;
   }


   public PendingIntent GetBroadcastPendingIntent<T>(string intentAction, PendingIntentFlags flags, int requestCode = 0, Action<Intent>? modifyIntent = null)
   {
       var intent = this.CreateIntent<T>(intentAction);
       modifyIntent?.Invoke(intent);

       var pendingIntent = PendingIntent.GetBroadcast(
           this.AppContext,
           requestCode,
           intent,
           this.GetPendingIntentFlags(flags)
       );
       return pendingIntent!;
   }


   public PendingIntentFlags GetPendingIntentFlags(PendingIntentFlags flags)
   {
       if (OperatingSystem.IsAndroidVersionAtLeast(31) && !flags.HasFlag(PendingIntentFlags.Mutable))
           flags |= PendingIntentFlags.Mutable;

       return flags;
   }


   public T GetSystemService<T>(string key) where T : Java.Lang.Object
       => (T)this.AppContext.GetSystemService(key);


   public void RegisterBroadcastReceiver<T>(bool exported, params string[] actions) where T : BroadcastReceiver, new()
   {
       var receiver = new T();
       var filter = new IntentFilter();
       foreach (var e in actions)
           filter.AddAction(e);

       if (OperatingSystem.IsAndroidVersionAtLeast(34))
       {
           var flags = exported ? ReceiverFlags.Exported : ReceiverFlags.NotExported;
           this.AppContext.RegisterReceiver(receiver, filter, flags);
       }
       else
       {
           this.AppContext.RegisterReceiver(new T(), filter);
       }
   }


   public bool IsInManifest(string androidPermission)
   {
       var permissions = this
           .AppContext!
           .PackageManager!
           .GetPackageInfo(
               this.AppContext!.PackageName!,
               PackageInfoFlags.Permissions
           )
           ?.RequestedPermissions;

       if (permissions != null)
       {
           foreach (var permission in permissions)
           {
               if (permission.Equals(androidPermission, StringComparison.InvariantCultureIgnoreCase))
                   return true;
           }
       }
       return false;
   }


   public T GetIntentValue<T>(string intentAction, Func<Intent, T> transform)
   {
       using var filter = new IntentFilter(intentAction);
       using var receiver = this.AppContext.RegisterReceiver(null, filter);
       return transform(receiver!);
   }


   public int GetColorByName(string colorName) => this
       .AppContext
       .Resources
       .GetIdentifier(
           colorName,
           "color",
           this.AppContext.PackageName
       );

   public int GetResourceIdByName(string iconName) => this
       .AppContext
       .Resources
       .GetIdentifier(
           iconName,
           "drawable",
           this.AppContext.PackageName
       );


   // Expects raw resource name like "notify_sound" or "raw/notify_sound"
   public int GetRawResourceIdByName(string rawName) => this
       .AppContext
       .Resources
       .GetIdentifier(
           rawName,
           "raw",
           this.AppContext.PackageName
       );


   public IObservable<PermissionRequestResult> RequestFilteredPermissions(params AndroidPermission[] androidPermissions)
   {
       var list = new List<string>();
       foreach (var p in androidPermissions)
       {
           var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
           var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

           if (meetsMin && meetsMax)
               list.Add(p.Permission);
       }
       return this.RequestPermissions(list.ToArray());
   }


   public bool EnsureAllManifestEntries(params AndroidPermission[] androidPermissions)
   {
       foreach (var p in androidPermissions)
       {
           var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
           var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

           if (meetsMin && meetsMax)
           {
               if (!this.IsInManifest(p.Permission))
                   return false;
           }
       }
       return true;
   }
 */
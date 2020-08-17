using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;


namespace Shiny.Beacons
{
    [Service(
        Name = nameof(ShinyBeaconMonitoringService),
        Enabled = false,
        ForegroundServiceType = ForegroundService.TypeNone
    )]
    public class ShinyBeaconMonitoringService : Service
    {
        Lazy<BackgroundTask> task = ShinyHost.LazyResolve<BackgroundTask>();

        // TODO: persistent notification?
        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            //this.StartForeground()
            this.task.Value.StartScan();
            return StartCommandResult.NotSticky;
        }


        public override void OnDestroy()
        {
            this.task.Value.StopScan();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
/*
 @Override
    public void onCreate() {
        mFusedLocationClient = LocationServices.getFusedLocationProviderClient(this);

        mLocationCallback = new LocationCallback() {
            @Override
            public void onLocationResult(LocationResult locationResult) {
                super.onLocationResult(locationResult);
                onNewLocation(locationResult.getLastLocation());
            }
        };

        createLocationRequest();
        getLastLocation();

        HandlerThread handlerThread = new HandlerThread(TAG);
        handlerThread.start();
        mServiceHandler = new Handler(handlerThread.getLooper());
        mNotificationManager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);

        // Android O requires a Notification Channel.
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            CharSequence name = getString(R.string.app_name);
            // Create the channel for the notification
            NotificationChannel mChannel =
                    new NotificationChannel(CHANNEL_ID, name, NotificationManager.IMPORTANCE_DEFAULT);

            // Set the Notification Channel for the Notification Manager.
            mNotificationManager.createNotificationChannel(mChannel);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Log.i(TAG, "Service started");
        boolean startedFromNotification = intent.getBooleanExtra(EXTRA_STARTED_FROM_NOTIFICATION,
                false);

        // We got here because the user decided to remove location updates from the notification.
        if (startedFromNotification) {
            removeLocationUpdates();
            stopSelf();
        }
        // Tells the system to not try to recreate the service after it has been killed.
        return START_NOT_STICKY;
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        mChangingConfiguration = true;
    }

    @Override
    public IBinder onBind(Intent intent) {
        // Called when a client (MainActivity in case of this sample) comes to the foreground
        // and binds with this service. The service should cease to be a foreground service
        // when that happens.
        Log.i(TAG, "in onBind()");
        stopForeground(true);
        mChangingConfiguration = false;
        return mBinder;
    }

    @Override
    public void onRebind(Intent intent) {
        // Called when a client (MainActivity in case of this sample) returns to the foreground
        // and binds once again with this service. The service should cease to be a foreground
        // service when that happens.
        Log.i(TAG, "in onRebind()");
        stopForeground(true);
        mChangingConfiguration = false;
        super.onRebind(intent);
    }

    @Override
    public boolean onUnbind(Intent intent) {
        Log.i(TAG, "Last client unbound from service");

        // Called when the last client (MainActivity in case of this sample) unbinds from this
        // service. If this method is called due to a configuration change in MainActivity, we
        // do nothing. Otherwise, we make this service a foreground service.
        if (!mChangingConfiguration && Utils.requestingLocationUpdates(this)) {
            Log.i(TAG, "Starting foreground service");
            /*
            // TODO(developer). If targeting O, use the following code.
            if (Build.VERSION.SDK_INT == Build.VERSION_CODES.O) {
                mNotificationManager.startServiceInForeground(new Intent(this,
                        LocationUpdatesService.class), NOTIFICATION_ID, getNotification());
            } else {
                startForeground(NOTIFICATION_ID, getNotification());
            }
             
startForeground(NOTIFICATION_ID, getNotification());
        }
        return true; // Ensures onRebind() is called when a client re-binds.
    }

    @Override
    public void onDestroy()
{
    mServiceHandler.removeCallbacksAndMessages(null);
}


public void requestLocationUpdates()
{
    Log.i(TAG, "Requesting location updates");
    Utils.setRequestingLocationUpdates(this, true);
    startService(new Intent(getApplicationContext(), LocationUpdatesService.class));
try
{
    mFusedLocationClient.requestLocationUpdates(mLocationRequest,
            mLocationCallback, Looper.myLooper());
}
catch (SecurityException unlikely)
{
    Utils.setRequestingLocationUpdates(this, false);
    Log.e(TAG, "Lost location permission. Could not request updates. " + unlikely);
}
    }

    public void removeLocationUpdates()
{
    Log.i(TAG, "Removing location updates");
    try
    {
        mFusedLocationClient.removeLocationUpdates(mLocationCallback);
        Utils.setRequestingLocationUpdates(this, false);
        stopSelf();
    }
    catch (SecurityException unlikely)
    {
        Utils.setRequestingLocationUpdates(this, true);
        Log.e(TAG, "Lost location permission. Could not remove updates. " + unlikely);
    }
}

private Notification getNotification()
{
    Intent intent = new Intent(this, LocationUpdatesService.class);

CharSequence text = Utils.getLocationText(mLocation);

// Extra to help us figure out if we arrived in onStartCommand via the notification or not.
intent.putExtra(EXTRA_STARTED_FROM_NOTIFICATION, true);

// The PendingIntent that leads to a call to onStartCommand() in this service.
PendingIntent servicePendingIntent = PendingIntent.getService(this, 0, intent,
        PendingIntent.FLAG_UPDATE_CURRENT);

// The PendingIntent to launch activity.
PendingIntent activityPendingIntent = PendingIntent.getActivity(this, 0,
        new Intent(this, MainActivity.class), 0);

NotificationCompat.Builder builder = new NotificationCompat.Builder(this)
        .addAction(R.drawable.ic_launch, getString(R.string.launch_activity),
                activityPendingIntent)
        .addAction(R.drawable.ic_cancel, getString(R.string.remove_location_updates),
                servicePendingIntent)
        .setContentText(text)
        .setContentTitle(Utils.getLocationTitle(this))
        .setOngoing(true)
        .setPriority(Notification.PRIORITY_HIGH)
        .setSmallIcon(R.mipmap.ic_launcher)
        .setTicker(text)
        .setWhen(System.currentTimeMillis());

// Set the Channel ID for Android O.
if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
{
    builder.setChannelId(CHANNEL_ID); // Channel ID
}

return builder.build();
    }

    private void getLastLocation()
{
    try
    {
        mFusedLocationClient.getLastLocation()
                .addOnCompleteListener(new OnCompleteListener<Location>() {
                        @Override
                        public void onComplete(@NonNull Task< Location > task) {
    if (task.isSuccessful() && task.getResult() != null)
    {
        mLocation = task.getResult();
    }
    else
    {
        Log.w(TAG, "Failed to get location.");
    }
}
                    });
        } catch (SecurityException unlikely)
{
    Log.e(TAG, "Lost location permission." + unlikely);
}
    }

    private void onNewLocation(Location location)
{
    Log.i(TAG, "New location: " + location);

    mLocation = location;

    // Notify anyone listening for broadcasts about the new location.
    Intent intent = new Intent(ACTION_BROADCAST);
    intent.putExtra(EXTRA_LOCATION, location);
    LocalBroadcastManager.getInstance(getApplicationContext()).sendBroadcast(intent);

    // Update notification content if running as a foreground service.
    if (serviceIsRunningInForeground(this))
    {
        mNotificationManager.notify(NOTIFICATION_ID, getNotification());
    }
}

private void createLocationRequest()
{
    mLocationRequest = new LocationRequest();
    mLocationRequest.setInterval(UPDATE_INTERVAL_IN_MILLISECONDS);
    mLocationRequest.setFastestInterval(FASTEST_UPDATE_INTERVAL_IN_MILLISECONDS);
    mLocationRequest.setPriority(LocationRequest.PRIORITY_HIGH_ACCURACY);
}

public class LocalBinder extends Binder
{
    LocationUpdatesService getService() {
        return LocationUpdatesService.this;
    }
}

public boolean serviceIsRunningInForeground(Context context)
{
    ActivityManager manager = (ActivityManager)context.getSystemService(
            Context.ACTIVITY_SERVICE);
    for (ActivityManager.RunningServiceInfo service : manager.getRunningServices(
            Integer.MAX_VALUE)) {
    if (getClass().getName().equals(service.service.getClassName()))
    {
        if (service.foreground)
        {
            return true;
        }
    }
}
return false;
    }

 
 */
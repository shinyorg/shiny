using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.Settings;
using Android.App;
using Android.Content;
using Android.Gms.Location;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
        readonly AndroidContext context;
        readonly FusedLocationProviderClient client;
        readonly ISettings settings;


        public GpsManagerImpl(AndroidContext context, ISettings settings)
        {
            this.settings = settings;
            this.context = context;
            this.client = LocationServices.GetFusedLocationProviderClient(this.context.AppContext);
        }


        public AccessState Status => this.context.GetCurrentAccessState(P.AccessFineLocation);
        public bool IsListening
        {
            get => this.settings.Get(nameof(GpsManagerImpl), false);
            private set => this.settings.Set(nameof(GpsManagerImpl), value);
        }


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async () =>
        {
            var location = await this.client.GetLastLocationAsync();
            if (location == null)
                return null;

            return new GpsReading(location);
        });


        public Task<AccessState> RequestAccess(bool backgroundMode)
            => this.context.RequestAccess(P.AccessFineLocation).ToTask();


        public async Task StartListener(GpsRequest request = null)
        {
            if (this.IsListening)
                throw new ArgumentException("GPS is already listening");

            var nativeRequest = new LocationRequest()
                .SetInterval(0L)
                .SetFastestInterval(0L)
                .SetPriority(LocationRequest.PriorityHighAccuracy);
                //.SetMaxWaitTime(0L)


            await this.client.RequestLocationUpdatesAsync(
                nativeRequest,
                this.GetPendingIntent()
            );
            this.IsListening = true;
            //mLocationRequest.setInterval(UPDATE_INTERVAL);
            //mLocationRequest.setFastestInterval(FASTEST_UPDATE_INTERVAL);
            //mLocationRequest.setMaxWaitTime(MAX_WAIT_TIME);
        }


        public async Task StopListener()
        {
            if (!this.IsListening)
                return;

            await this.client.RemoveLocationUpdatesAsync(this.GetPendingIntent());
            this.IsListening = false;
        }


        public IObservable<IGpsReading> WhenReading()
            => GpsBroadcastReceiver.WhenReading();


        protected virtual PendingIntent GetPendingIntent()
        {
            var intent = new Intent(this.context.AppContext, typeof(GpsBroadcastReceiver));
            intent.SetAction(GpsBroadcastReceiver.INTENT_ACTION);
            return PendingIntent.GetBroadcast(
                this.context.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
        }
    }
}
/*

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.api.GoogleApiClient;
import com.google.android.gms.location.LocationRequest;
import com.google.android.gms.location.LocationServices;


 * The only activity in this sample. Displays UI widgets for requesting and removing location
 * updates, and for the batched location updates that are reported.
 *
 * Location updates requested through this activity continue even when the activity is not in the
 * foreground. Note: apps running on "O" devices (regardless of targetSdkVersion) may receive
 * updates less frequently than the interval specified in the {@link LocationRequest} when the app
 * is no longer in the foreground.
public class MainActivity extends FragmentActivity implements GoogleApiClient.ConnectionCallbacks,
        GoogleApiClient.OnConnectionFailedListener,
        SharedPreferences.OnSharedPreferenceChangeListener {

    private static final String TAG = MainActivity.class.getSimpleName();
private static final int REQUEST_PERMISSIONS_REQUEST_CODE = 34;
 * The desired interval for location updates. Inexact. Updates may be more or less frequent.
// FIXME: 5/16/17
private static final long UPDATE_INTERVAL = 10 * 1000;

 * The fastest rate for active location updates. Updates will never be more frequent
 * than this value, but they may be less frequent.
// FIXME: 5/14/17
private static final long FASTEST_UPDATE_INTERVAL = UPDATE_INTERVAL / 2;

 * The max time before batched results are delivered by location services. Results may be
 * delivered sooner than this interval.
private static final long MAX_WAIT_TIME = UPDATE_INTERVAL * 3;
*/
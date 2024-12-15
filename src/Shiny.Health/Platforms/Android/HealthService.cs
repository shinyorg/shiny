// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using Android.App;
// using Android.Content;
// using Android.Gms.Auth.Api.SignIn;
// using Android.Gms.Common;
// using Android.Gms.Common.Apis;
// using Android.Gms.Fitness;
// using Android.Gms.Fitness.Data;
// using Android.Gms.Fitness.Request;
// using Java.Util.Concurrent;
// using Shiny.Hosting;
// using NativeDataType = Android.Gms.Fitness.Data.DataType;
//
// namespace Shiny.Health;
//
//
// public class HealthService : IHealthService, IAndroidLifecycle.IOnActivityResult
// {
//     const int REQUEST_CODE = 8765;
//     readonly AndroidPlatform platform;
//
//
//     public HealthService(AndroidPlatform platform)
//     {
//         this.platform = platform;
//     }
//
//
//     public Task<IList<NumericHealthResult>> GetAverageHeartRate(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
//         => this.Query(
//             NativeDataType.AggregateHeartRateSummary,
//             NativeDataType.TypeHeartRateBpm,
//             start,
//             end,
//             interval,
//             (dp, st, end) =>
//             {
//                 var field = dp.DataType.Fields.First();
//                 var value = dp.GetValue(field).AsFloat();
//                 return new NumericHealthResult(DataType.Calories, st, end, value);
//             }
//         );
//
//
//     public Task<IList<NumericHealthResult>> GetCalories(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
//         => this.Query(
//             NativeDataType.AggregateCaloriesExpended,
//             NativeDataType.TypeCaloriesExpended,
//             start,
//             end,
//             interval,
//             (dp, st, end) =>
//             {
//                 var field = dp.DataType.Fields.First();
//                 var value = dp.GetValue(field).AsFloat();
//                 return new NumericHealthResult(DataType.Calories, st, end, value);
//             }
//         );
//
//
//     public Task<IList<NumericHealthResult>> GetDistances(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
//         => this.Query(
//             NativeDataType.AggregateDistanceDelta,
//             NativeDataType.TypeDistanceDelta,
//             start,
//             end,
//             interval,
//             (dp, st, end) =>
//             {
//                 var field = dp.DataType.Fields.First();
//                 var value = dp.GetValue(field).AsFloat();
//                 return new NumericHealthResult(DataType.Distance, st, end, value);
//             }
//         );
//
//
//     public Task<IList<NumericHealthResult>> GetStepCounts(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
//         => this.Query(
//             NativeDataType.AggregateStepCountDelta,
//             NativeDataType.TypeStepCountDelta,
//             start,
//             end,
//             interval,
//             (dp, st, end) =>
//             {
//                 var field = dp.DataType.Fields.First();
//                 var value = dp.GetValue(field).AsInt();
//                 return new NumericHealthResult(DataType.StepCount, st, end, value);
//             }
//         );
//
//
//     TaskCompletionSource<bool>? permissionRequest;
//     public async Task<IEnumerable<(DataType Type, bool Success)>> RequestPermissions(params DataType[] dataTypes)
//     {
//         if (this.IsAuthorizedInternal(dataTypes))
//             return dataTypes.Select(x => (x, false));
//
//         this.permissionRequest = new();
//         //using var _ = cancelToken.Register(() => this.permissionRequest.TrySetCanceled());
//
//         //<uses-permission android:name="android.permission.ACTIVITY_RECOGNITION"/>
//         var options = this.ToFitnessOptions(dataTypes);
//         GoogleSignIn.RequestPermissions(
//             this.platform.CurrentActivity,
//             REQUEST_CODE,
//             GoogleSignIn.GetLastSignedInAccount(this.platform.AppContext),
//             options
//         );
//         var result = await this.permissionRequest.Task.ConfigureAwait(false);
//         return dataTypes.Select(x => (x, result));
//     }
//
//
//     protected bool IsAuthorizedInternal(params DataType[] dataTypes)
//     {
//         var result = false;
//         if (this.IsGooglePlayServicesAvailable())
//         {
//             var options = this.ToFitnessOptions(dataTypes);
//             result = GoogleSignIn.HasPermissions(
//                 GoogleSignIn.GetLastSignedInAccount(this.platform.CurrentActivity),
//                 options
//             );
//         }
//         return result;
//     }
//
//
//     protected FitnessOptions ToFitnessOptions(DataType[] dataTypes)
//     {
//         var options = FitnessOptions.InvokeBuilder();
//
//         foreach (var dataType in dataTypes)
//         {
//             var type = dataType switch
//             {
//                 DataType.Calories => NativeDataType.AggregateCaloriesExpended,
//                 DataType.Distance => NativeDataType.AggregateDistanceDelta,
//                 DataType.HeartRate => NativeDataType.AggregateHeartRateSummary,
//                 DataType.StepCount => NativeDataType.AggregateStepCountDelta
//             };
//             options.AddDataType(type, FitnessOptions.AccessRead);
//         }
//         return options.Build();
//     }
//
//
//     bool IsGooglePlayServicesAvailable()
//     {
//         var googleApi = GoogleApiAvailability.Instance;
//         var status = googleApi.IsGooglePlayServicesAvailable(this.platform.CurrentActivity);
//
//         return status == ConnectionResult.Success;
//     }
//
//
//     const string SIGNIN_STATUS = "googleSignInStatus";
//     public void Handle(Activity activity, int requestCode, Result resultCode, Intent data)
//     {
//         if (data.HasExtra(SIGNIN_STATUS))
//         {
//             var status = (Statuses)data.GetParcelableExtra(SIGNIN_STATUS, Java.Lang.Class.FromType(typeof(Statuses)))!;
//             switch (status.StatusCode)
//             {
//                 case GoogleSignInStatusCodes.SignInCurrentlyInProgress:
//                 case GoogleSignInStatusCodes.Success:
//                     break;
//
//                 default:
//                     this.permissionRequest?.TrySetException(new InvalidOperationException("Google Fit Setup Issue: " + status));
//                     break;
//             }
//         }
//         if (requestCode == REQUEST_CODE)
//             this.permissionRequest?.TrySetResult(resultCode == Result.Ok);
//     }
//
//     async Task<IList<T>> Query<T>(
//         NativeDataType aggregation,
//         NativeDataType dataType,
//         DateTimeOffset start,
//         DateTimeOffset end,
//         Interval interval,
//         Func<DataPoint, DateTimeOffset, DateTimeOffset, T> transform
//     )
//     {
//         var timeUnit = ToNative(interval);
//         var unixStart = Math.Abs(start.ToUnixTimeSeconds());
//         var unixEnd = Math.Abs(end.ToUnixTimeSeconds());
//         var readRequest = new DataReadRequest.Builder()
//             .Aggregate(dataType, aggregation)
//             .BucketByTime(1, timeUnit)
//             .SetTimeRange(unixStart, unixEnd, timeUnit)
//             .Build();
//
//         var list = new List<T>();
//         var dataReadResponse = await FitnessClass
//             .GetHistoryClient(this.platform.CurrentActivity, null)!
//             .ReadDataAsync(readRequest)
//             .ConfigureAwait(false);
//
//         if (dataReadResponse.Buckets.Count > 0)
//         {
//             foreach (var bucket in dataReadResponse.Buckets)
//             {
//                 foreach (var dataSet in bucket.DataSets)
//                 {
//                     foreach (var dp in dataSet.DataPoints)
//                     {
//                         var dstart = DateTimeOffset.FromUnixTimeMilliseconds(dp.GetStartTime(TimeUnit.Milliseconds));
//                         var dend = DateTimeOffset.FromUnixTimeMilliseconds(dp.GetEndTime(TimeUnit.Milliseconds));
//                         var item = transform.Invoke(dp, dstart, dend);
//
//                         list.Add(item);
//                     }
//                 }
//             }
//         }
//         return list;
//     }
//
//     static TimeUnit ToNative(Interval interval) => interval switch
//     {
//         Interval.Days => TimeUnit.Days!,
//         Interval.Hours => TimeUnit.Hours!,
//         Interval.Minutes => TimeUnit.Minutes!,
//         _ => throw new InvalidOperationException("Invalid Interval")
//     };
// }
//
//
//
// //protected FitnessOptions ToFitnessOptions(Permission[] permissions)
// //{
// //    var options = FitnessOptions.InvokeBuilder();
//
// //    foreach (var permission in permissions)
// //    {
// //        if (permission.Type == PermissionType.Read || permission.Type == PermissionType.Both)
// //            options.AddDataType(permission.Metric.DataType, FitnessOptions.AccessRead);
//
// //        if (permission.Type == PermissionType.Write || permission.Type == PermissionType.Both)
// //            options.AddDataType(permission.Metric.DataType, FitnessOptions.AccessWrite);
// //    }
// //    //switch (permission.Kind)
// //    //{
// //    //    case HealthInfoKind.Steps:
// //    //        options
// //    //            .AddDataType(DataType.TypeStepCountCumulative, direction)
// //    //            .AddDataType(DataType.TypeStepCountDelta, direction);
// //    //        break;
//
// //    //    case HealthInfoKind.Distances:
// //    //        options.AddDataType(DataType.TypeDistanceDelta, direction);
// //    //        break;
//
// //    //    case HealthInfoKind.Calories:
// //    //        options.AddDataType(DataType.TypeCaloriesExpended, direction);
// //    //        break;
//
// //    //    case HealthInfoKind.HeartRate:
// //    //        options.AddDataType(DataType.TypeHeartRateBpm, direction);
// //    //        break;
// //    //}
// //    //}
// //    return options.Build();
// //}
//
// //public class DataPointListener : Java.Lang.Object, IOnDataPointListener
// //{
// //    readonly TaskCompletionSource<DataPoint> onDataPoint;
// //    public DataPointListener(TaskCompletionSource<DataPoint> onDataPoint) => this.onDataPoint = onDataPoint;
// //    public void OnDataPoint(DataPoint dataPoint) => this.onDataPoint.SetResult(dataPoint);
// //}
//
// // for writing
// //var client = FitnessClass
// // .GetRecordingClient(
// //     act,
// //     GoogleSignIn.GetLastSignedInAccount(act)
// // );
//
// //public IObservable<T> Monitor<T>(HealthMetric<T> metric) => Observable.Create<T>(ob =>
// //{
// //    var act = this.platform.CurrentActivity;
// //    var listener = new DataPointListener(dp =>
// //    {
// //        var value = metric.FromNative(dp);
// //        ob.OnNext(value);
// //    });
//
// //    var client = FitnessClass.GetSensorsClient(act, GoogleSignIn.GetLastSignedInAccount(act));
//
// //    client
// //        .AddAsync(
// //            new SensorRequest.Builder()
// //                .SetDataType(metric.DataType)
// //                .SetSamplingRate(10, TimeUnit.Seconds)
// //                .Build(),
// //            listener
// //        )
// //        .ContinueWith(x =>
// //        {
// //            if (x.Exception != null)
// //                ob.OnError(x.Exception);
// //        });
//
// //    return () => client.Remove(listener);
// //});
//
//
// //public Task<bool> IsAuthorized(params Permission[] permissions)
// //    => Task.FromResult(this.IsAuthorizedInternal(permissions));
//
//
// //void Google()
// //{
// //    var apiClient = new GoogleApiClient.Builder(this.platform.CurrentActivity)
// //        .AddApi(FitnessClass.SENSORS_API)
// //        .UseDefaultAccount()
// //        .AddScope(FitnessClass.ScopeActivityRead)
// //        .AddConnectionCallbacks(bundle =>
// //        {
// //            //if (data.HasExtra(SIGNIN_STATUS))
// //            //{
// //            //    var status = (Statuses)data.GetParcelableExtra(SIGNIN_STATUS, Java.Lang.Class.FromType(typeof(Statuses)))!;
// //            //    switch (status.StatusCode)
// //            //    {
// //            //        case GoogleSignInStatusCodes.SignInCurrentlyInProgress:
// //            //        case GoogleSignInStatusCodes.Success:
// //            //            break;
//
// //            //        default:
// //            //            this.permissionRequest?.TrySetException(new InvalidOperationException("Google Fit Setup Issue: " + status));
// //            //            break;
// //            //    }
// //            //}
// //            //if (requestCode == REQUEST_CODE)
// //            //    this.permissionRequest?.TrySetResult(resultCode == Result.Ok);
// //        })
// //        .AddOnConnectionFailedListener(result =>
// //        {
//
// //        })
// //        //.AddScope(FitnessClass.ScopeActivityReadWrite) 
// //        .Build();
//
// //    apiClient.Connect();
//
// //    //.AddConnectionCallbacks()
// //}
// //    /*
// //    public class MyActivity extends FragmentActivity
// //                 implements ConnectionCallbacks, OnConnectionFailedListener, OnDataPointListener {
// //            private static final int REQUEST_OAUTH = 1001;
// //            private GoogleApiClient mGoogleApiClient;
//
// //            @Override
// //            protected void onCreate(@Nullable Bundle savedInstanceState) {
// //                super.onCreate(savedInstanceState);
//
// //                // Create a Google Fit Client instance with default user account.
// //                mGoogleApiClient = new GoogleApiClient.Builder(this)
// //                        .addApi(Fitness.SENSORS_API)  // Required for SensorsApi calls
// //                        // Optional: specify more APIs used with additional calls to addApi
// //                        .useDefaultAccount()
// //                        .addScope(Fitness.SCOPE_ACTIVITY_READ_WRITE)
// //                        .addConnectionCallbacks(this)
// //                        .addOnConnectionFailedListener(this)
// //                        .build();
//
// //                mGoogleApiClient.connect();
// //            }
//
// //            @Override
// //            public void onConnected(Bundle connectionHint) {
// //                // Connected to Google Fit Client.
// //                Fitness.SensorsApi.add(
// //                        mGoogleApiClient,
// //                        new SensorRequest.Builder()
// //                                .setDataType(DataType.STEP_COUNT_DELTA)
// //                                .build(),
// //                        this);
// //            }
//
// //            @Override
// //            public void onDataPoint(DataPoint dataPoint) {
// //                // Do cool stuff that matters.
// //            }
//
// //            @Override
// //            public void onConnectionSuspended(int cause) {
// //                // The connection has been interrupted. Wait until onConnected() is called.
// //            }
//
// //            @Override
// //            public void onConnectionFailed(ConnectionResult result) {
// //                // Error while connecting. Try to resolve using the pending intent returned.
// //                if (result.getErrorCode() == FitnessStatusCodes.NEEDS_OAUTH_PERMISSIONS) {
// //                    try {
// //                        result.startResolutionForResult(this, REQUEST_OAUTH);
// //                    } catch (SendIntentException e) {
// //                    }
// //                }
// //            }
//
// //            @Override
// //            public void onActivityResult(int requestCode, int resultCode, Intent data) {
// //                if (requestCode == REQUEST_OAUTH && resultCode == RESULT_OK) {
// //                    mGoogleApiClient.connect();
// //                }
// //            } 
// //     */
// //    //        .useDefaultAccount()
// //    //.addScope(Fitness.SCOPE_ACTIVITY_READ_WRITE)
//
//
//
//
// //public AccessState GetCurrentStatus(Permission permission)
// //{
// //    throw new NotImplementedException();
// //}
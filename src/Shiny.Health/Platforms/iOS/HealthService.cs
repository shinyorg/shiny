using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using HealthKit;

namespace Shiny.Health;


public class HealthService : IHealthService
{
    public Task<IList<NumericHealthResult>> GetAverageHeartRate(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
        => this.Query(
            HKQuantityTypeIdentifier.HeartRate,
            HKStatisticsOptions.DiscreteAverage,
            start,
            end,
            interval,
            result =>
            {
                var avg = result.AverageQuantity()?.GetDoubleValue(HKUnit.Count.UnitDividedBy(HKUnit.Minute)) ?? 0;
                return new NumericHealthResult(
                    DataType.HeartRate,
                    start,
                    end,
                    avg
                );
            },
            cancelToken
        );


    public Task<IList<NumericHealthResult>> GetCalories(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
        => this.Query(
            HKQuantityTypeIdentifier.ActiveEnergyBurned,
            HKStatisticsOptions.CumulativeSum,
            start,
            end,
            interval,
            result =>
            {
                var sum = result.SumQuantity()?.GetDoubleValue(HKUnit.Kilocalorie) ?? 0;
                return new NumericHealthResult(
                    DataType.Calories,
                    result.StartDate.ToDateTime(),
                    result.EndDate.ToDateTime(),
                    sum
                );
            },
            cancelToken
        );


    public Task<IList<NumericHealthResult>> GetDistances(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
        // TODO: what about cycling/swimming?
        => this.Query(
            HKQuantityTypeIdentifier.DistanceWalkingRunning,
            HKStatisticsOptions.CumulativeSum,
            start,
            end,
            interval,
            result =>
            {
                var sum = result.SumQuantity()?.GetDoubleValue(HKUnit.Meter) ?? 0;
                return new NumericHealthResult(
                    DataType.Distance,
                    result.StartDate.ToDateTime(),
                    result.EndDate.ToDateTime(),
                    sum
                );
            },
            cancelToken
        );

    public Task<IList<NumericHealthResult>> GetStepCounts(DateTimeOffset start, DateTimeOffset end, Interval interval, CancellationToken cancelToken = default)
        => this.Query(
            HKQuantityTypeIdentifier.StepCount,
            HKStatisticsOptions.CumulativeSum,
            start,
            end,
            interval,
            result =>
            {
                var sum = result.SumQuantity()?.GetDoubleValue(HKUnit.Count) ?? 0;
                return new NumericHealthResult(
                    DataType.StepCount,
                    result.StartDate.ToDateTime(),
                    result.EndDate.ToDateTime(),
                    sum
                );
            },
            cancelToken
        );


    public async Task<IEnumerable<(DataType Type, bool Success)>> RequestPermissions(params DataType[] dataTypes)
    {
        //    if (!OperatingSystemShim.IsIOSVersionAtLeast(12))
        //        return false;

        //    if (!HKHealthStore.IsHealthDataAvailable)
        //        return false; 
        var share = new NSMutableSet<HKSampleType>();
        var read = new NSMutableSet<HKObjectType>();

        foreach (var dataType in dataTypes)
        {
            var native = ToNativeType(dataType);
            var qtyType = HKQuantityType.Create(native)!;
            read.Add(qtyType);
            //if (permission.Type == PermissionType.Read || permission.Type == PermissionType.Both)
            //    read.Add(qtyType);

            //if (permission.Type == PermissionType.Write || permission.Type == PermissionType.Both)
            //    share.Add(qtyType);
        }
        // TODO: throw if no read permissions?

        using var store = new HKHealthStore();
        var tuple = await store.RequestAuthorizationToShareAsync(
            new NSSet<HKSampleType>(share.ToArray()),
            new NSSet<HKObjectType>(read.ToArray())
        );
        if (!tuple.Item1)
            throw new InvalidOperationException(tuple.Item2.LocalizedDescription);

        var list = new List<(DataType, bool)>();
        foreach (var dataType in dataTypes)
        {
            var good = GetCurrentStatus(dataType) == AccessState.Available;
            list.Add((dataType, good));
        }
        return list;
    }


    public AccessState GetCurrentStatus(DataType dataType)
    {
        if (!OperatingSystemShim.IsIOSVersionAtLeast(12))
            return AccessState.NotSupported;

        if (!HKHealthStore.IsHealthDataAvailable)
            return AccessState.NotSupported;

        using var store = new HKHealthStore();
        var native = ToNativeType(dataType);
        var type = HKQuantityType.Create(native)!;
        var status = store.GetAuthorizationStatus(type);

        return status switch
        {
            HKAuthorizationStatus.NotDetermined => AccessState.Unknown,
            HKAuthorizationStatus.SharingDenied => AccessState.Denied,
            HKAuthorizationStatus.SharingAuthorized => AccessState.Available
        };
    }

    static HKQuantityTypeIdentifier ToNativeType(DataType dataType) => dataType switch
    {
        DataType.StepCount => HKQuantityTypeIdentifier.StepCount,
        DataType.HeartRate => HKQuantityTypeIdentifier.HeartRate,
        DataType.Calories => HKQuantityTypeIdentifier.ActiveEnergyBurned,
        DataType.Distance => HKQuantityTypeIdentifier.DistanceWalkingRunning,
        _ => throw new InvalidOperationException("Invalid Type")
    };


    async Task<IList<T>> Query<T>(
        HKQuantityTypeIdentifier quantityTypeIdentifier,
        HKStatisticsOptions statsOption,
        DateTimeOffset start,
        DateTimeOffset end,
        Interval interval,
        Func<HKStatistics, T?> transform,
        CancellationToken cancellationToken
    )
    {
        var tcs = new TaskCompletionSource<IList<T>>();
        var calendar = NSCalendar.CurrentCalendar;

        var anchorComponents = calendar.Components(
            NSCalendarUnit.Day | NSCalendarUnit.Month | NSCalendarUnit.Year,
            (NSDate)start.LocalDateTime
        );
        anchorComponents.Hour = 0;
        var anchorDate = calendar.DateFromComponents(anchorComponents);
        var qtyType = HKQuantityType.Create(quantityTypeIdentifier)!;

        var query = new HKStatisticsCollectionQuery(
            qtyType,
            null,
            statsOption,
            anchorDate,
            ToNative(interval)
        );
        query.InitialResultsHandler = (qry, results, err) =>
        {
            if (err != null)
            {
                tcs.TrySetException(new InvalidOperationException(err.Description));
            }
            else
            {
                var list = new List<T>();

                results.EnumerateStatistics(
                    (NSDate)start.LocalDateTime,
                    (NSDate)end.LocalDateTime,
                    (result, stop) =>
                    {
                        try
                        {
                            var value = transform(result);
                            if (value != null)
                                list.Add(value);
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                        }
                    }
                );
                tcs.TrySetResult(list);
            }
        };

        using var store = new HKHealthStore();
        using var ct = cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled();
            store.StopQuery(query);
        });

        store.ExecuteQuery(query);
        var result = await tcs.Task.ConfigureAwait(false);
        return result;
    }


    //public async Task<bool> IsAuthorized(params Permission[] permissions)
    //{
    //    if (!OperatingSystemShim.IsIOSVersionAtLeast(12))
    //        return false;

    //    if (!HKHealthStore.IsHealthDataAvailable)
    //        return false;

    //    var perms = ToSets(permissions);
    //    using var store = new HKHealthStore();

    //    var result = await store.GetRequestStatusForAuthorizationToShareAsync(perms.Share, perms.Read);
    //    //result == HKAuthorizationRequestStatus.ShouldRequest
    //    return result == HKAuthorizationRequestStatus.Unnecessary;
    //}


    static NSDateComponents ToNative(Interval interval)
    {
        var native = new NSDateComponents();

        switch (interval)
        {
            case Interval.Days:
                native.Day = 1;
                break;

            case Interval.Hours:
                native.Hour = 1;
                break;

            case Interval.Minutes:
                native.Minute = 1;
                break;
        }
        return native;
    }
}


//static (NSSet<HKSampleType> Share, NSSet<HKObjectType> Read) ToSets(Permission[] permissions)
//{
//    var share = new NSMutableSet<HKSampleType>();
//    var read = new NSMutableSet<HKObjectType>();

//    foreach (var permission in permissions)
//    {
//        var qtyType = HKQuantityType.Create(permission.Metric.QuantityTypeIdentifier)!;
//        if (permission.Type == PermissionType.Read || permission.Type == PermissionType.Both)
//            read.Add(qtyType);

//        if (permission.Type == PermissionType.Write || permission.Type == PermissionType.Both)
//            share.Add(qtyType);
//    }
//    // TODO: throw if no read permissions?
//    return (
//        new NSSet<HKSampleType>(share.ToArray()),
//        new NSSet<HKObjectType>(read.ToArray())
//    );
//}



//public IObservable<T> Monitor<T>(HealthMetric<T> metric) => Observable.Create<T>(ob =>
//{
//    var predicate = HKQuery.GetPredicateForSamples(NSDate.Now, null, HKQueryOptions.None);
//    var qty = HKQuantityType.Create(metric.QuantityTypeIdentifier)!;

//    var query = new HKAnchoredObjectQuery(
//        qty,
//        predicate,
//        null,
//        0,
//        new HKAnchoredObjectUpdateHandler((qry, sample, deleted, anchor, e) =>
//        {
//            if (e != null)
//            {

//            }
//            else
//            {
//                foreach (var s in sample)
//                {
//                    if (s is HKQuantitySample qtys)
//                    {
//                        //var value = metric.FromNative(qtys);
//                        //ob.OnNext(null)
//                    }
//                }
//            }
//            //.Quantity.GetDoubleValue
//            //sample[0].Source;
//            //sample[0].SourceRevision
//            //sample[0].Uuid
//            //sample[0].StartDate
//            //sample[0].EndDate
//        })
//    );
//    var store = new HKHealthStore();
//    store.ExecuteQuery(query);

//    return () =>
//    {
//        store.StopQuery(query);
//        store.Dispose();
//    };
//});




////public class SleepAnalysisHealthMetric : HealthMetric<object>
//////{
////#if ANDROID
////    public override DataType AggregationDataType => throw new NotImplementedException();
////    public override DataType DataType => throw new NotImplementedException();

////    public override object FromNative(DataPoint dataPoint)
////    {
////        throw new NotImplementedException();
////    }
////    //    @RequiresApi(api = Build.VERSION_CODES.N)
////    //    public void getSleepAnalysis(Context context, double startDate, double endDate, final Promise promise)
////    //    {
////    //        if (android.os.Build.VERSION.SDK_INT < android.os.Build.VERSION_CODES.N)
////    //        {
////    //            promise.reject(String.valueOf(FitnessError.ERROR_METHOD_NOT_AVAILABLE), "Method not available");
////    //            return;
////    //        }

////    //        SessionReadRequest request = new SessionReadRequest.Builder()
////    //                .readSessionsFromAllApps()
////    //                .read(DataType.TYPE_ACTIVITY_SEGMENT)
////    //                .setTimeInterval((long)startDate, (long)endDate, TimeUnit.MILLISECONDS)
////    //                .build();

////    //        Fitness.getSessionsClient(context, GoogleSignIn.getLastSignedInAccount(context))
////    //                .readSession(request)
////    //                .addOnSuccessListener(new OnSuccessListener<SessionReadResponse>() {
////    //                    @Override
////    //                    public void onSuccess(SessionReadResponse response)
////    //        {
////    //            List<Object> sleepSessions = response.getSessions()
////    //                .stream()
////    //                .filter(new Predicate<Session>() {
////    //                                @Override
////    //                                public boolean test(Session s)
////    //            {
////    //                return s.getActivity().equals(FitnessActivities.SLEEP);
////    //            }
////    //        })
////    //                            .collect(Collectors.toList());

////    //        WritableArray sleep = Arguments.createArray();
////    //        for (Object session : sleepSessions)
////    //        {
////    //            List<DataSet> dataSets = response.getDataSet((Session)session);
////    //            for (DataSet dataSet : dataSets)
////    //            {
////    //                processSleep(dataSet, (Session)session, sleep);
////    //            }
////    //        }

////    //        promise.resolve(sleep);
////    //    }
////    //})
////    //                .addOnFailureListener(new OnFailureListener()
////    //{
////    //    @Override
////    //                    public void onFailure(@NonNull Exception e)
////    //    {
////    //        promise.reject(e);
////    //    }
////    //});
////    //    }

////    //private void processSleep(DataSet dataSet, Session session, WritableArray map)
////    //{

////    //    for (DataPoint dp : dataSet.getDataPoints())
////    //    {
////    //        for (Field field : dp.getDataType().getFields())
////    //        {
////    //            WritableMap sleepMap = Arguments.createMap();
////    //            sleepMap.putString("value", dp.getValue(field).asActivity());
////    //            sleepMap.putString("sourceId", session.getIdentifier());
////    //            sleepMap.putString("startDate", dateFormat.format(dp.getStartTime(TimeUnit.MILLISECONDS)));
////    //            sleepMap.putString("endDate", dateFormat.format(dp.getEndTime(TimeUnit.MILLISECONDS)));
////    //            map.pushMap(sleepMap);
////    //        }
////    //    }
////    //}
////#endif


////    //RCT_REMAP_METHOD(getSleepAnalysis,
////    //                 withStartDate: (double) startDate

////    //                 andEndDate: (double) endDate

////    //                 withSleepAnalysisResolver:(RCTPromiseResolveBlock) resolve

////    //                 andSleepAnalysisRejecter:(RCTPromiseRejectBlock) reject)
////    //{

////    //    if (!startDate)
////    //    {
////    //        NSError* error = [RCTFitness createErrorWithCode: ErrorDateNotCorrect andDescription: RCT_ERROR_DATE_NOT_CORRECT];
////    //        [RCTFitness handleRejectBlock:reject error:error] ;
////    //        return;
////    //    }

////    //    NSDate* sd = [RCTFitness dateFromTimeStamp: startDate / 1000];
////    //    NSDate* ed = [RCTFitness dateFromTimeStamp: endDate / 1000];

////    //    HKSampleType* sampleType = [HKSampleType categoryTypeForIdentifier: HKCategoryTypeIdentifierSleepAnalysis];
////    //    NSPredicate* predicate = [HKQuery predicateForSamplesWithStartDate: sd endDate: ed options: HKQueryOptionNone];

////    //    HKSampleQuery* query = [[HKSampleQuery alloc] initWithSampleType: sampleType predicate:predicate limit:0 sortDescriptors: nil resultsHandler:^(HKSampleQuery * query, NSArray * results, NSError * error) {
////    //        if (error)
////    //        {
////    //            NSError* error = [RCTFitness createErrorWithCode: ErrorNoEvents andDescription: RCT_ERROR_NO_EVENTS];
////    //            [RCTFitness handleRejectBlock:reject error:error] ;
////    //            return;
////    //        }

////    //        NSMutableArray* data = [NSMutableArray arrayWithCapacity: 1];

////    //    for (HKCategorySample* sample in results)
////    //        {
////    //            NSString* startDateString = [RCTFitness ISO8601StringFromDate: sample.startDate];
////    //            NSString* endDateString = [RCTFitness ISO8601StringFromDate: sample.endDate];

////    //            NSString* valueString;

////    //            switch (sample.value)
////    //            {
////    //                case HKCategoryValueSleepAnalysisInBed:
////    //                    valueString = @"INBED";
////    //                    break;
////    //                case HKCategoryValueSleepAnalysisAsleep:
////    //                    valueString = @"ASLEEP";
////    //                    break;
////    //                default:
////    //                    valueString = @"UNKNOWN";
////    //                    break;
////    //            }

////    //            NSDictionary* elem = @{
////    //                @"value" : valueString,
////    //                @"sourceName" : [[[sample sourceRevision] source] name],
////    //                @"sourceId" : [[[sample sourceRevision] source] bundleIdentifier],
////    //                @"startDate" : startDateString,
////    //                @"endDate" : endDateString,
////    //        };

////    //            [data addObject:elem] ;
////    //        }

////    //        dispatch_async(dispatch_get_main_queue(), ^{
////    //            resolve(data);
////    //        });
////    //    }];

////    //    [self.healthStore executeQuery:query] ;
////    //}
////#endif
////}
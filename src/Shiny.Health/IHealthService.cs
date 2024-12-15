using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Health;


public interface IHealthService
{
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="permission"></param>
    //    /// <returns></returns>
    //    AccessState GetCurrentStatus(DataType dataType);

    // TODO: I really need this to come back as a batch of approve/deny since Apple Health allows this
    /// <summary>
    /// 
    /// </summary>
    /// <param name="permissions"></param>
    /// <returns></returns>
    Task<IEnumerable<(DataType Type, bool Success)>> RequestPermissions(params DataType[] dataTypes);
    // returns each data type with a read and a write state
    // request by multiple data types with different

    Task<IList<NumericHealthResult>> GetCalories(
        DateTimeOffset start,
        DateTimeOffset end,
        Interval interval,
        CancellationToken cancelToken = default
    );

    Task<IList<NumericHealthResult>> GetDistances(
        DateTimeOffset start,
        DateTimeOffset end,
        Interval interval,
        CancellationToken cancelToken = default
    );

    Task<IList<NumericHealthResult>> GetStepCounts(
        DateTimeOffset start,
        DateTimeOffset end,
        Interval interval,
        CancellationToken cancelToken = default
    );

    Task<IList<NumericHealthResult>> GetAverageHeartRate(
        DateTimeOffset start,
        DateTimeOffset end,
        Interval interval,
        CancellationToken cancelToken = default
    );


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="metric"></param>
    /// <returns></returns>
    //IObservable<T> Monitor<T>(HealthMetric<T> metric);
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Linq;

namespace Shiny.Net.Http;


public record HttpTransferMetric2(
    double Progress, // 0.0 - 1.0
    long BytesPerSecond = 0,
    TimeSpan? EstimatedTimeRemaining = null
);


public static class HttpClientExtensions
{
    public static IObservable<HttpTransferMetric2> Download(this HttpClient httpClient, HttpRequestMessage request, string saveFullPath) => Observable.Create<HttpTransferMetric2>(async ob =>
    {
        var response = await httpClient.SendAsync(request);
        response.
        return () =>
        {

        };
    });



    public static IObservable<HttpTransferMetric2> Upload(this HttpClient httpClient, HttpRequestMessage request, string uploadFullPath) => Observable.Create<HttpTransferMetric2>(ob =>
    {
        return () =>
        {
        };
    });
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public class HttpTransferManager : IHttpTransferManager
{
    //readonly IRepository<HttpTransferRequest> repository;

    //public HttpTransferManager(IRepository<HttpTransferRequest> repository)
    //{
    //    this.repository = repository;
    //}


    public string? Title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public string? Message { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int Progress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int Total { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public bool IsIndeterministic { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task Cancel(string id) => throw new NotImplementedException();
    public Task Cancel(QueryFilter? filter = null) => throw new NotImplementedException();
    public Task<HttpTransfer> Enqueue(HttpTransferRequest request) => throw new NotImplementedException();
    public Task<HttpTransfer> GetTransfer(string id) => throw new NotImplementedException();
    public Task<IList<HttpTransfer>> GetTransfers(QueryFilter? filter = null) => throw new NotImplementedException();
    public IObservable<HttpTransfer> WhenUpdated() => throw new NotImplementedException(); // TODO: should I move this?
}

//using System;
//using System.IO;
//using System.Reactive.Linq;
//using System.Reactive.Disposables;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Net.Http;
//using Microsoft.Extensions.Logging;
//using Android;
//using Android.Content;
//using Observable = System.Reactive.Linq.Observable;
//using Native = Android.App.DownloadManager;
//using Shiny.Stores;

//namespace Shiny.Net.Http;


//public class HttpTransferManager : HttpClientHttpTransferManager
//{
//    readonly AndroidPlatform platform;
//    IObservable<HttpTransfer>? httpObs;

//    public HttpTransferManager(
//        AndroidPlatform platform,
//        IRepository<HttpTransfer> repository,
//        ILogger<IHttpTransferManager> logger
//    )
//    : base(
//        logger,
//        repository
//    )
//    {
//        this.platform = platform;
//    }


//    public void Start() => this.platform.RegisterBroadcastReceiver<HttpTransferBroadcastReceiver>(
//        Native.ActionDownloadComplete,
//        Intent.ActionBootCompleted
//    );


//    public async Task Cancel(string identifier)
//    {
//        await base.Cancel(identifier);
//        if (Int64.TryParse(identifier, out var id))
//        {
//            this.platform
//                .GetManager()
//                .Remove(id);
//        }
//    }


//    public override IObservable<HttpTransfer> WhenUpdated()
//    {
//        var query = new QueryFilter().ToNative();

//        this.httpObs ??= Observable
//            .Create<HttpTransfer>(ob =>
//            {
//                var lastRun = DateTime.UtcNow;
//                var disposer = new CompositeDisposable();

//                HttpTransferBroadcastReceiver
//                    .HttpEvents
//                    .Subscribe(ob.OnNext)
//                    .DisposedBy(disposer);

//                Observable
//                    .Interval(TimeSpan.FromSeconds(2))
//                    .Subscribe(_ =>
//                    {
//                        using (var cursor = this.platform.GetManager().InvokeQuery(query))
//                        {
//                            while (cursor.MoveToNext())
//                            {
//                                var lastModEpoch = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnLastModifiedTimestamp));
//                                var epoch = DateTimeOffset.FromUnixTimeMilliseconds(lastModEpoch);
//                                if (epoch > lastRun)
//                                {
//                                    var transfer = cursor.ToLib();
//                                    ob.OnNext(transfer);
//                                }
//                            }
//                        }

//                        lastRun = DateTime.UtcNow;
//                    })
//                    .DisposedBy(disposer);

//                return disposer;
//            })
//            .Publish()
//            .RefCount();

//        return this.httpObs.Merge(base.WhenUpdated());
//    }


//    protected async Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
//    {
//        if (request.HttpMethod != HttpMethod.Get)
//            throw new ArgumentException("Only GETs are supported for downloads on Android");

//        var access = await this.platform.RequestAccess(Manifest.Permission.WriteExternalStorage);
//        if (access != AccessState.Available)
//            throw new ArgumentException("Invalid access to external storage - " + access);

//        access = await this.platform.RequestAccess(Manifest.Permission.ReadExternalStorage);
//        if (access != AccessState.Available)
//            throw new ArgumentException("Invalid access to external storage - " + access);

//        var dlPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
//        var path = Path.Combine(dlPath, request.LocalFile.Name);

//        var native = new Native
//            .Request(Android.Net.Uri.Parse(request.Uri))
//            .SetDescription(request.LocalFile.FullName)
//            .SetDestinationUri(ToNativeUri(path)) // WRITE_EXTERNAL_STORAGE
//            .SetAllowedOverMetered(request.UseMeteredConnection);

//        foreach (var header in request.Headers)
//            native.AddRequestHeader(header.Key, header.Value);

//        var id = this.platform.GetManager().Enqueue(native);
//        return new HttpTransfer(
//            id.ToString(),
//            request.Uri,
//            request.LocalFile.FullName,
//            false,
//            request.UseMeteredConnection,
//            null,
//            0,
//            0,
//            HttpTransferState.Pending
//        );
//    }


//    protected Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
//        => Task.FromResult(this.GetAll(filter));


//    IEnumerable<HttpTransfer> GetAll(QueryFilter filter)
//    {
//        var query = filter.ToNative();
//        using var cursor = this.platform.GetManager().InvokeQuery(query);
//        while (cursor.MoveToNext())
//            yield return cursor.ToLib();
//    }


//    static Android.Net.Uri ToNativeUri(string filePath)
//    {
//        var native = new Java.IO.File(filePath);
//        return Android.Net.Uri.FromFile(native);
//    }
//}

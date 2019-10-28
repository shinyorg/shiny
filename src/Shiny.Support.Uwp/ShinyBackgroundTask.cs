using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;


namespace Shiny.Support.Uwp
{
    public sealed class ShinyBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var host = Type.GetType("Shiny.UwpShinyHost, Shiny.Core");
            var method = host.GetMethod("BackgroundRun");
            method.Invoke(host, new[] { taskInstance });
        }

        //static readonly ConcurrentDictionary<Guid, AppServiceConnection> Connections = new ConcurrentDictionary<Guid, AppServiceConnection>();
        //BackgroundTaskDeferral deferral;
        //Guid connectionId;


        //public void Run(IBackgroundTaskInstance taskInstance)
        //{
        //    var appComm = taskInstance.TriggerDetails as AppServiceTriggerDetails;

        //    if (appComm?.AppServiceConnection == null)
        //    {
        //        var host = Type.GetType("Shiny.UwpShinyHost, Shiny.Core");
        //        var method = host.GetMethod("BackgroundRun");
        //        method.Invoke(host, new[] { taskInstance });
        //    }
        //    else
        //    {
        //        taskInstance.GetDeferral();

        //        this.connectionId = Guid.NewGuid();
        //        Connections.TryAdd(this.connectionId, appComm.AppServiceConnection);

        //        taskInstance.Canceled += this.OnCancelled;
        //        appComm.AppServiceConnection.RequestReceived += this.OnRequestReceived;
        //        appComm.AppServiceConnection.ServiceClosed += this.OnServiceClosed;
        //    }
        //}


        //void OnCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        //{
        //    RemoveConnection(this.connectionId);
        //    this.deferral?.Complete();
        //    this.deferral = null;
        //}


        //void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        //    => RemoveConnection(this.connectionId);


        //async Task SendMessage(Guid connectionKey, AppServiceConnection connection, ValueSet message)
        //{
        //    try
        //    {
        //        var result = await connection.SendMessageAsync(message);

        //        if (result.Status == AppServiceResponseStatus.Failure)
        //            RemoveConnection(connectionKey);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

        //async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        //{
        //    var appServiceDeferral = args.GetDeferral();
        //    try
        //    {
        //        var otherConnections = Connections
        //            .ToList()
        //            .Where(i => i.Key != this.connectionId)
        //            .ToList();

        //        foreach (var connection in otherConnections)
        //        {
        //            await SendMessage(connection.Key, connection.Value, args.Request.Message);
        //        }
        //    }
        //    finally
        //    {
        //        appServiceDeferral.Complete();
        //    }
        //}


        //static void RemoveConnection(Guid key)
        //{
        //    var connection = Connections[key];
        //    connection.Dispose();
        //    Connections.TryRemove(key, out _);
        //}
    }
}

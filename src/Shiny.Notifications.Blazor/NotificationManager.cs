//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.JSInterop;
//using Shiny.Stores;
//using Shiny.Infrastructure;

//namespace Shiny.Notifications.Blazor;


//public class NotificationManager : INotificationManager, IShinyWebAssemblyService
//{
//    readonly IRepository<Notification> repository;


//    public NotificationManager(IRepository<Notification> repository)
//    {
//        this.repository = repository;
//    }

//    public Task OnStart(IJSInProcessRuntime jsRuntime) => throw new NotImplementedException();

//    public Task AddChannel(Notifications.Channel channel) => throw new NotImplementedException();
//    public Task Cancel(int id) => throw new NotImplementedException();
//    public Task Cancel(CancelScope cancelScope = CancelScope.All) => throw new NotImplementedException();
//    public Task ClearChannels() => throw new NotImplementedException();
//    public Task<Notifications.Channel?> GetChannel(string channelId) => throw new NotImplementedException();
//    public Task<IList<Notifications.Channel>> GetChannels() => throw new NotImplementedException();
//    public Task<Notification?> GetNotification(int notificationId) => throw new NotImplementedException();
//    public Task<IEnumerable<Notification>> GetPendingNotifications() => throw new NotImplementedException();
//    public Task RemoveChannel(string channelId) => throw new NotImplementedException();
//    public Task<Shiny.AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification) => throw new NotImplementedException();
//    public Task Send(Notification notification) => throw new NotImplementedException();
//}


using System;
using System.Windows.Input;
using Acr.UserDialogs;
using Shiny.Notifications;
using ReactiveUI;


namespace Samples.Notifications
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(INotificationManager notificationManager, IUserDialogs dialogs)
        {
            this.SendTest = ReactiveCommand.CreateFromTask(() => notificationManager.Send(new Notification
            {
                Title = "Hello",
                Message = "This is a test message"
            }));
            this.PermissionCheck = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await notificationManager.RequestAccess();
                dialogs.Toast("Permission Check Result: " + result);
            });
        }


        public ICommand SendTest { get; }
        public ICommand PermissionCheck { get; }
    }
}
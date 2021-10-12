Title: Events and Delegates
---

```csharp
using Shiny;
using Shiny.Notifications;


namespace YourNamespace 
{
    public class NotificationDelegate : INotificationDelegate
    {
        public NotificationDelegate()
        {
            // yes, you can dependency inject here
        }


        public async Task OnEntry(Notification notification) 
        {
        }


        public async Task OnReceived(Notification notification)
        {
        }
    }
}
```

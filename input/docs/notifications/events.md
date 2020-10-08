<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/notifications/events.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Events and Delegates
Order: 2
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

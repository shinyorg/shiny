using System;
using AndroidX.Core.App;

namespace Shiny;


public interface IAndroidForegroundServiceDelegate
{
    void Configure(NotificationCompat.Builder builder);
}
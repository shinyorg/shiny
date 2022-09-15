using System;
using AndroidX.Core.App;

namespace Shiny;


// TODO: need to be able to pass shiny service argument to this as well
public interface IAndroidForegroundService<T>
{
    // TODO: what if user wants to hold a reference to CHANGE notification value(s)?
    void Configure(NotificationCompat.Builder builder, T arg);
}
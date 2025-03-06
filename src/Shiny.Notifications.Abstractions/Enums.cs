using System;

[Flags]
public enum AccessRequestFlags
{
    Notification = 0,
    LocationAware = 1,
    TimeSensitivity = 2,
    All = Notification | LocationAware | TimeSensitivity
}


public enum CancelScope
{
    DisplayedOnly,
    Pending,
    All,
}
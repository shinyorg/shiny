Title: Motion Activity
---
Motion activity is a great mechanism for querying for how your user is moving.  This set of API's works slightly different than other APIs in Shiny.  iOS does not allow watching for these events in the background, but it allows you to query by date/time range (careful, the query can yield A LOT of data).  In order to emulate this same queryable mechanism on Android, Shiny starts up a broadcast receiver that continually receives events and stores them in a local SQLite database.

The events captured by this API are
* Automotive
* Stationary
* Walking
* Running
* Cycling

<?! PackageInfo "Shiny.Locations" "Shiny.Locations.IMotionActivityManager" /?>


## Usage

You can either inject this service or resolve it via the Shiny.ShinyHost.Resolve method.  The interface being used here is Shiny.Locations.IMotionActivity

Make sure before you get going to check support on your OS using 

```csharp
if (motionActivity.IsSupport) {

}
```

Here are the main enums you'll be working with
```csharp
[Flags]
public enum MotionActivityType
{
    Unknown = 0,
    Stationary = 1,
    Walking = 2,
    Running = 4,
    Automotive = 8,
    Cycling = 16
}


public enum MotionActivityConfidence
{
    Low,
    Medium,
    High
}

```

### querying

The main part of this API is the querying which is extrememly straightforward.  Careful - if your time range is wide, this method will easily return 1000s of rows.

```csharp

var data = await motionActivity.Query(startDateTime, endDateTime);
foreach (var record in data) {
    //record.Types.HasEnum() // types is a flags enum as shown above
    // record.Confidence
    //record.Timestamp
}
```

### monitoring (foreground only)

You can monitor events changes while your app is in the foreground using the following

```csharp
motionActivity
    .WhenActivityChanged()
    .Subscribe(x => {
        // you will get the same type of event as the query
        // make sure to pop on the main thread here :)
    })
```
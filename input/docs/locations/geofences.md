Title: Geofences
---

## HOW TO USE

### To start monitoring

    CrossGeofences.Current.StartMonitoring(new GeofenceRegion( 
        "My House", // identifier - must be unique per registered geofence
        Center = new Position(LATITUDE, LONGITUDE), // center point    
        Distance.FromKilometers(1) // radius of fence
    ));

### Wire up to notifications

    CrossGeofences.Current.RegionStatusChanged += (sender, args) => 
    {
        args.State // entered or exited
        args.Region // Identifier & details
    };

### Stop monitoring a region
    
    CrossGeofences.Current.StopMonitoring(GeofenceRegion);

    or

    CrossGeofences.Current.StopAllMonitoring();

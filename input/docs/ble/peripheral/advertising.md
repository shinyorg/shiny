# Advertising

Advertising is severally downplayed by both operating systems.  On iOS, you are limited to advertising the local name and service UUIDs

* On iOS, you can only advertise a custom local name and custom service UUIDs
* On Android, things are different
    * You cannot control the device naming
    * TX Power (high, medium, low, balanced)
    * Service Data
    * Service UUIDs
    * Specific Manufacturer Data

For now, I have chosen to support only the same feature band as iOS


```csharp

CrossBleAdapter.Current.Advertiser.Start(new AdvertisementData
{
    LocalName = "TestServer",
    ServiceUUIDs = new Guid[] { /* your custom UUIDs here */ }
});

CrossBleAdapter.Current.Advertiser.Stop();
```
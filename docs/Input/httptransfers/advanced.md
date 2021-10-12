Title: Advanced
---
# Advanced Usage

Watching the transfer with metrics
```csharp

public class ViewModel
{
    readonly IHttpTransferManager transfers;


    public ViewModel(IHttpTransferManager httpTransfers)
    {
        transfers
            .WhenUpdated()
            .WithMetrics() // this will calculate estimated time remaining and transfer speed
    }
}
```
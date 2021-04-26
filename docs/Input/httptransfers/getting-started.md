Title: Getting Started
Order: 1
---

As developers, we often take internet connectivity and speed for granted.  Mobile phones are taking HUGE sized photos these days that can take quite some time to upload.

## Features

* Background Uploads & Downloads on each platform
* Supports transfer filtering based on metered connections (iOS & UWP only at the moment)
* Event Based Metrics
  * Percentage Complete
  * Total Bytes Expected
  * Total Bytes Transferred
  * Transfer Speed (Bytes Per Second)
  * Estimated Completion Time

<?! PackageInfo "Shiny.Net.Http" /?>

## Setup

1. Create a delegate in your shared code

```csharp
using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.Net.Http;

namespace YourNamespace
{
    public class MyHttpTransferDelegate : IHttpTransferDelegate
    {

        public Task OnError(HttpTransfer transfer, Exception ex) 
        {
        }

        public async Task OnCompleted(HttpTransfer transfer)
        {
        }
    }
}

```

2. Add the following to your [Shiny Startup](xref:startup)

<?! Startup ?>
services.UseHttpTransfers<YourNamespace.MyHttpTransferDelegate>();
<?!/ Startup ?>

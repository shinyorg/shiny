Title: Delegate
---
# Delegate


## Shiny.Net.Http.IHttpTransferDelegate

### Task OnError(HttpTransfer transfer, Exception exception)
Backends do die, authentication runs out, whatever the case may be - this is where your uploads & downloads go to die


### Task OnCompleted(HttpTransfers transfer)
This is called when your uploads and downloads are done.  This is a great time to send a notification to let your user know you've finished something!
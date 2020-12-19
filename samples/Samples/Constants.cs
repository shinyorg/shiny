using System;


namespace Samples
{
    static class Constants
    {
        public const string EstimoteUuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
#if DEBUG
        public const string AppCenterTokens = "";
        public const string PhotoSyncUploadUri = "https://blah.com";
        public const string AnhListenerConnectionString = "Endpoint=sb://shinysamples.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=jI6ss5WOD//xPNuHFJmS7sWWzqndYQyz7wAVOMTZoLE=";
        public const string AnhHubName = "shinysamples";
#else
        public const string AppCenterTokens = "#{AppCenterTokens}#";
        public const string AnhListenerConnectionString = "#{AnhListenerConnectionString}#";
        public const string PhotoSyncUploadUri = "#{PhotoSyncUploadUri}#";
        public const string AnhHubName = "#{AnhHubName}#";
#endif
    }
}

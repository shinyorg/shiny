using System;


namespace Samples
{
    static class Constants
    {
        public const string EstimoteUuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
#if DEBUG
        public const string AppCenterToken = "android=485115cd-ad77-4a43-8d70-7244b560cdbe;ios=a3dc0119-e48d-45b9-9dda-1e3d954d2c4f";
        public const string AnhListenerConnectionString = "Endpoint=sb://shinysamples.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=jI6ss5WOD//xPNuHFJmS7sWWzqndYQyz7wAVOMTZoLE=";
        public const string AnhHubName = "shinysamples";
#else
        public const string AppCenterToken = "#{AppCenterToken}#";
        public const string AnhListenerConnectionString = "#{AnhListenerConnectionString}#";
        public const string AnhHubName = "#{AnhHubName}#";
#endif
    }
}

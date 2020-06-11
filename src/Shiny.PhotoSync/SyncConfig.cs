using System;


namespace Shiny.PhotoSync
{
    public class SyncConfig
    {
        public SyncConfig(string uploadToUri)
        {
            if (uploadToUri.IsEmpty())
                throw new ArgumentException("Value not set", nameof(uploadToUri));

            if (!Uri.TryCreate(uploadToUri, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid URI");

            this.UploadToUri = uploadToUri;
        }


        public string? UploadToUri { get; set; }
        public bool ShowBadgeCount { get; set; }
        public bool NotifyOnStart { get; set; }
        public bool NotifyOnComplete { get; set; }
    }
}

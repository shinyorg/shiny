using Samples.Models;
using System;


namespace Samples.LocationSync
{
    public class ItemViewModel
    {
        readonly LocationSyncEvent location;
        public ItemViewModel(LocationSyncEvent e) => this.location = e;

        
        public string Description => this.location.Description;
        public string DateCreated => this.location.DateCreated.ToLocalTime().ToShortDateString();
        public string Details => this.location.DateSync == null
            ? $"Pending - {this.location.DateLastAttempt.ToShortDateString()} - Retries {this.location.Retries}"
            : $"Synchronized: {this.location.DateSync.Value.ToLocalTime()}";
    }
}

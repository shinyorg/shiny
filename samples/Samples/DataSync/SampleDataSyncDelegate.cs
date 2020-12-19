using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.DataSync;
using System;
using System.Threading.Tasks;


namespace Samples.DataSync
{
    public class SampleDataSyncDelegate : ReactiveObject, IDataSyncDelegate
    {
        readonly Shiny.IMessageBus messageBus;


        public SampleDataSyncDelegate(Shiny.IMessageBus messageBus)
        {
            this.messageBus = messageBus;
        }


        /// <summary>
        /// This is used to show failures in the your sync process
        /// </summary>
        [Reactive] public bool AllowOutgoing { get; set; } = true;


        public async Task Push(SyncItem item)
        {
            await Task.Delay(1000); // simulate a delay to an http call
            if (!this.AllowOutgoing)
                throw new ArgumentException("Testing sync failure");

            this.messageBus.Publish(item);
        }
    }
}

using System;
using Shiny.Support.Uwp;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    class UwpBridgeImpl : IUwpBridge
    {
        readonly UwpContext context;
        public UwpBridgeImpl(UwpContext context)
            => this.context = context;

        public void Bridge(IBackgroundTaskInstance taskInstance) => this.context.Bridge(taskInstance);
    }
}

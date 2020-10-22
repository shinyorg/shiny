using System;


namespace Shiny
{
    public interface IAppDelegateBackgroundUrlHandler
    {
        void HandleEventsForBackgroundUrl(string sessionIdentifier, Action completionHandler);
    }
}

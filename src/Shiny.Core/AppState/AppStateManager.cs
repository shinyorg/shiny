using System;
using System.Collections.Generic;
using Shiny.Logging;


namespace Shiny.AppState
{
    class AppStateManager : IShinyStartupTask
    {
        readonly IEnumerable<IAppStateDelegate> delegates;
        public AppStateManager(IMessageBus messageBus, IEnumerable<IAppStateDelegate> delegates)
        {
            this.delegates = delegates;
            messageBus
                .Listener<AppEvent>()
                .Subscribe(x =>
                {
                    this.Execute(x => x.OnForeground());
                    this.Execute(x => x.OnBackground());
                    this.Execute(x => x.OnTerminate());
                });
        }


        public void Start() => this.Execute(x => x.OnStart());


        void Execute(Action<IAppStateDelegate> execute) => Log.SafeExecute(() =>
        {
            foreach (var del in this.delegates)
                execute(del);
        });
    }
}

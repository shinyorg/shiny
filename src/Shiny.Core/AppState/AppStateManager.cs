using System;
using System.Collections.Generic;
using Shiny.Logging;


namespace Shiny.AppState
{
    class AppStateManager : IShinyStartupTask
    {
        readonly IEnumerable<IAppStateDelegate> delegates;
        public AppStateManager(IEnumerable<IAppStateDelegate> delegates)
            => this.delegates = delegates;


        //bool IsAppInForeground { get; }
        public void Start() => this.Execute(x => x.OnStart());
        internal void OnForeground() => this.Execute(x => x.OnForeground());
        internal void OnBackground() => this.Execute(x => x.OnBackground());

        void Execute(Action<IAppStateDelegate> execute) => Log.SafeExecute(() =>
        {
            foreach (var del in this.delegates)
                execute(del);
        });
    }
}

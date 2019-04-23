using System;


namespace Shiny.Logging
{
    class ActionLogger : ILogger
    {
        readonly Action<Exception, (string Key, string Value)[]> onError;
        readonly Action<string, string, (string Key, string Value)[]> onEvent;

        public ActionLogger(Action<Exception, (string Key, string Value)[]> onError,
                            Action<string, string, (string Key, string Value)[]> onEvent)
        {
            this.onError = onError;
            this.onEvent = onEvent;
        }


        public void Write(Exception exception, params (string Key, string Value)[] parameters) => this.onError(exception, parameters);
        public void Write(string eventName, string description, params (string Key, string Value)[] parameters) => this.onEvent(eventName, description, parameters);
    }
}

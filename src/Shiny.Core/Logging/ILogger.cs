using System;


namespace Shiny.Logging
{
    public interface ILogger
    {
        void Write(Exception exception, params (string Key, string Value)[] parameters);
        void Write(string eventName, string description, params (string Key, string Value)[] parameters);
    }
}

using System;
using System.Diagnostics;


namespace Shiny.Logging
{
    public class DebugLogger : ILogger
    {
        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => WriteMsg($"[EXCEPTION] {exception}", parameters);


        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
            => WriteMsg($"[{eventName}] {description}", parameters);


        static void WriteMsg(string msg, params (string Key, string Value)[] parameters)
        {
            Debug.WriteLine(msg);
            foreach (var pair in parameters)
                Debug.WriteLine($"\t{pair.Key} - {pair.Value}");
        }
    }
}

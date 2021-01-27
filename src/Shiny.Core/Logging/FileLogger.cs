using System;
using System.IO;


namespace Shiny.Logging
{
    public class FileLogger : ILogger
    {
        public string LogFileName { get; set; }
        readonly object syncLock = new object();


        public FileLogger(string? logFileName = null) => this.LogFileName = logFileName ?? "shiny.log";


        public void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            lock (this.syncLock)
                File.AppendAllText(this.LogFileName, $"[{DateTime.Now.ToString("MM/d/yyyy hh:mm:ss tt")}] {exception}");
        }


        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
            lock (this.syncLock)
                File.AppendAllText(this.LogFileName, $"[{DateTime.Now.ToString("MM/d/yyyy hh:mm:ss tt")}] {eventName} - {description}");
        }
    }
}

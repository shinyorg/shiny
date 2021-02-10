using System;
using System.IO;


namespace Shiny.Logging
{
    public class FileLogger : ILogger
    {
        public string? LogFileName { get; private set; }
        readonly object syncLock = new object();

        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => this.Write(exception.ToString());

        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
            => this.Write($"{eventName} - {description}");


        void Write(string value)
        {
            lock (this.syncLock)
            {
                this.LogFileName ??= Path.Combine(ShinyHost.Resolve<IPlatform>().AppData.FullName, "shiny.log");
                File.AppendAllText(this.LogFileName, $"[{DateTime.Now.ToString("MM/d/yyyy hh:mm:ss tt")}] {value}");
            }
        }
    }
}

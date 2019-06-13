using System;
using System.Collections.Generic;


namespace Shiny.Logging
{
    public struct LogError
    {
        public LogError(Exception exception, IEnumerable<(string, string)> parameters)
        {
            this.Exception = exception;
            this.Parameters = parameters.ToDictionary();
        }


        public Exception Exception { get; }
        public IDictionary<string, string> Parameters { get; }
    }
}

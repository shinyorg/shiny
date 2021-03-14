using System;


namespace Shiny.Integrations.Sqlite.Logging
{
    public class NullScope : IDisposable
    {
        public static IDisposable Instance { get; } = new NullScope();
        public void Dispose() { }
    }
}

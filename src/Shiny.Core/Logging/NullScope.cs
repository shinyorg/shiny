using System;


namespace Shiny.Logging
{
    public class NullScope : IDisposable
    {
        public static NullScope Instance {  get; } = new NullScope();
        public void Dispose() { }
    }
}

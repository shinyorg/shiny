using System;

namespace Shiny.Logging.Firebase;


public class NullScope : IDisposable
{
    public static IDisposable Instance { get; } = new NullScope();
    public void Dispose() { }
}

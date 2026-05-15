namespace Shiny;

internal sealed class ActionDisposable(Action action) : IDisposable
{
    public void Dispose() => action();
}

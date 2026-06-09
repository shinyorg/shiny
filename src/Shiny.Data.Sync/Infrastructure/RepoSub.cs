using System;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// IDisposable wrapper that calls the supplied unsubscribe action on dispose.
/// Mirrors Shiny.Net.Http's helper of the same name.
/// </summary>
internal sealed class RepoSub(Action unsubscribe) : IDisposable
{
    bool disposed;

    public void Dispose()
    {
        if (this.disposed)
            return;
        this.disposed = true;
        unsubscribe();
    }
}

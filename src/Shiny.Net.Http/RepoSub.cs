using System;

namespace Shiny.Net.Http;

/// <summary>
/// Lightweight IDisposable that runs an unsubscribe action on disposal.
/// Used to clean up repository ActionOccurred event handlers.
/// </summary>
internal sealed class RepoSub(Action unsubscribe) : IDisposable
{
    public void Dispose() => unsubscribe();
}

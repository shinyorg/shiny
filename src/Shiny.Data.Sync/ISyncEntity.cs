namespace Shiny.Data.Sync;


/// <summary>
/// Contract for any type that can be sync'd through <see cref="IDataSyncManager"/>.
/// </summary>
public interface ISyncEntity
{
    /// <summary>
    /// The stable, server-recognized identifier for this entity.
    /// </summary>
    string Identifier { get; }
}

namespace Shiny.Support.Repositories;


/// <summary>
/// Represents an entity that can be stored in a Shiny repository
/// </summary>
public interface IRepositoryEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity
    /// </summary>
    string Identifier { get; }
}

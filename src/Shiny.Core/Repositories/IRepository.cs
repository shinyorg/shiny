using System;
using System.Collections.Generic;

namespace Shiny.Support.Repositories;


/// <summary>
/// Describes the kind of mutation that occurred in an <see cref="IRepository"/>.
/// </summary>
public enum RepositoryAction
{
    /// <summary>An entity was removed.</summary>
    Remove,
    /// <summary>A new entity was added.</summary>
    Add,
    /// <summary>An existing entity was updated.</summary>
    Update,
    /// <summary>All entities of a given type were cleared.</summary>
    Clear
}


/// <summary>
/// Stores and retrieves entities keyed by string identifier, partitioned by entity type.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Returns true if an entity of the given type with the specified identifier exists in storage.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query.</typeparam>
    /// <param name="identifier">The unique identifier of the entity.</param>
    bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Gets the entity with the specified identifier, or null if it does not exist.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to retrieve.</typeparam>
    /// <param name="identifier">The unique identifier of the entity.</param>
    TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Gets all entities of the specified type from storage.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to retrieve.</typeparam>
    IReadOnlyList<TEntity> GetAll<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Inserts or updates the entity. Returns true if an existing entity was updated, false if a new entity was inserted.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to store.</typeparam>
    /// <param name="entity">The entity to store.</param>
    bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes the entity with the specified identifier. Returns true if the entity existed and was removed.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to remove.</typeparam>
    /// <param name="identifier">The unique identifier of the entity.</param>
    bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes all entities of the specified type from storage.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to clear.</typeparam>
    void Clear<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Inserts a new entity. Throws <see cref="RepositoryException"/> if the identifier already exists.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to insert.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <exception cref="RepositoryException">Thrown when an entity with the same identifier already exists.</exception>
    void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Updates an existing entity. Throws <see cref="RepositoryException"/> if no entity with the given identifier exists.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to update.</typeparam>
    /// <param name="entity">The entity to update.</param>
    /// <exception cref="RepositoryException">Thrown when no entity with the given identifier exists.</exception>
    void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Fires after any add, update, remove, or clear operation completes against the repository.
    /// </summary>
    event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> ActionOccurred;
}

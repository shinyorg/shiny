using System;
using System.Collections.Generic;

namespace Shiny.Support.Repositories;


/// <summary>
/// Identifies the kind of change emitted by an <see cref="IRepository"/>.
/// </summary>
public enum RepositoryAction
{
    /// <summary>An entity was removed.</summary>
    Remove,
    /// <summary>An entity was inserted.</summary>
    Add,
    /// <summary>An existing entity was updated.</summary>
    Update,
    /// <summary>All entities of a type were cleared.</summary>
    Clear
}


/// <summary>
/// Typed key/value repository that persists <see cref="IRepositoryEntity"/> instances
/// and raises change notifications.
/// </summary>
public interface IRepository
{
    /// <summary>
    /// Returns true if an entity of the given type and identifier exists in storage.
    /// </summary>
    bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns the entity with the given identifier, or null if not found.
    /// </summary>
    TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns all entities of the given type currently in storage.
    /// </summary>
    IReadOnlyList<TEntity> GetAll<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Inserts or updates an entity. Returns true if an existing record was updated, false if a new one was added.
    /// </summary>
    bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes the entity with the given identifier. Returns true if a record was removed.
    /// </summary>
    bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes all entities of the given type from storage.
    /// </summary>
    void Clear<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Inserts a new entity. Throws if an entity with the same identifier already exists.
    /// </summary>
    void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Updates an existing entity. Throws if no entity with the identifier exists.
    /// </summary>
    void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Raised after every successful repository mutation (insert, update, remove, clear).
    /// </summary>
    event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> ActionOccurred;
}

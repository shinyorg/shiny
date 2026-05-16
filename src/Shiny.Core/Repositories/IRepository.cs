using System;
using System.Collections.Generic;

namespace Shiny.Support.Repositories;


public enum RepositoryAction
{
    Remove,
    Add,
    Update,
    Clear
}


public interface IRepository
{
    /// <summary>
    /// Returns true if a given identifier/type value is found in storage
    /// </summary>
    bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns a specific entity by its key value
    /// </summary>
    TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Gets all entities from storage of a given type
    /// </summary>
    IReadOnlyList<TEntity> GetAll<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns true if updating otherwise false if new
    /// </summary>
    bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes an entity from storage - if the entity exists, returns true, otherwise false
    /// </summary>
    bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Clears all entities of a given type
    /// </summary>
    void Clear<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Will save entity by its identifier, if the identifier already exists, an error is thrown
    /// </summary>
    void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Will save entity by its identifier, if the identifier does not exist, an error is thrown
    /// </summary>
    void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Fires when an action occurs within the repository
    /// </summary>
    event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> ActionOccurred;
}

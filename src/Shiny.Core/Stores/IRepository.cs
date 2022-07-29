using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shiny.Stores;


public enum RepositoryAction
{
    Remove,
    Add,
    Update,
    Clear
}


public interface IRepository<TEntity> where TEntity : IStoreEntity
{
    /// <summary>
    /// Returns true if a given key/type value is found in storage
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> Exists(string key);

    /// <summary>
    /// Returns a specific entity by its key value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<TEntity?> Get(string key);

    /// <summary>
    /// Gets all entities from storage of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<IList<TEntity>> GetList(Expression<Func<TEntity, bool>>? expression = null);

    /// <summary>
    /// Returns true if updating otherwise false if new
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> Set(TEntity entity);

    /// <summary>
    /// Removes an entity from storage - if the entity exists, returns true, otherwise false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> Remove(string key);

    /// <summary>
    /// Clears all entities of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task Clear();


    /// <summary>
    /// An observable for watching events within the repository
    /// </summary>
    /// <returns></returns>
    IObservable<(RepositoryAction Action, TEntity? Entity)> WhenActionOccurs();
}

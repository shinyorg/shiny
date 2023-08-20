using System.Linq.Expressions;

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
    // TODO: update transaction void UpdateTransaction<T>(string identifier, Action<T> updateFunc)
    // TODO: update/clear all UpdateAll<T>

    /// <summary>
    /// Returns true if a given identifier/type value is found in storage
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns></returns>
    bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns a specific entity by its key value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="identifier"></param>
    /// <returns></returns>
    TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Gets all entities from storage of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    IList<TEntity> GetList<TEntity>(Expression<Func<TEntity, bool>>? expression = null) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Returns true if updating otherwise false if new
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Removes an entity from storage - if the entity exists, returns true, otherwise false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="identifier"></param>
    /// <returns></returns>
    bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Clears all entities of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    void Clear<TEntity>() where TEntity : IRepositoryEntity;

    /// <summary>
    /// Will save entity by its identifier, if the identifier already exists, an error is thrown
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// Will save entity by its identifier, if the identifier does not exist, an error is thrown
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;

    /// <summary>
    /// An observable for watching events within the repository
    /// </summary>
    /// <returns></returns>
    IObservable<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> WhenActionOccurs();
}
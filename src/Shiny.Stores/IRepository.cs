using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shiny.Stores;


public interface IRepository
{
    /// <summary>
    /// Returns true if a given key/type value is found in storage
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> Exists<T>(string key) where T : class;

    /// <summary>
    /// Returns a specific entity by its key value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<T?> Get<T>(string key) where T : class;

    /// <summary>
    /// Gets all entities from storage of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<IDictionary<string, T>> GetListWithKeys<T>(Expression<Func<T, bool>>? expression = null) where T : class;

    /// <summary>
    /// Gets all entities from storage of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    Task<IList<T>> GetList<T>(Expression<Func<T, bool>>? expression = null) where T : class;

    /// <summary>
    /// Returns true if updating otherwise false if new
    /// </summary>
    /// <param name="key"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<bool> Set(string key, object entity);

    /// <summary>
    /// Removes an entity from storage - if the entity exists, returns true, otherwise false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> Remove<T>(string key) where T : class;

    /// <summary>
    /// Clears all entities of a given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task Clear<T>() where T : class;
}

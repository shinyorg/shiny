using System;


namespace Shiny.Infrastructure
{
    public interface IStore
    {
        void Set(string key, object value);
        object Get(Type objectType, string key);
        bool Contains(string key);
    }
}
//    public interface IEntity
//    {
//        public string Id { get; set; }
//    }

//    //public enum StoreEventType
//    //{
//    //    Add,
//    //    Update,
//    //    Remove
//    //}


//    //public class StoreEvent
//    //{
//    //    public IEntity Entity { get; }
//    //    public StoreEventType Type { get; }
//    //}

//    public interface IStore<T> where T : class, IEntity, new()
//    {
//        Task<T?> Get(string id);
//        Task<bool> Exists(string id);
//        Task<IList<T>> GetAll();
//        Task Set(T entity);
//        Task<bool> Remove(string id);
//        Task Clear();
//    }
//}

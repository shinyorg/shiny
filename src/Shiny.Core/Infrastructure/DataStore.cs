//using System;
//using System.Threading.Tasks;

//namespace Shiny.Infrastructure
//{
//    public enum DataStoreEvent
//    {
//        Add,
//        Update,
//        Delete
//    }


//    public interface IDataStore
//    {
//        void Map<TMain, TPersist>(Func<TMain, TPersist> transfer, Func<TPersist, TMain> outgoing);
//        IObservable<(TIn tin, TOut tout, DataStoreEvent e)> WhenReceived<TIn, TOut>();
//        Task Save<T>(T entity);
//        Task Delete<T>(T entity);
//    }


//    public class DataStore : IDataStore
//    {
//        public DataStore()
//        {
//        }

//        public Task Delete<T>(T entity)
//        {
//            throw new NotImplementedException();
//        }

//        public void Map<TMain, TPersist>(Func<TMain, TPersist> transfer, Func<TPersist, TMain> outgoing)
//        {
//            throw new NotImplementedException();
//        }

//        public Task Save<T>(T entity)
//        {
//            throw new NotImplementedException();
//        }

//        public IObservable<(TIn tin, TOut tout, DataStoreEvent e)> WhenReceived<TIn, TOut>()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}

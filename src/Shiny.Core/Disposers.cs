//using System;
//using System.Threading.Tasks;


//namespace Shiny
//{
//    public class AsyncScope : IAsyncDisposable
//    {
//        Func<ValueTask> dispose;
//        public AsyncScope(Func<ValueTask> dispose) => this.dispose = dispose;
//        public ValueTask DisposeAsync() => this.dispose.Invoke();
//    }


//    public static class Disposers
//    {
//        public static AsyncScope CreateAsync(Func<ValueTask> dispose) => new AsyncScope(dispose);
//    }
//}

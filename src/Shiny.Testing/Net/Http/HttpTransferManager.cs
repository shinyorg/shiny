//using System;
//using System.Collections.Generic;
//using System.Reactive.Subjects;
//using System.Threading.Tasks;
//using Shiny.Net.Http;


//namespace Shiny.Testing.Net.Http
//{
//    public class UploadManager : IHttpTransferManager
//    {
//        public Task Cancel(string id) => Task.CompletedTask;
//        public Task Cancel(QueryFilter filter = null) => Task.CompletedTask;
//        public Task<HttpTransfer> Enqueue(HttpTransferRequest request) => Task.FromResult(default(HttpTransfer));
//        public Task<HttpTransfer> GetTransfer(string id) => Task.FromResult(default(HttpTransfer));

//        public Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter filter = null)
//        {
//            throw new NotImplementedException();
//        }


//        public Subject<HttpTransfer> UpdatedSubject { get; } = new Subject<HttpTransfer>();
//        public IObservable<HttpTransfer> WhenUpdated() => this.UpdatedSubject;
//    }
//}

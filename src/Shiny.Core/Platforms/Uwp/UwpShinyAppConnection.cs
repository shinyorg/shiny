//using System;
//using System.Linq;
//using System.Reactive;
//using System.Reactive.Linq;
//using Shiny.Infrastructure;
//using Windows.ApplicationModel.AppService;

//namespace Shiny
//{
//    public class UwpShinyAppConnection : IShinyStartupTask
//    {
//        const string AppServiceName = nameof(UwpShinyAppConnection);

//        readonly ISerializer serializer;
//        AppServiceConnection connection;

//        public UwpShinyAppConnection(ISerializer serializer) => this.serializer = serializer;


//        public IObservable<T> Listen<T>(string eventName) => Observable.Create<T>(ob =>
//        {
//            return () => { };
//        });


//        public IObservable<Unit> Send(string eventName, object obj)
//        {
//            //await _connection.SendMessageAsync("Echo", Message.Text);
//        }


//        //private void ConnectionOnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
//        //{
//        //    var appServiceDeferral = args.GetDeferral();
//        //    try
//        //    {
//        //        ValueSet valueSet = args.Request.Message;
//        //        OnMessageReceived?.Invoke(valueSet);
//        //    }
//        //    finally
//        //    {
//        //        appServiceDeferral.Complete();
//        //    }
//        //}ageAsync(new KeyValuePair<string, object>(key, value));
//        //}


//        public async void Start()
//        {
//            var listing = await AppServiceCatalog.FindAppServiceProvidersAsync(AppServiceName);
//            var packageName = listing.FirstOrDefault()?.PackageFamilyName;

//            this.connection = new AppServiceConnection
//            {
//                AppServiceName = AppServiceName,
//                PackageFamilyName = packageName
//            };

//            //connection.RequestReceived += null;
//            //connection.ServiceClosed += null;
//            //var status = await connection.OpenAsync();
//            //if (status != AppServiceConnectionStatus.Success)
//            //    throw new Exception("");
//        }


//        void Close()
//        {
//            //this.connection.RequestReceived -= this.OnRequestReceived;
//            //this.connection.ServiceClosed -= this.OnServiceClosed;
//            this.connection.Dispose();
//            this.connection = null;
//        }

//    }
//}
using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;


namespace Shiny
{
    public static class Utils
    {
        public static async Task<JSObjectReference> Import(this IJSRuntime jsRuntime, string moduleName, string fileName)
        {
            var module = await jsRuntime.InvokeAsync<JSObjectReference>(
                "import", $"./_content/{moduleName}/{fileName}.js"
            );
            return module;
        }


        public static AccessState ToAccessState(string value) => value switch
        {
            "granted" => AccessState.Available,
            "denied" => AccessState.Denied,
            "notsupported" => AccessState.NotSupported,
            _ => AccessState.Unknown
        };


        //public static async Task<JsObjectReferenceDynamic> Import<T>(this IJSRuntime jsRuntime, string fileName)
        //{
        //    var libraryName = typeof(T).Assembly.GetName().Name;
        //    var module = await jsRuntime.InvokeAsync<JSObjectReference>(
        //        "import", $"./_content/{libraryName}/{fileName}.js")
        //    );
        //    return new JsObjectReferenceDynamic(module);
        //}

        //        public IObservable<IJSObjectReference> Import(this IJSRuntime js, string module, string fileName)
        //        {

        //        }

        // TODO: object binder to editcontext
    }
}

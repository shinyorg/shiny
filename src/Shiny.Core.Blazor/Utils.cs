using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Implementation;

namespace Shiny;


public static class Utils
{
    public static async Task<IJSObjectReference> Import(this IJSRuntime jsRuntime, string moduleName, string fileName)
    {
        //var uri = $"./_content/{moduleName}/{fileName}";
        var uri = $"./_content/{moduleName}/{fileName}";
        var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", uri);
        return module;
    }


    public static async Task<IJSInProcessObjectReference> ImportInProcess(this IJSRuntime jsRuntime, string moduleName, string fileName)
    {
        var inproc = jsRuntime as IJSInProcessRuntime;
        if (inproc == null)
            throw new InvalidOperationException("JS Runtime is not running in-proc");

        var import = await inproc.Import(moduleName, fileName);
        return (IJSInProcessObjectReference)import;
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

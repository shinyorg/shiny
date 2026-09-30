using Android.App.AppFunctions;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidAppFunctionException = Android.App.AppFunctions.AppFunctionException;

namespace Shiny.AppFunctions;

/// <summary>
/// The platform AppFunctionService (Android 16+). Declared in this library's AndroidManifest.xml; the system binds
/// it after Application.OnCreate, so the Shiny host is normally ready already.
/// </summary>
[Register("shiny.appfunctions.ShinyAppFunctionService")]
[System.Runtime.Versioning.SupportedOSPlatform("android36.0")]
public class ShinyAppFunctionService : AppFunctionService
{
    /// <summary>Response extras key holding <see cref="AppFunctionContext.Dialog"/>, when the handler set one.</summary>
    public const string DialogExtraKey = "shiny.appfunctions.dialog";

    public override void OnExecuteFunction(
        ExecuteAppFunctionRequest request,
        string callingPackage,
        SigningInfo callingPackageSigningInfo,
        CancellationSignal cancellationSignal,
        IOutcomeReceiver callback)
    {
        var cts = new CancellationTokenSource();
        cancellationSignal.SetOnCancelListener(new CancelListener(cts));
        var functionId = request.FunctionIdentifier;
        var parameters = request.Parameters;

        _ = Task.Run(async () =>
        {
            try
            {
                var dispatcher = await AppFunctionsHost.WaitForDispatcher(cts.Token).ConfigureAwait(false);
                var function = dispatcher.Registry.Functions.FirstOrDefault(x => x.Id == functionId);
                if (function == null)
                {
                    callback.OnError(ToJava(AppFunctionErrorCode.NotFound, $"Unknown function '{functionId}'", notFoundIsFunction: true));
                    return;
                }

                var json = GenericDocumentJson.ToJson(parameters, function);
                var invocation = new AppFunctionInvocation(functionId, AppFunctionPlatform.Android, AppVisibility.IsVisible, callingPackage);
                var outcome = await dispatcher.Execute(invocation, json, cts.Token).ConfigureAwait(false);

                if (outcome.Status == AppFunctionStatus.Success)
                {
                    var document = GenericDocumentJson.ToResultDocument(outcome.ResultJson, function.Result);
                    Bundle? extras = null;
                    if (outcome.Dialog != null)
                    {
                        extras = new Bundle();
                        extras.PutString(DialogExtraKey, outcome.Dialog);
                    }
                    callback.OnResult(extras == null ? new ExecuteAppFunctionResponse(document) : new ExecuteAppFunctionResponse(document, extras));
                }
                else
                {
                    callback.OnError(ToJava(outcome.ErrorCode ?? AppFunctionErrorCode.Denied, outcome.Message ?? "Failed"));
                }
            }
            catch (AppFunctionException ex)
            {
                callback.OnError(ToJava(ex.Code, ex.Message));
            }
            catch (Exception ex)
            {
                callback.OnError(ToJava(AppFunctionErrorCode.AppError, ex.Message));
            }
            finally
            {
                cts.Dispose();
            }
        });
    }

    static Java.Lang.Object ToJava(AppFunctionErrorCode code, string message, bool notFoundIsFunction = false)
    {
        var error = code switch
        {
            AppFunctionErrorCode.InvalidArgument => AppFunctionError.InvalidArgument,
            // an entity id that does not exist is a bad argument; only an unknown function id is "function not found"
            AppFunctionErrorCode.NotFound => notFoundIsFunction ? AppFunctionError.FunctionNotFound : AppFunctionError.InvalidArgument,
            AppFunctionErrorCode.Denied => AppFunctionError.Denied,
            AppFunctionErrorCode.Cancelled => AppFunctionError.Cancelled,
            _ => AppFunctionError.AppUnknownError
        };
        // IOutcomeReceiver is erased to Java.Lang.Object, and a Java Throwable binds as a .NET Exception
        return new AndroidAppFunctionException(error, message).JavaCast<Java.Lang.Object>();
    }

    sealed class CancelListener(CancellationTokenSource cts) : Java.Lang.Object, CancellationSignal.IOnCancelListener
    {
        public void OnCancel()
        {
            try { cts.Cancel(); } catch (ObjectDisposedException) { }
        }
    }
}

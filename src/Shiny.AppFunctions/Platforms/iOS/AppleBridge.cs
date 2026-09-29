using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Shiny.AppFunctions.Platforms.iOS;

/// <summary>
/// C ABI between the generated Swift App Intents and the dispatcher (swift/ShinyAppFunctions.swift is the other half).
/// Swift calls the registered function pointer; C# answers through <c>shiny_af_complete</c>, which resumes the
/// Swift continuation. A function pointer (rather than an exported symbol) works the same on Mono and NativeAOT.
/// </summary>
static class AppleBridge
{
    const int FlagForeground = 1;

    // status codes understood by ShinyAppFunctions.swift
    const int StatusSuccess = 0;
    const int StatusError = 1;
    const int StatusNeedsForeground = 2;

    [DllImport("__Internal")]
    static extern unsafe void shiny_af_set_handler(delegate* unmanaged<IntPtr, IntPtr, int, IntPtr, void> handler);

    [DllImport("__Internal")]
    static extern unsafe void shiny_af_complete(IntPtr context, int status, byte* json);

    static int registered;

    public static unsafe void Register()
    {
        if (Interlocked.Exchange(ref registered, 1) == 0)
            shiny_af_set_handler(&OnInvoke);
    }

    [UnmanagedCallersOnly]
    static void OnInvoke(IntPtr functionId, IntPtr argumentsJson, int flags, IntPtr completion)
    {
        var id = Marshal.PtrToStringUTF8(functionId) ?? "";
        var json = Marshal.PtrToStringUTF8(argumentsJson) ?? "{}";
        var invocation = new AppFunctionInvocation(id, AppFunctionPlatform.Apple, (flags & FlagForeground) != 0);

        // never block the Swift caller's thread
        _ = Task.Run(async () =>
        {
            AppFunctionOutcome outcome;
            try
            {
                var dispatcher = await AppFunctionsHost.WaitForDispatcher().ConfigureAwait(false);
                outcome = await dispatcher.Execute(invocation, json, CancellationToken.None).ConfigureAwait(false);
            }
            catch (AppFunctionException ex)
            {
                outcome = AppFunctionOutcome.Failed(ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                outcome = AppFunctionOutcome.Failed(AppFunctionErrorCode.AppError, ex.Message);
            }
            Complete(completion, outcome);
        });
    }

    static unsafe void Complete(IntPtr completion, AppFunctionOutcome outcome)
    {
        var status = outcome.Status switch
        {
            AppFunctionStatus.Success => StatusSuccess,
            AppFunctionStatus.NeedsForeground => StatusNeedsForeground,
            _ => StatusError
        };
        var bytes = Encoding.UTF8.GetBytes(ToReply(outcome) + "\0");
        fixed (byte* p = bytes)
            shiny_af_complete(completion, status, p);
    }

    /// <summary>{"value": &lt;json&gt;, "dialog": "...", "code": "...", "message": "..."}</summary>
    internal static string ToReply(AppFunctionOutcome outcome)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (outcome.ResultJson != null)
            {
                writer.WritePropertyName("value");
                writer.WriteRawValue(outcome.ResultJson, skipInputValidation: true);
            }
            if (outcome.Dialog != null)
                writer.WriteString("dialog", outcome.Dialog);
            if (outcome.ErrorCode != null)
                writer.WriteString("code", outcome.ErrorCode.Value.ToString());
            if (outcome.Message != null)
                writer.WriteString("message", outcome.Message);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

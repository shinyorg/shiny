namespace Shiny.BluetoothLE.Hosting.SourceGenerators;


/// <summary>
/// Emits the generated half of an <c>[L2CapService]</c> class - listener lifetime, per-channel
/// dispatch, and the assigned PSM.
/// </summary>
static class L2CapEmitter
{
    public static string Emit(L2CapModel model)
    {
        var writer = CodeWriter.File();
        using (writer.Namespace(model.Namespace))
        {
            writer.GeneratedCode();
            using (writer.Block($"partial class {model.ClassName}"))
            {
                writer.Line("readonly global::System.Threading.CancellationTokenSource __l2capCts = new();");
                writer.Line($"{Names.L2CapInstance}? __l2capInstance;");
                writer.Line();

                writer.Line("/// <summary>The PSM assigned by the platform, or zero while the listener is closed.</summary>");
                writer.Line("public ushort Psm => this.__l2capInstance?.Psm ?? (ushort)0;");
                writer.Line();
                writer.Line("/// <summary>Whether the PSM is currently published.</summary>");
                writer.Line("public bool IsListening => this.__l2capInstance != null;");
                writer.Line();

                writer.Line("/// <summary>Called when the channel handler throws. Implement it in your half of the class to observe failures.</summary>");
                writer.Line($"partial void OnL2CapChannelError({Names.L2CapChannel} channel, {Names.Exception} exception);");
                writer.Line();

                writer.Line("/// <summary>Publishes the PSM and starts accepting centrals. Called by generated registration code.</summary>");
                using (writer.Block($"internal async global::System.Threading.Tasks.Task<{Names.L2CapInstance}> OpenL2Cap({Names.BleHostingManager} manager)"))
                {
                    writer.Line("var instance = await manager");
                    writer.Line($"    .OpenL2Cap({(model.Secure ? "true" : "false")}, channel => {{ _ = this.__ServeL2CapChannel(channel); }})");
                    writer.Line("    .ConfigureAwait(false);");
                    writer.Line();
                    writer.Line("this.__l2capInstance = instance;");
                    writer.Line("return instance;");
                }
                writer.Line();

                EmitServe(writer, model);

                writer.Line("/// <summary>Cancels in-flight channel handlers. Called by generated registration code.</summary>");
                using (writer.Block("internal void ShutdownL2Cap()"))
                {
                    writer.Line($"try {{ this.__l2capCts.Cancel(); }} catch ({Names.Exception}) {{ /* already disposed */ }}");
                    writer.Line("this.__l2capInstance = null;");
                }
            }
        }
        return writer.ToString();
    }


    static void EmitServe(CodeWriter writer, L2CapModel model)
    {
        var handler = model.Handler;
        using (writer.Block($"async global::System.Threading.Tasks.Task __ServeL2CapChannel({Names.L2CapChannel} channel)"))
        {
            writer.Line("var cancellationToken = this.__l2capCts.Token;");
            writer.Line($"var channelContext = new {Names.BleL2CapContext}(channel);");
            using (writer.Block("try"))
            {
                if (handler == null)
                    writer.Line("await global::System.Threading.Tasks.Task.CompletedTask.ConfigureAwait(false);");
                else if (handler.IsAwaitable)
                    writer.Line($"await {handler.Invocation}.ConfigureAwait(false);");
                else
                    writer.Line($"{handler.Invocation};");
            }
            using (writer.Block($"catch ({Names.OperationCanceledException})"))
            {
                writer.Line("// listener was shut down");
            }
            using (writer.Block($"catch ({Names.Exception} ex)"))
            {
                writer.Line("this.OnL2CapChannelError(channel, ex);");
            }
            using (writer.Block("finally"))
            {
                writer.Line("// the handler owns the conversation; once it returns the channel is done");
                writer.Line($"try {{ channel.Dispose(); }} catch ({Names.Exception}) {{ /* best effort */ }}");
            }
        }
        writer.Line();
    }
}

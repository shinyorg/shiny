using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Wearables;

namespace Sample.Shared.Maui.Delegates;

public class SampleWearableDelegate(
    ILogger<SampleWearableDelegate> logger,
    IEventStore events
) : WearableDelegate
{
    public const string Category = "Wearables";

    public override Task OnStatusChanged(WearableStatus status)
        => this.Log($"Status: paired={status.IsPaired}, app={status.IsAppInstalled}, reachable={status.IsReachable}");

    // the watch gets its own text back, so a round trip can be tested from the watch side too
    public override async Task<byte[]?> OnMessageReceived(WearableMessage message)
    {
        var text = message.GetString();
        await this.Log($"Message on '{message.Path}': {text}", message.NodeId);
        return message.ExpectsReply ? WearableData.FromString("echo: " + text) : null;
    }

    public override Task OnContextReceived(WearableContext context)
        => this.Log("Context: " + context.GetString(), context.NodeId);

    public override Task OnTransferReceived(WearableTransfer transfer)
        => this.Log($"Transfer on '{transfer.Path}': {transfer.GetString()}", transfer.NodeId);

    public override Task OnFileReceived(WearableFile file)
        => this.Log($"File '{file.FileName}' on '{file.Path}' -> {file.LocalPath}", file.NodeId);

    public override Task OnTransferCompleted(WearableTransferResult result)
        => this.Log(result.Succeeded
            ? $"Transfer {result.Id} ({result.Kind}) delivered"
            : $"Transfer {result.Id} ({result.Kind}) failed: {result.Error}"
        );

    Task Log(string description, string? nodeId = null)
    {
        logger.LogInformation("{Description}", description);
        return events.Add(Category, description, new Dictionary<string, string?> { ["NodeId"] = nodeId });
    }
}

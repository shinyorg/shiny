using System.Text.Json;
using Microsoft.Maui.DevFlow.Agent.Core;

namespace Shiny.DeviceTests.Framework;


/// <summary>
/// Exposes the test runner through the DevFlow agent, so the host runner (or `maui devflow extensions call`)
/// can list, start and poll runs and relay peer commands. Routes live under
/// /api/v1/ext/org.shiny.devicetests on the agent port.
/// </summary>
public static class DevFlowTestExtension
{
    public static void Register(AgentOptions options)
    {
        var ext = options.RegisterExtension(
            DeviceTestConstants.ExtensionNamespace,
            "Shiny end-to-end device tests",
            1,
            ["tests", "peer"]
        );

        ext.MapTool(
            "list",
            "List every registered device test",
            "GET",
            "/tests",
            _ => Task.FromResult(Json(Runner.GetTests(), DeviceTestJsonContext.Default.ListTestInfo))
        );

        ext.MapTool(
            "run",
            "Start a test run. Body: { filter?: 'Suite[.Test],...', hostUrl?: 'http://runner:5199' }",
            "POST",
            "/run",
            req =>
            {
                var request = Deserialize(req.Body, DeviceTestJsonContext.Default.TestRunRequest) ?? new TestRunRequest(null, null);
                try
                {
                    return Task.FromResult(Json(Runner.Start(request), DeviceTestJsonContext.Default.TestRunStarted));
                }
                catch (InvalidOperationException ex)
                {
                    return Task.FromResult(HttpResponse.Error(ex.Message, 409));
                }
            }
        );

        ext.MapTool(
            "status",
            "Current (or last) run status with every result so far",
            "GET",
            "/status",
            _ => Task.FromResult(Json(Runner.Status, DeviceTestJsonContext.Default.TestRunStatus))
        );

        ext.MapTool(
            "cancel",
            "Cancel the current run",
            "POST",
            "/cancel",
            _ =>
            {
                Runner.Cancel();
                return Task.FromResult(HttpResponse.Ok("cancelled"));
            }
        );

        ext.MapTool(
            "peer",
            "Execute a peer command on this device. Body: { command, args? }",
            "POST",
            "/peer",
            async req =>
            {
                var request = Deserialize(req.Body, DeviceTestJsonContext.Default.PeerCommandRequest);
                if (request == null)
                    return HttpResponse.Error("command is required", 400);

                var dispatcher = Services.GetRequiredService<PeerCommandDispatcher>();
                var response = await dispatcher.ExecuteAsync(request, CancellationToken.None).ConfigureAwait(false);
                return Json(response, DeviceTestJsonContext.Default.PeerCommandResponse);
            }
        );
    }


    static IServiceProvider Services => IPlatformApplication.Current?.Services
        ?? throw new InvalidOperationException("The app has not finished starting");

    static TestRunner Runner => Services.GetRequiredService<TestRunner>();


    static HttpResponse Json<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo) => new()
    {
        StatusCode = 200,
        ContentType = "application/json",
        Body = JsonSerializer.Serialize(value, typeInfo)
    };


    static T? Deserialize<T>(string? body, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
        => String.IsNullOrWhiteSpace(body) ? default : JsonSerializer.Deserialize(body, typeInfo);
}

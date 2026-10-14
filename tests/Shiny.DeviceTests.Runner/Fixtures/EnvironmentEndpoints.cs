namespace Shiny.DeviceTests.Runner.Fixtures;


/// <summary>
/// /env/* - what a test asks the host to do to its environment: report capabilities, move the simulated
/// location, deliver a push, or relay a command to the peer device.
/// </summary>
public static class EnvironmentEndpoints
{
    public static void MapEnvironment(this WebApplication app, RunnerState state)
    {
        app.MapGet("/env/info", () => Results.Json(
            new HostInfo(
                state.Target.Platform,
                state.Target.IsSimulator,
                state.Capabilities,
                state.Peer?.Device.Platform
            ),
            DeviceTestJsonContext.Default.HostInfo
        ));

        app.MapPost("/env/location", async (HttpRequest req, CancellationToken ct) =>
        {
            var body = await req.ReadFromJsonAsync(DeviceTestJsonContext.Default.SetLocationRequest, ct);
            if (body == null)
                return Results.BadRequest("latitude/longitude required");

            if (!state.Target.CanSetLocation)
                return Results.Problem($"Cannot simulate location on {state.Target}", statusCode: 501);

            Console.WriteLine($"  > location {body.Latitude}, {body.Longitude}");
            return await Run(() => state.Target.SetLocationAsync(body.Latitude, body.Longitude, ct));
        });

        app.MapPost("/env/push", async (HttpRequest req, CancellationToken ct) =>
        {
            var body = await req.ReadFromJsonAsync(DeviceTestJsonContext.Default.SendPushRequest, ct);
            if (body == null)
                return Results.BadRequest("payloadJson required");

            if (!state.Target.CanPush)
                return Results.Problem($"Cannot deliver a simulated push to {state.Target}", statusCode: 501);

            Console.WriteLine("  > push");
            return await Run(() => state.Target.SendPushAsync(body.PayloadJson, ct));
        });

        app.MapPost("/env/call", async (HttpRequest req, CancellationToken ct) =>
        {
            var body = await req.ReadFromJsonAsync(DeviceTestJsonContext.Default.PlaceCallRequest, ct);
            if (body == null)
                return Results.BadRequest("number required");

            if (!state.Target.CanPlaceCall)
                return Results.Problem($"Cannot place a call to {state.Target}", statusCode: 501);

            Console.WriteLine($"  > call from {body.Number}");
            var type = await state.Target.PlaceCallAsync(body.Number, body.RingSeconds, ct);
            return Results.Json(new PlaceCallResult(type), DeviceTestJsonContext.Default.PlaceCallResult);
        });

        app.MapPost("/env/peer", async (HttpRequest req, CancellationToken ct) =>
        {
            var body = await req.ReadFromJsonAsync(DeviceTestJsonContext.Default.PeerCommandRequest, ct);
            if (body == null)
                return Results.BadRequest("command required");

            if (state.Peer == null)
                return Results.Problem("No peer device attached (--peer)", statusCode: 501);

            Console.WriteLine($"  > peer {body.Command}");
            var response = await state.Peer.PeerAsync(body, ct);
            return Results.Json(response, DeviceTestJsonContext.Default.PeerCommandResponse);
        });
    }


    // the tool's own error (simctl/adb output) goes back to the test, so it can tell "not possible here" from a bug
    static async Task<IResult> Run(Func<Task> action)
    {
        try
        {
            await action();
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ! {ex.Message}");
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }
}

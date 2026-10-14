using Shiny.Locations;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Moves the simulator/emulator through the runner. Simulated fixes are not always re-delivered on their own (the
/// Android fused provider especially only reports while something is polling), so HoldAsync keeps re-sending the
/// position until whatever the test is waiting for completes.
/// </summary>
static class SimulatedLocation
{
    // downtown Toronto - B and C step ~1.1km north each, far enough apart that readings are unambiguous
    public static readonly Position A = new(43.6532, -79.3832);
    public static readonly Position B = new(43.6632, -79.3832);
    public static readonly Position C = new(43.6732, -79.3832);

    // how close a reading must land to the position that was pushed
    public static readonly Distance Tolerance = Distance.FromMeters(150);


    public static bool IsNear(this GpsReading reading, Position position)
        => reading.Position.GetDistanceTo(position) <= Tolerance;


    public static Task SetAsync(TestContext ctx, Position position)
    {
        ctx.Log($"location -> {position.Latitude}, {position.Longitude}");
        return ctx.Host.SetLocationAsync(position.Latitude, position.Longitude, ctx.CancellationToken);
    }


    public static async Task<T> HoldAsync<T>(TestContext ctx, Position position, Task<T> waitFor)
    {
        await SetAsync(ctx, position).ConfigureAwait(false);
        while (!waitFor.IsCompleted)
        {
            await Task.WhenAny(waitFor, Task.Delay(TimeSpan.FromSeconds(5), ctx.CancellationToken)).ConfigureAwait(false);
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (!waitFor.IsCompleted)
                await ctx.Host.SetLocationAsync(position.Latitude, position.Longitude, ctx.CancellationToken).ConfigureAwait(false);
        }
        return await waitFor.ConfigureAwait(false);
    }


    public static async Task HoldAsync(TestContext ctx, Position position, Task waitFor)
        => await HoldAsync(ctx, position, WithResult(waitFor)).ConfigureAwait(false);


    static async Task<bool> WithResult(Task task)
    {
        await task.ConfigureAwait(false);
        return true;
    }
}

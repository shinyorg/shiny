using Microsoft.Extensions.AI;

namespace Shiny.Locations.Extensions.AI.Internal;

static class LocationAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(IGpsManager gps) =>
    [
        new GetCurrentLocationFunction(gps),
        new GetDistanceToFunction(gps),
        new EstimateTravelTimeFunction(gps)
    ];
}

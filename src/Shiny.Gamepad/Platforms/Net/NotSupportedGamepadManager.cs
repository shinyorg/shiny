using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>
/// An <see cref="IGamepadManager"/> for hosts with no controller API - a server, a console tool, a
/// test run.
/// </summary>
/// <remarks>
/// Reports no controllers, forever, and never raises a connection event. Nothing throws: code
/// written against the real thing already has to cope with "no controller is plugged in", so
/// reporting exactly that is both honest and the case every caller has handled.
/// </remarks>
class NotSupportedGamepadManager(ILogger<NotSupportedGamepadManager> logger) : AbstractGamepadManager(logger)
{
    protected override Task OnStart(CancellationToken ct)
    {
        this.Logger.WatchStarted(0);
        return Task.CompletedTask;
    }
}

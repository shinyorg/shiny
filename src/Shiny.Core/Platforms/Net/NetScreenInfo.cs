using System;

namespace Shiny;


/// <summary>
/// The <see cref="IScreenInfo"/> for a host with no display API of its own - Linux, console apps and services. It
/// reports <see cref="ScreenMetrics.Empty"/>, never raises a change, and cannot lock orientation.
/// </summary>
public class NetScreenInfo : AbstractScreenInfo
{
    protected override ScreenMetrics ReadMetrics() => ScreenMetrics.Empty;
    protected override IDisposable Watch(Action refresh) => new ActionDisposable(() => { });
}

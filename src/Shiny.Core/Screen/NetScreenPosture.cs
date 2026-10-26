using System;

namespace Shiny;


/// <summary>
/// The <see cref="IScreenPosture"/> for a platform with no posture API - macOS, Mac Catalyst, tvOS, Windows, Linux,
/// console apps and services. It reports <see cref="ScreenPosture.None"/> and never raises a change.
/// </summary>
public class NetScreenPosture : AbstractScreenPosture
{
    public override bool IsSupported => false;
    protected override ScreenPosture ReadPosture() => ScreenPosture.None;
    protected override IDisposable Watch(Action refresh) => new ActionDisposable(() => { });
}

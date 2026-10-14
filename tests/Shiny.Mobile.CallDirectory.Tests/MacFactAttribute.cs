namespace Shiny.CallDirectory.Tests;


/// <summary>
/// The store P/Invokes the system libsqlite3 at its Apple path and the contract test needs swiftc, so these run on
/// macOS only.
/// </summary>
public sealed class MacFactAttribute : FactAttribute
{
    public MacFactAttribute()
    {
        if (!OperatingSystem.IsMacOS())
            this.Skip = "Needs macOS (system libsqlite3 and swiftc)";
    }
}

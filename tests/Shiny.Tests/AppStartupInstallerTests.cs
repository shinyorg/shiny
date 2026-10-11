using System;
using System.Threading.Tasks;
using Xunit;

namespace Shiny.Tests;


public class AppStartupInstallerTests
{
    [Theory]
    [InlineData(@"C:\apps\tool.exe", @"C:\apps\tool.exe")]
    [InlineData(@"C:\Program Files\My App\app.exe", @"""C:\Program Files\My App\app.exe""")]
    [InlineData(@"C:\dir with space\", @"""C:\dir with space\\""")]
    [InlineData(@"say ""hi""", @"""say \""hi\""""")]
    [InlineData("", @"""""")]
    public void CommandLineQuote_FollowsCommandLineToArgvRules(string input, string expected)
        => Assert.Equal(expected, AppStartupInstaller.CommandLineQuote(input));


    [Fact]
    public void DesktopEntryQuote_EscapesReservedCharacters()
        => Assert.Equal(@"""/opt/my app/run \$HOME \`x\` \""q\"" \\""", AppStartupInstaller.DesktopEntryQuote(@"/opt/my app/run $HOME `x` ""q"" \"));


    [Fact]
    public void SystemdQuote_DoublesSpecifiersAndExpansions()
        => Assert.Equal(@"""/opt/app --rate 50%% --home $$HOME""", AppStartupInstaller.SystemdQuote("/opt/app --rate 50% --home $HOME"));


    [Fact]
    public void Values_NeverCarryANewline()
    {
        Assert.Equal("a b", AppStartupInstaller.DesktopEntryValue("a\r\nb"));
        Assert.Equal("a b", AppStartupInstaller.SystemdValue("a\nb"));
    }


    [Fact]
    public async Task UnsupportedHost_ReportsNotSupported()
    {
        // the test host is macOS or Windows CI on plain net10.0 - Windows is a real install, so only assert off it
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
            return;

        var installer = new AppStartupInstaller(new AppStartupOptions());
        Assert.False(installer.IsSupported);
        Assert.Equal(AppStartupState.NotSupported, await installer.GetState());
        Assert.Equal(AppStartupState.NotSupported, await installer.Register());
    }
}

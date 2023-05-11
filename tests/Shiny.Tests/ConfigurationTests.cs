
namespace Shiny.Tests;

[Trait("Category", "Configuration")]
public class ConfigurationTests 
{
    [Fact(DisplayName = "Configuration - Platform Overrides")]
    public void PlatformOverrides()
    {
        var cfg = MauiProgram.Configuration; // this is built into the core container of MAUI
        var value = cfg["PlatformValue"];
        var shouldBe = "";
#if ANDROID
        shouldBe = "ANDROID";
#elif MACCATALYST
        shouldBe = "MACCATALYST";
#elif IOS
        shouldBe = "IOS";
#endif
        value.Should().Be(shouldBe);
    }
}
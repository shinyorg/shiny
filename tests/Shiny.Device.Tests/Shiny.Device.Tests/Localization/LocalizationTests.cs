using System.Threading.Tasks;
using Shiny.Localization;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Shiny.Device.Tests.Localization
{
    public class LocalizationTests
    {
        readonly ITestOutputHelper output;
        readonly ILocalizationManager localizationManager;

        public LocalizationTests(ITestOutputHelper output)
        {
            this.output = output;
            this.localizationManager = ShinyHost.Container.GetService<ILocalizationManager>();
        }

        async Task<bool> Setup() => await this.localizationManager.InitializeAsync();

        [Fact]
        public async Task SetupTest()
        {
            var success = await this.Setup();

            Assert.True(success);
        }

        [Fact]
        public async Task StatusTest()
        {
            await this.Setup();

            Assert.Equal(LocalizationState.Some, this.localizationManager.Status);
        }

        [Fact]
        public async Task LocalizationTest()
        {
            await this.Setup();

            var localizedValue = this.localizationManager.GetText(nameof(DeviceTextResources.TestKey));

            Assert.Equal(DeviceTextResources.TestKey, localizedValue);
        }
    }
}

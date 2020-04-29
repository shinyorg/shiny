using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Localization;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Device.Tests.Localization.OtherResources;
using Xunit;

namespace Shiny.Device.Tests.Localization
{
    public class LocalizationTests
    {
        readonly ITestOutputHelper output;
        readonly ILocalizationManager localizationManager;
        bool isInitialized;

        public LocalizationTests(ITestOutputHelper output)
        {
            this.output = output;
            this.localizationManager = ShinyHost.Container.GetService<ILocalizationManager>();
        }

        async Task<bool> Setup(CultureInfo? culture = null)
        {
            if(!this.isInitialized)
                this.isInitialized = await this.localizationManager.InitializeAsync(culture);

            return this.isInitialized;
        }

        [Fact]
        public async Task SetupTest()
        {
            var success = await this.Setup();

            this.output.WriteLine("Localization initialization succeed!");

            Assert.True(success);
        }

        [Fact]
        public async Task StatusTest()
        {
            await this.Setup();

            this.output.WriteLine($"Does '{LocalizationState.Some}' equals '{this.localizationManager.Status}' ?");

            Assert.Equal(LocalizationState.Some, this.localizationManager.Status);
        }

        [Fact]
        public async Task CurrentCultureTest()
        {
            var initializationCulture = CultureInfo.CreateSpecificCulture("es-ES");

            await this.Setup(initializationCulture);

            this.output.WriteLine($"{nameof(this.localizationManager.CurrentCulture)}: {this.localizationManager.CurrentCulture.DisplayName}");

            Assert.Equal(initializationCulture, this.localizationManager.CurrentCulture);
        }

        [Fact]
        public async Task AvailableCulturesTest()
        {
            await this.Setup();

            this.output.WriteLine($"{nameof(this.localizationManager.AvailableCultures)}: {string.Join(", ", this.localizationManager.AvailableCultures.Select(x => x.DisplayName))}");

            Assert.Contains(CultureInfo.CreateSpecificCulture("es-ES"), this.localizationManager.AvailableCultures);
        }

        [Fact]
        public async Task DeviceLocalizationTest()
        {
            await this.Setup();

            var localizedValue = this.localizationManager.GetText(nameof(DeviceTextResources.TestKey));

            this.output.WriteLine($"Does '{DeviceTextResources.TestKey}' equals '{localizedValue}' ?");

            Assert.Equal(DeviceTextResources.TestKey, localizedValue);
        }

        [Fact]
        public async Task OtherLocalizationTest()
        {
            await this.Setup();

            var localizedValue = this.localizationManager.GetText(nameof(OtherTextResources.OtherTestKey));

            this.output.WriteLine($"Does '{OtherTextResources.OtherTestKey}' equals '{localizedValue}' ?");

            Assert.Equal(OtherTextResources.OtherTestKey, localizedValue);
        }
    }
}

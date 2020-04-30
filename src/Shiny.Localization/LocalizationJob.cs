using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Localization
{
    public class LocalizationJob : IJob
    {
        readonly ILocalizationManager localizationManager;
        readonly ILocalizationOptions localizationOptions;

        public LocalizationJob(ILocalizationManager localizationManager, ILocalizationOptions localizationOptions)
        {
            this.localizationManager = localizationManager;
            this.localizationOptions = localizationOptions;
        }

        public Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken) =>
            this.localizationManager.InitializeAsync(this.localizationOptions.InitializationCulture,
                this.localizationOptions.TryParents, this.localizationOptions.RefreshAvailableCultures, cancelToken);
    }
}

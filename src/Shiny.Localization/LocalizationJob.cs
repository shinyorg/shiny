using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Shiny.Localization
{
    public class LocalizationJob : IJob
    {
        readonly ILocalizationManager localizationManager;

        public LocalizationJob(ILocalizationManager localizationManager)
        {
            this.localizationManager = localizationManager;
        }

        public Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            jobInfo.Parameters.TryGetValue(nameof(LocalizationOptions.InitializationCulture), out var initializationCulture);

            return this.localizationManager.InitializeAsync((CultureInfo?) initializationCulture, cancelToken);
        }
    }
}

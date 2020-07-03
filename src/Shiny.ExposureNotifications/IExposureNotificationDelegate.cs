using System;
using System.Threading.Tasks;

namespace Shiny.ExposureNotifications
{
    public interface IExposureNotificationDelegate
    {
        Task SendSelfDiagnosis();
        Task OnExposureDetected();
        //Task FetchExposureKeyBatchFilesFromServerAsync(Func<IEnumerable<string>, Task> submitBatches, CancellationToken cancellationToken);

        //// Might be exposed, check and alert user if necessary
        //Task ExposureDetectedAsync(ExposureDetectionSummary summary, Func<Task<IEnumerable<ExposureInfo>>> getExposureInfo);

        //Task UploadSelfExposureKeysToServerAsync(IEnumerable<TemporaryExposureKey> temporaryExposureKeys);
    }
}

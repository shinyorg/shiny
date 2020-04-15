using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    public interface ILocalizationManager
    {
        IObservable<LocalizationState> WhenLocalizationStatusChanged();

        LocalizationState Status { get; }

        Task<bool> InitializeAsync(CultureInfo? cultureInfo = null, CancellationToken token = default);

        string GetText(string key);
    }
}

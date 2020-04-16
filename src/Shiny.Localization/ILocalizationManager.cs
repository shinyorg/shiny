using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    public interface ILocalizationManager
    {
        IObservable<LocalizationState> WhenLocalizationStatusChanged();

        LocalizationState Status { get; }

        CultureInfo CurrentCulture { get; }

        IList<CultureInfo> AvailableCultures { get; }

        Task<bool> InitializeAsync(CultureInfo? culture = null, CancellationToken token = default);

        string GetText(string key);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    public interface ILocalizationManager
    {
        /// <summary>
        /// Observable localization initialization state
        /// </summary>
        /// <returns>Localization state</returns>
        IObservable<LocalizationState> WhenLocalizationStatusChanged();

        /// <summary>
        /// Current localization state
        /// </summary>
        LocalizationState Status { get; }

        /// <summary>
        /// Current culture
        /// </summary>
        CultureInfo CurrentCulture { get; }

        /// <summary>
        /// Observable available cultures
        /// </summary>
        /// <returns>Available cultures</returns>
        IObservable<IList<CultureInfo>> WhenAvailableCulturesChanged();

        /// <summary>
        /// All available culture
        /// </summary>
        IList<CultureInfo> AvailableCultures { get; }

        /// <summary>
        /// Refresh AvailableCultures property (auto scan resx files with use of ResxTextProvider)
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True if refresh succeed</returns>
        Task<bool> RefreshAvailableCulturesAsync(CancellationToken token = default);

        /// <summary>
        /// Initialize localization plugin
        /// </summary>
        /// <param name="culture">Default initialization culture (default: null = CurrentUICulture)</param>
        /// <param name="tryParents">Try with parent culture up to invariant when the asked one can't be found (default: true)</param>
        /// <param name="refreshAvailableCultures">Refresh AvailableCultures property during initialization (default: false)</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if initialization succeed</returns>
        Task<bool> InitializeAsync(CultureInfo? culture = null, bool tryParents = true, bool refreshAvailableCultures = false, CancellationToken token = default);

        /// <summary>
        /// Get localized key
        /// </summary>
        /// <param name="key">Key to localize</param>
        /// <returns>Localized key or [key] if it can't be found</returns>
        string GetText(string key);
    }
}

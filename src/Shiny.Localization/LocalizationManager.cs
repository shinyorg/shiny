using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Logging;

namespace Shiny.Localization
{
    public class LocalizationManager : ILocalizationManager
    {
        readonly IEnumerable<ITextProvider> textProviders;
        readonly IList<IDictionary<string, string>> providersLocalizations;
        readonly Subject<LocalizationState> stateChanged;
        readonly SemaphoreSlim locker;

        public LocalizationManager(IEnumerable<ITextProvider> textProviders)
        {
            this.textProviders = textProviders;
            this.providersLocalizations = new List<IDictionary<string, string>>();
            this.stateChanged = new Subject<LocalizationState>();
            this.locker = new SemaphoreSlim(1, 1);
            this.AvailableCultures = new List<CultureInfo>();
        }

        IObservable<LocalizationState> ILocalizationManager.WhenLocalizationStatusChanged() => this.stateChanged;

        public LocalizationState Status { get; private set; }

        public CultureInfo CurrentCulture { get; private set; }

        public IList<CultureInfo> AvailableCultures { get; }

        public virtual async Task<bool> InitializeAsync(CultureInfo? culture = null, CancellationToken token = default)
        {
            await this.locker.WaitAsync(token);

            try
            {
                this.providersLocalizations.Clear();
                this.AvailableCultures.Clear();
                this.stateChanged.OnNext(this.Status = LocalizationState.Initializing);

                foreach (var textProvider in this.textProviders)
                {
                    var availableCultures = await textProvider.GetAvailableCulturesAsync(token);
                    foreach (var availableCulture in availableCultures)
                    {
                        if(this.AvailableCultures.All(x => x.Name != availableCulture.Name))
                            this.AvailableCultures.Add(availableCulture);
                    }
                }

                if (culture == null || !this.AvailableCultures.Contains(culture))
                {
                    culture = this.AvailableCultures.Contains(CultureInfo.CurrentUICulture)
                        ? CultureInfo.CurrentUICulture
                        : CultureInfo.InvariantCulture;
                }

                foreach (var textProvider in this.textProviders)
                {
                    var localizations = await textProvider.GetTextResourcesAsync(culture, token);
                    if (!localizations.IsEmpty())
                        this.providersLocalizations.Add(localizations);
                }

                if (!this.providersLocalizations.IsEmpty())
                    this.CurrentCulture = culture;

                this.stateChanged.OnNext(this.Status = this.providersLocalizations.IsEmpty() ? LocalizationState.None : LocalizationState.Some);

                return !this.providersLocalizations.IsEmpty();
            }
            catch (Exception ex)
            {
                this.stateChanged.OnNext(this.Status = LocalizationState.Error);
                Log.Write(ex);
                return false;
            }
            finally
            {
                this.locker.Release();
            }
        }

        public virtual string GetText(string key)
        {
            try
            {
                foreach (var providerLocalizations in this.providersLocalizations)
                {
                    if (providerLocalizations.TryGetValue(key, out var value))
                        return value;
                }

                return $"[{key}]";
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                return $"[{key}]";
            }
        }
    }
}

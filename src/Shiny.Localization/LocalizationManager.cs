using System;
using System.Collections.Generic;
using System.Globalization;
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
        }

        public IObservable<LocalizationState> OnStateChanged => this.stateChanged;

        public virtual async Task<bool> InitializeAsync(CultureInfo? askedCulture = null, CancellationToken token = default)
        {
            await this.locker.WaitAsync(token);

            try
            {
                this.providersLocalizations.Clear();
                this.stateChanged.OnNext(LocalizationState.Initializing);

                #region Asked culture

                /*
                 * Priority 1: Check if potential asked culture is available in each provider
                 */
                if (askedCulture != null)
                {
                    foreach (var textProvider in this.textProviders)
                    {
                        var localizations = await textProvider.GetTextResourcesAsync(askedCulture, token);
                        if (!localizations.IsEmpty())
                            this.providersLocalizations.Add(localizations);
                    }
                }

                #endregion

                #region UI culture

                /*
                 * Priority 2: Check if UI culture is available in each provider
                 */
                if (!Equals(CultureInfo.CurrentUICulture, askedCulture))
                {
                    foreach (var textProvider in this.textProviders)
                    {
                        var localizations = await textProvider.GetTextResourcesAsync(CultureInfo.CurrentUICulture, token);
                        if (!localizations.IsEmpty())
                            this.providersLocalizations.Add(localizations);
                    }
                }

                #endregion

                #region Invariant culture

                /*
                 * Priority 3: Check if invariant culture is available in each provider
                 */
                if (!Equals(CultureInfo.InvariantCulture, askedCulture) && !Equals(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture))
                {
                    foreach (var textProvider in this.textProviders)
                    {
                        var localizations = await textProvider.GetTextResourcesAsync(CultureInfo.InvariantCulture, token);
                        if (!localizations.IsEmpty())
                            this.providersLocalizations.Add(localizations);
                    }
                }

                #endregion

                this.stateChanged.OnNext(this.providersLocalizations.IsEmpty() ? LocalizationState.Empty : LocalizationState.Available);

                return !this.providersLocalizations.IsEmpty();
            }
            catch (Exception ex)
            {
                this.stateChanged.OnNext(LocalizationState.Error);
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

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
                var foundProvidersLocalizations = new Dictionary<CultureInfo, IList<IDictionary<string, string>>>();
                var availableCultures = new List<CultureInfo>();
                this.providersLocalizations.Clear();
                this.AvailableCultures.Clear();
                this.stateChanged.OnNext(this.Status = LocalizationState.Initializing);

                foreach (var textProvider in this.textProviders)
                {
                    var providerAvailableCultures = await textProvider.GetAvailableCulturesAsync(token);
                    foreach (var providerAvailableCulture in providerAvailableCultures)
                    {
                        if(availableCultures.All(x => x.Name != providerAvailableCulture.Name))
                            availableCultures.Add(providerAvailableCulture);
                    }
                }

                if (!availableCultures.IsEmpty())
                {
                    var invariantCulture = availableCultures.FirstOrDefault(x => x.Name == CultureInfo.InvariantCulture.Name);
                    if (invariantCulture != null)
                    {
                        this.AvailableCultures.Add(invariantCulture);
                        availableCultures.Remove(invariantCulture);
                    }

                    foreach (var availableCulture in availableCultures.OrderBy(x => x.Name))
                    {
                        this.AvailableCultures.Add(availableCulture);
                    }

                    if (culture == null)
                        culture = CultureInfo.CurrentUICulture;

                    foreach (var textProvider in this.textProviders)
                    {
                        var currentCulture = culture;
                        var tryParent = true;
                        while (tryParent)
                        {
                            var localizations = await textProvider.GetTextResourcesAsync(currentCulture, token);
                            if (!localizations.IsEmpty())
                            {
                                if (!foundProvidersLocalizations.TryGetValue(currentCulture, out var currentCultureLocalizations))
                                {
                                    currentCultureLocalizations = new List<IDictionary<string, string>>();
                                    foundProvidersLocalizations.Add(currentCulture, currentCultureLocalizations);
                                }

                                currentCultureLocalizations.Add(localizations);
                                tryParent = false;
                            }
                            else if (currentCulture.Name == CultureInfo.InvariantCulture.Name)
                            {
                                tryParent = false;
                            }
                            else
                            {
                                currentCulture = currentCulture.Parent;
                            }
                        }
                    }

                    if (!foundProvidersLocalizations.IsEmpty())
                    {
                        if (foundProvidersLocalizations.Any(x => x.Key.Name != CultureInfo.InvariantCulture.Name) &&
                            foundProvidersLocalizations.Any(x => x.Key.Name == CultureInfo.InvariantCulture.Name))
                            foundProvidersLocalizations.Remove(CultureInfo.InvariantCulture);

                        var foundProvidersLocalizationsOrdered = foundProvidersLocalizations.OrderByDescending(x => x.Key.Name.Length).ToList();
                        foreach (var foundCultureLocalizations in foundProvidersLocalizationsOrdered)
                        {
                            foreach (var foundLocalizations in foundCultureLocalizations.Value)
                            {
                                this.providersLocalizations.Add(foundLocalizations);
                            }
                        }
                        this.CurrentCulture = foundProvidersLocalizationsOrdered.First().Key;
                    } 
                }

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

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
        readonly Subject<IList<CultureInfo>> availableCulturesChanged;
        readonly SemaphoreSlim availableCulturesLocker;
        readonly SemaphoreSlim initializationLocker;

        public LocalizationManager(IEnumerable<ITextProvider> textProviders)
        {
            this.textProviders = textProviders;
            this.providersLocalizations = new List<IDictionary<string, string>>();
            this.stateChanged = new Subject<LocalizationState>();
            this.availableCulturesChanged = new Subject<IList<CultureInfo>>();
            this.availableCulturesLocker = new SemaphoreSlim(1, 1);
            this.initializationLocker = new SemaphoreSlim(1, 1);
            this.AvailableCultures = new List<CultureInfo>();
        }

        IObservable<LocalizationState> ILocalizationManager.WhenLocalizationStatusChanged() => this.stateChanged;

        public LocalizationState Status { get; private set; }

        public CultureInfo CurrentCulture { get; private set; }

        public IObservable<IList<CultureInfo>> WhenAvailableCulturesChanged() => this.availableCulturesChanged;

        public IList<CultureInfo> AvailableCultures { get; }

        public virtual async Task<bool> RefreshAvailableCulturesAsync(CancellationToken token = default)
        {
            await this.availableCulturesLocker.WaitAsync(token);

            try
            {
                var availableCultures = new List<CultureInfo>();

                foreach (var textProvider in this.textProviders)
                {
                    var providerAvailableCultures = await textProvider.GetAvailableCulturesAsync(token);
                    foreach (var providerAvailableCulture in providerAvailableCultures)
                    {
                        if (availableCultures.All(x => x.Name != providerAvailableCulture.Name))
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
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Write(e);
                return false;
            }
            finally
            {
                this.availableCulturesChanged.OnNext(this.AvailableCultures);
                this.availableCulturesLocker.Release();
            }
        }

        public virtual async Task<bool> InitializeAsync(CultureInfo? culture = null, bool tryParents = true, bool refreshAvailableCultures = false, CancellationToken token = default)
        {
            await this.initializationLocker.WaitAsync(token);

            try
            {
                var foundProvidersLocalizations = new Dictionary<CultureInfo, IList<IDictionary<string, string>>>();
                this.providersLocalizations.Clear();
                this.AvailableCultures.Clear();
                this.stateChanged.OnNext(this.Status = LocalizationState.Initializing);

                if (refreshAvailableCultures)
                    await this.RefreshAvailableCulturesAsync(token);

                if (culture == null)
                    culture = CultureInfo.CurrentUICulture;

                foreach (var textProvider in this.textProviders)
                {
                    var currentCulture = culture;
                    var currentTryParents = tryParents;
                    while (currentTryParents)
                    {
                        var localizations = await textProvider.GetTextResourcesAsync(currentCulture, token);
                        if (!localizations.IsEmpty())
                        {
                            if (!foundProvidersLocalizations.TryGetValue(currentCulture,
                                out var currentCultureLocalizations))
                            {
                                currentCultureLocalizations = new List<IDictionary<string, string>>();
                                foundProvidersLocalizations.Add(currentCulture, currentCultureLocalizations);
                            }

                            currentCultureLocalizations.Add(localizations);
                            currentTryParents = false;
                        }
                        else if (currentCulture.Name == CultureInfo.InvariantCulture.Name)
                        {
                            currentTryParents = false;
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
                this.initializationLocker.Release();
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

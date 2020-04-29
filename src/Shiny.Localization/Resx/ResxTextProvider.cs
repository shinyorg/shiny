using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization.Resx
{
    /// <summary>
    /// Resx text provider
    /// </summary>
    /// <typeparam name="T">Should be a Resx Designer class</typeparam>
    public class ResxTextProvider<T> : IResxTextProvider<T> where T : class
    {
        readonly ResourceManager resourceManager;
        readonly CultureInfo? invariantCulture;
        TaskCompletionSource<IList<CultureInfo>>? availableCulturesTcs;
        TaskCompletionSource<IDictionary<string, string>>? textResourcesTcs;

        public ResxTextProvider(ILocalizationOptions options)
        {
            this.resourceManager = new ResourceManager(typeof(T));
            this.invariantCulture = options.TextProviders[this.GetType()];
        }

        public Task<IList<CultureInfo>> GetAvailableCulturesAsync(CancellationToken token = default)
        {
            if (this.availableCulturesTcs?.Task != null &&
                !this.availableCulturesTcs.Task.IsCanceled && 
                !this.availableCulturesTcs.Task.IsCompleted &&
                !this.availableCulturesTcs.Task.IsFaulted)
                return this.availableCulturesTcs.Task;

            this.availableCulturesTcs = new TaskCompletionSource<IList<CultureInfo>>();

            Task.Run(() =>
            {
                using (token.Register(() => this.availableCulturesTcs.TrySetCanceled()))
                {
                    var availableCultures = new List<CultureInfo>();
                    var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                    foreach (var culture in allCultures)
                    {
                        try
                        {
                            var rs = this.resourceManager.GetResourceSet(culture, true, false);
                            if (rs != null)
                                availableCultures.Add(culture.Name == CultureInfo.InvariantCulture.Name && this.invariantCulture != null ? this.invariantCulture : culture);
                        }
                        catch (CultureNotFoundException e)
                        {

                        }
                    }

                    this.resourceManager.ReleaseAllResources();

                    this.availableCulturesTcs.TrySetResult(availableCultures);
                }
            }, token);
            

            return this.availableCulturesTcs.Task;
        }

        public Task<IDictionary<string, string>> GetTextResourcesAsync(CultureInfo cultureInfo, CancellationToken token = default)
        {
            if (this.textResourcesTcs?.Task != null &&
                !this.textResourcesTcs.Task.IsCanceled &&
                !this.textResourcesTcs.Task.IsCompleted &&
                !this.textResourcesTcs.Task.IsFaulted)
                return this.textResourcesTcs.Task;

            this.textResourcesTcs = new TaskCompletionSource<IDictionary<string, string>>();

            Task.Run(() =>
            {
                using (token.Register(() => this.textResourcesTcs.TrySetCanceled()))
                {
                    var localizations = this.resourceManager.GetResourceSet(cultureInfo, true, false)
                        ?.Cast<DictionaryEntry>()
                        .ToDictionary(e => e.Key.ToString(), e => e.Value.ToString());

                    this.textResourcesTcs.TrySetResult(localizations ?? new Dictionary<string, string>());
                }
            }, token);
            
            return this.textResourcesTcs.Task;
        }
    }
}

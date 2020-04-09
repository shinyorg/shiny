using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    /// <summary>
    /// Resx text provider
    /// </summary>
    /// <typeparam name="T">Should be a Resx Designer class</typeparam>
    public class ResxTextProvider<T> : ITextProvider where T : class
    {
        readonly ResourceManager resourceManager;

        public ResxTextProvider()
        {
            this.resourceManager = new ResourceManager(typeof(T));
        }

        public Task<IDictionary<string, string>> GetTextResourcesAsync(CultureInfo cultureInfo, CancellationToken token = default)
        {
            var localizations = this.resourceManager.GetResourceSet(cultureInfo, true, true)
                ?.Cast<DictionaryEntry>()
                .ToDictionary(e => e.Key.ToString(), e => e.Value.ToString());

            return Task.FromResult<IDictionary<string, string>>(localizations ?? new Dictionary<string, string>());
        }
    }
}

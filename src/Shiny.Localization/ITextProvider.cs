using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    public interface ITextProvider
    {
        /// <summary>
        /// Get all text resources from the provider for the specified culture
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetTextResourcesAsync(CultureInfo cultureInfo, CancellationToken token = default);
    }
}

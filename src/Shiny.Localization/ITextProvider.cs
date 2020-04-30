using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Localization
{
    public interface ITextProvider
    {
        /// <summary>
        /// Culture used as invariant (default = InvariantCulture)
        /// </summary>
        CultureInfo? InvariantCulture { get; }

        /// <summary>
        /// Get all available cultures from the provider
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IList<CultureInfo>> GetAvailableCulturesAsync(CancellationToken token = default);

        /// <summary>
        /// Get all text resources from the provider for the specified culture
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> GetTextResourcesAsync(CultureInfo cultureInfo, CancellationToken token = default);
    }
}

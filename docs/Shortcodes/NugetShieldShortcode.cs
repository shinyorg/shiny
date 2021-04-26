using Statiq.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Docs.Shortcodes
{
    //https://statiq.dev/framework/content/shortcodes
    public class NugetShieldShortcode : IShortcode
    {
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packageName = args.FirstOrDefault().Value;
            var usePackageLabel = args.Any(x => x.Key?.Equals("label") ?? false);
            var a = Utils.ToNugetShield(packageName, usePackageLabel);

            return new[] { new ShortcodeResult(a) };
        }
    }
}

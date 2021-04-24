using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Statiq.Common;


namespace Docs.Shortcodes
{
    public class PackageInfo : IShortcode
    {
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packageName = args.FirstOrDefault().Value;
            var package = Utils.GetPackage(packageName);
            var nuget = Utils.ToNugetShield(package.Name, false);

            var table = @$"
<table>
    <tr>
        <th>Area</th>
        <th>Info</th>
    </tr>
    <tr>
        <td>NuGet</td>
        <td>{nuget}</td>
    </tr>
</table>";

            return new[] { new ShortcodeResult(table) };
        }
    }
}

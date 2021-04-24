using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Statiq.Common;


namespace Docs.Shortcodes
{
    public class NugetPage : IShortcode
    {
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            // groupby category for separate tables
            var list = Utils.GetAllPackages().OrderBy(x => x.Name).ToList();

            var sb = new StringBuilder();
            sb.Append("<table><tr><th>Name</th><th>Description</th><th>NuGet</th>");

            foreach (var obj in list)
            {
                var shield = Utils.ToNugetShield(obj.Name, false);
                sb.AppendLine($"<tr><td>{obj.Name}</td><td>{obj.Description}</td><td>{shield}</td></tr>");
            }
            sb.AppendLine("</table>");
            return new [] { new ShortcodeResult(sb.ToString()) };
        }
    }
}

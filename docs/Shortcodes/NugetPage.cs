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
            //context.Phase == Phase.Process
            var list = Utils
                .GetAllPackages()
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name)
                .ToList();

            var sb = new StringBuilder();
            sb
                .AppendLine("# ")
                .AppendLine()
                .AppendLine("|Name|Description|NuGet|")
                .AppendLine("|----|-----------|-----|");

            foreach (var obj in list)
            {
                var shield = Utils.ToNugetShield(obj.Name, false);
                sb.AppendLine($"|**{obj.Name}**|{obj.Description}|{shield}|");
            }
            return new [] { new ShortcodeResult(sb.ToString()) };
        }
    }
}

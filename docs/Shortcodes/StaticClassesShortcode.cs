using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Statiq.Common;


namespace Docs.Shortcodes
{
    public class StaticClassesShortcode : SyncShortcode
    {
        public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var services = Utils
                .GetAllPackages()
                .Where(x => x.Services != null)
                .SelectMany(x => x.Services)
                .Where(x =>
                    !x.Name.IsNullOrEmpty() &&
                    !x.Static.IsNullOrEmpty()
                )
                .OrderBy(x => x.Name)
                .ToList();

            var sb = new StringBuilder();
            sb
                .AppendLine("|Service|Static Class|")
                .AppendLine("|-------|------------|");

            foreach (var service in services)
                sb.AppendLine($"|{service.Name}|{service.Static}|");

            return new ShortcodeResult(sb.ToString());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Statiq.Common;


namespace Docs.Shortcodes
{
    public class StaticClasses : IShortcode
    {
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packages = Utils.GetAllPackages().Where(x => !x.Services?.Any(x => !x.Static.IsNullOrEmpty()) ?? false).OrderBy(x => x.Name);
            var sb = new StringBuilder();

            sb.Append("<table><tr><th>Package</th><th>Service</th><th>Static Class</th></tr>");
            foreach (var package in packages)
            {
                var services = package.Services!.OrderBy(x => x.Name);
                foreach (var service in services)
                    sb.AppendLine($"<tr><td>{package.Name}</td><td>{service.Name}</td><td>{service.Static}</td></tr>");
            }
            sb.Append("</table>");

            return new [] { new ShortcodeResult(sb.ToString()) };
        }
    }
}

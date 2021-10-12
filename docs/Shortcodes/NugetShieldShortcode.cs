using Statiq.Common;
using System.Collections.Generic;


namespace Docs.Shortcodes
{
    public class NugetShieldShortcode : SyncShortcode
    {
        public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var packageName = args[0].Value;
            var label = args.Length == 2 ? args[1].Value : null;
            var a = Utils.ToNugetShield(packageName, label);

            return new ShortcodeResult(a);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Statiq.Common;


namespace Docs.Shortcodes
{
    public class StartupShortcode : IShortcode
    {
        const string StartupLayout = @"
```csharp
using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace YourNamespace
{
    public class YourShinyStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        { 
            {{BODY}} 
        }
    }
}
```
";
        public async Task<IEnumerable<ShortcodeResult>> ExecuteAsync(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
        {
            var full = StartupLayout.Replace("{{BODY}}", content);
            return new [] { new ShortcodeResult(full) };
        }
    }
}

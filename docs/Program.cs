using System;
using System.Threading.Tasks;
using Devlead.Statiq.Code;
using Devlead.Statiq.Tabs;
using Statiq.App;
using Statiq.Common;
using Statiq.Web;
using Docs.Shortcodes;


namespace Docs
{
    class Program
    {
        public static async Task<int> Main(string[] args) =>
            await Bootstrapper
                .Factory
                .CreateWeb(args)
                .AddSetting(Keys.Host, "shinyorg.github.io")
                .AddSetting(Keys.LinksUseHttps, true)
                .DeployToGitHubPages(
                    "shinyorg",
                    "shinyorg.github.io",
                    Config.FromSetting<string>("GITHUB_TOKEN")
                )
                .AddTabGroupShortCode()
                .AddIncludeCodeShortCode()
                .AddShortcode<StartupShortcode>("Startup")
                .AddShortcode<PackageInfoShortcode>("PackageInfo")
                .AddShortcode<NugetShieldShortcode>("NugetShield")
                .AddShortcode<StaticClassesShortcode>("StaticClasses")
                .RunAsync();
    }
}

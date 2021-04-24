using System;
using System.Threading.Tasks;
using Docs.Shortcodes;
using Statiq.App;
using Statiq.Common;
using Statiq.Web;


namespace Docs
{
    class Program
    {
        public static async Task<int> Main(string[] args) =>
            await Bootstrapper
                .Factory
                .CreateWeb(args)
                .AddSetting(Keys.Host, "shinyorg.github.io")
                //.AddSetting(Keys.LinkRoot, "/docs")
                .AddSetting(Keys.LinksUseHttps, true)
                //.AddSetting(Constants.EditLink, ConfigureEditLink())
                .AddShortcode<PackageInfo>(nameof(PackageInfo))
                .AddShortcode<NugetShield>(nameof(NugetShield))
                .AddShortcode<NugetPage>(nameof(NugetPage))
                .AddShortcode<StaticClasses>(nameof(StaticClasses))
                .RunAsync();

        //static Config<string> ConfigureEditLink() => Config.FromDocument((doc, ctx) =>
        //{
        //    string.Format("https://github.com/{0}/{1}/edit/{2}/docs/input/{3}",
        //        ctx.GetString(Constants.Site.Owner),
        //        ctx.GetString(Constants.Site.Repository),
        //        ctx.GetString(Constants.Site.Branch),
        //        doc.Source.GetRelativeInputPath());
        //});
    }
}

using System.Threading.Tasks;
using Cake.Boots;
using Cake.Core.Diagnostics;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    public sealed class BootsTask : AsyncFrostingTask<BuildContext>
    {
        const ReleaseChannel Channel = ReleaseChannel.Preview;


        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI && context.UseXamarinPreview;


        public override async Task RunAsync(BuildContext context)
        {
            context.Log.Information($"Installing Boots - {Channel} Channel");

            //context.VSToolSetup();
            await context.Boots(Product.Mono, Channel);
            await context.Boots(Product.XamarinAndroid, Channel);
            await context.Boots(Product.XamariniOS, Channel);

            context.Log.Information($"Boots {Channel} Installed Successfully");
        }
    }
}

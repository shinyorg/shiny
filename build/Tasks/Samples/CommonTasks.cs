using System.Threading.Tasks;
using Cake.Boots;
using Cake.Common.IO;
using Cake.Core.Diagnostics;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Samples
{
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"samples/**/bin/{context.MsBuildConfiguration}");
        }
    }


    public sealed class BootsTask : AsyncFrostingTask<BuildContext>
    {
        const ReleaseChannel Channel = ReleaseChannel.Preview;


        public override async Task RunAsync(BuildContext context)
        {
            if (context.IsRunningInCI)
            {
                context.Log.Information($"Installing Boots - {Channel} Channel");

                await context.Boots(Product.Mono, Channel);
                await context.Boots(Product.XamarinAndroid, Channel);
                await context.Boots(Product.XamariniOS, Channel);
            }
            context.Log.Information($"Boots {Channel} Installed Successfully");
        }
    }
}

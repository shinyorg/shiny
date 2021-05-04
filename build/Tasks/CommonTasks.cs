using System;
using System.Threading.Tasks;
using Cake.Boots;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    [TaskName("Boots")]
    public sealed class BootsTask : AsyncFrostingTask<BuildContext>
    {
        const ReleaseChannel Channel = ReleaseChannel.Stable;


        public override async Task RunAsync(BuildContext context)
        {
            if (!context.IsRunningInCI)
                return;

            await context.Boots(Product.Mono, Channel);
            await context.Boots(Product.XamarinAndroid, Channel);
            await context.Boots(Product.XamariniOS, Channel);
        }
    }
}

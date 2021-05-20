using Cake.Core;
using Cake.Core.IO;


namespace ShinyBuild
{
    public static class Extensions 
    {
        public static IProcess Execute(this ICakeContext context, FilePath exe, string args) => 
            context.ProcessRunner.Start(
                exe,
                new ProcessSettings
                {
                    Arguments = ProcessArgumentBuilder.FromString(args)
                }
            );
    }
}
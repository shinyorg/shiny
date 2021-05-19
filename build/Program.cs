using System;
using Cake.Frosting;

namespace ShinyBuild
{
    class Program
    {
        //https://cakebuild.net/docs/running-builds/runners/cake-frosting#bootstrapping-for-cake-frosting
        public static int Main(string[] args)
            => new CakeHost()
                .UseContext<BuildContext>()
                //.UseLifetime<BuildLifetime>()
                //.InstallTool(new Uri("dotnet:n?package=GitVersion.Tool&version=5.6.9"))
                .Run(args);
    }
}

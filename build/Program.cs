using System;
using Cake.Frosting;

namespace ShinyBuild
{
    class Program
    {
        // TODO: build samples on src or samples
        // TODO: build src on src changes
        // TODO: build docs on main
        //https://cakebuild.net/docs/running-builds/runners/cake-frosting#bootstrapping-for-cake-frosting
        public static int Main(string[] args)
            => new CakeHost()
                .UseContext<BuildContext>()
                //.InstallTool(new Uri("nuget:?package=NUnit.ConsoleRunner&version=3.11.1"))
                .Run(args);
    }
}

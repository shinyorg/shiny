using System;
using Nuke.Common;


interface INugetPublish : INukeBuild, INugetCredentials
{

}


//DotNetNuGetPush(s => s
//        .SetSource(Source)
//        .SetSymbolSource(SymbolSource)
//        .SetApiKey(ApiKey)
//        .CombineWith(
//            OutputDirectory.GlobFiles("*.nupkg").NotEmpty(), (cs, v) => cs
//                .SetTargetPath(v)),
//degreeOfParallelism: 5,
//continueOnFailure: true);
//const string MainNuget = "https://api.nuget.org/v3/index.json";

//public override bool ShouldRun(BuildContext context)
//{
//    var result = context.IsNugetDeployBranch && context.IsRunningInCI;
//    if (result && String.IsNullOrWhiteSpace(context.NugetApiKey))
//        throw new ArgumentException("NugetApiKey is missing");

//    return result;
//}


//public override void Run(BuildContext context)
//{
//    // delete symbols for now
//    context.DeleteFiles("src/**/*.symbols.nupkg");

//    DoDeploy(context, context.NugetApiKey, MainNuget);
//}


//static void DoDeploy(BuildContext context, string apiKey, string sourceUrl)
//{
//    var settings = new DotNetCoreNuGetPushSettings
//    {
//        ApiKey = apiKey,
//        Source = sourceUrl,
//        SkipDuplicate = true
//    };
//    // delete symbols for now
//    context.DeleteFiles("src/**/*.symbols.nupkg");

//    var packages = context.GetFiles("src/**/*.nupkg");
//    foreach (var package in packages)
//    {
//        try
//        {
//            context.DotNetCoreNuGetPush(package.FullPath, settings);
//        }
//        catch (Exception ex)
//        {
//            if (context.AllowNugetUploadFailures)
//                context.Error($"Error Upload: {package.FullPath} - Exception: {ex}");
//            else
//                throw; // break build
//        }
//    }
//}
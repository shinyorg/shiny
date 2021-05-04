using System;
using System.Linq;

using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.MSBuild;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Git;

namespace ShinyBuild.Tasks
{
    [TaskName("Clean")]
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectories($"./src/**/bin/{context.MsBuildConfiguration}");
        }
    }


    //[TaskName("Tests")]
    //public sealed class BasicTestsTask : FrostingTask<BuildContext>
    //{
    //    public override void Run(BuildContext context)
    //    {
    //        context.XUnit2("../tests/");
    //    }
    //}


    [TaskName("Default")]
    [IsDependentOn(typeof(CleanTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            //var next = this.GetNextBuildNumber(context);

            context.MSBuild("Build.sln", x => x
                .WithRestore()
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("BuildNumber", "0")
                .WithProperty("PreviewRelease", (!context.IsMainBranch).ToString())
                .WithProperty("OS", context.Environment.Platform.Family == Cake.Core.PlatformFamily.Windows ? "WINDOWS_NT" : "MAC")
                .SetConfiguration(context.MsBuildConfiguration)
            );
        }


        //int GetNextBuildNumber(BuildContext context)
        //{
        //    var lastBuildTag = context
        //        .GitTags(context.Environment.WorkingDirectory)
        //        .FirstOrDefault(x =>
        //            x.FriendlyName.StartsWith("build-number-")
        //        );

        //    if (lastBuildTag == null)
        //        return 0;

        //    var buildNumberString = lastBuildTag.FriendlyName.Replace("build-number-", "");
        //    var buildNumber = Int32.Parse(buildNumberString);
        //    buildNumber++;

        //    // delete old tag, create new tag

        //    return buildNumber;
        //}
    }


    [TaskName("NugetDeploy")]
    [IsDependentOn(typeof(BuildTask))]
    //[IsDependeeOf(typeof(BasicTestsTask))]
    public sealed class NugetDeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (!context.Branch.FriendlyName.Equals("preview") && !context.IsMainBranch)
                return;

            var settings = new DotNetCoreNuGetPushSettings
            {
                ApiKey = context.NugetApiKey,
                Source = "https://api.nuget.org/v3/index.json",
                SkipDuplicate = true
            };
            var packages = context.GetFiles("../src/**/*.nupkg");
            foreach (var package in packages)
                context.DotNetCoreNuGetPush(package.FullPath, settings);
        }
    }
}
/*
//Trying to avoid any npm installs or anything that takes extra time...
const   https = require('https'),
        zlib = require('zlib'),
        fs = require('fs'),
        env = process.env;

function fail(message, exitCode=1) {
    console.log(`::error::${message}`);
    process.exit(1);
}

function request(method, path, data, callback) {

    try {
        if (data) {
            data = JSON.stringify(data);
        }
        const options = {
            hostname: 'api.github.com',
            port: 443,
            path,
            method,
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': data ? data.length : 0,
                'Accept-Encoding' : 'gzip',
                'Authorization' : `token ${env.INPUT_TOKEN}`,
                'User-Agent' : 'GitHub Action - development'
            }
        }
        const req = https.request(options, res => {

            let chunks = [];
            res.on('data', d => chunks.push(d));
            res.on('end', () => {
                let buffer = Buffer.concat(chunks);
                if (res.headers['content-encoding'] === 'gzip') {
                    zlib.gunzip(buffer, (err, decoded) => {
                        if (err) {
                            callback(err);
                        } else {
                            callback(null, res.statusCode, decoded && JSON.parse(decoded));
                        }
                    });
                } else {
                    callback(null, res.statusCode, buffer.length > 0 ? JSON.parse(buffer) : null);
                }
            });

            req.on('error', err => callback(err));
        });

        if (data) {
            req.write(data);
        }
        req.end();
    } catch(err) {
        callback(err);
    }
}


function main() {

    const path = 'BUILD_NUMBER/BUILD_NUMBER';
    const prefix = env.INPUT_PREFIX ? `${env.INPUT_PREFIX}-` : '';

    //See if we've already generated the build number and are in later steps...
    if (fs.existsSync(path)) {
        let buildNumber = fs.readFileSync(path);
        console.log(`Build number already generated in earlier jobs, using build number ${buildNumber}...`);
        //Setting the output and a environment variable to new build number...
        fs.writeFileSync(process.env.GITHUB_ENV, `BUILD_NUMBER=${buildNumber}`);
        console.log(`::set-output name=build_number::${buildNumber}`);
        return;
    }

    //Some sanity checking:
    for (let varName of ['INPUT_TOKEN', 'GITHUB_REPOSITORY', 'GITHUB_SHA']) {
        if (!env[varName]) {
            fail(`ERROR: Environment variable ${varName} is not defined.`);
        }
    }

    request('GET', `/repos/${env.GITHUB_REPOSITORY}/git/refs/tags/${prefix}build-number-`, null, (err, status, result) => {

        let nextBuildNumber, nrTags;

        if (status === 404) {
            console.log('No build-number ref available, starting at 1.');
            nextBuildNumber = 1;
            nrTags = [];
        } else if (status === 200) {
            const regexString = `/${prefix}build-number-(\\d+)$`;
            const regex = new RegExp(regexString);
            nrTags = result.filter(d => d.ref.match(regex));

            const MAX_OLD_NUMBERS = 5; //One or two ref deletes might fail, but if we have lots then there's something wrong!
            if (nrTags.length > MAX_OLD_NUMBERS) {
                fail(`ERROR: Too many ${prefix}build-number- refs in repository, found ${nrTags.length}, expected only 1. Check your tags!`);
            }

            //Existing build numbers:
            let nrs = nrTags.map(t => parseInt(t.ref.match(/-(\d+)$/)[1]));

            let currentBuildNumber = Math.max(...nrs);
            console.log(`Last build nr was ${currentBuildNumber}.`);

            nextBuildNumber = currentBuildNumber + 1;
            console.log(`Updating build counter to ${nextBuildNumber}...`);
        } else {
            if (err) {
                fail(`Failed to get refs. Error: ${err}, status: ${status}`);
            } else {
                fail(`Getting build-number refs failed with http status ${status}, error: ${JSON.stringify(result)}`);
            }
        }

        let newRefData = {
            ref:`refs/tags/${prefix}build-number-${nextBuildNumber}`,
            sha: env.GITHUB_SHA
        };

        request('POST', `/repos/${env.GITHUB_REPOSITORY}/git/refs`, newRefData, (err, status, result) => {
            if (status !== 201 || err) {
                fail(`Failed to create new build-number ref. Status: ${status}, err: ${err}, result: ${JSON.stringify(result)}`);
            }

            console.log(`Successfully updated build number to ${nextBuildNumber}`);

            //Setting the output and a environment variable to new build number...
            //fs.writeFileSync('$GITHUB_ENV', `BUILD_NUMBER=${nextBuildNumber}`);
            fs.writeFileSync(process.env.GITHUB_ENV, `BUILD_NUMBER=${nextBuildNumber}`);

            console.log(`::set-output name=build_number::${nextBuildNumber}`);
            //Save to file so it can be used for next jobs...
            fs.writeFileSync('BUILD_NUMBER', nextBuildNumber.toString());

            //Cleanup
            if (nrTags) {
                console.log(`Deleting ${nrTags.length} older build counters...`);

                for (let nrTag of nrTags) {
                    request('DELETE', `/repos/${env.GITHUB_REPOSITORY}/git/${nrTag.ref}`, null, (err, status, result) => {
                        if (status !== 204 || err) {
                            console.warn(`Failed to delete ref ${nrTag.ref}, status: ${status}, err: ${err}, result: ${JSON.stringify(result)}`);
                        } else {
                            console.log(`Deleted ${nrTag.ref}`);
                        }
                    });
                }
            }

        });
    });
}

main();

 */
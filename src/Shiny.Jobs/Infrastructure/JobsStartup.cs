﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Shiny.Jobs.Infrastructure;

public class JobsStartup : IShinyStartupTask
{

    public static bool ClearJobsBeforeRegistering { get; set; }

    static readonly List<JobInfo> jobs = new List<JobInfo>();
    public static void AddJob(JobInfo jobInfo) => jobs.Add(jobInfo);


    readonly IJobManager jobManager;
    readonly ILogger logger;


    public JobsStartup(IJobManager jobManager, ILogger<JobsStartup> logger)
    {
        this.jobManager = jobManager;
        this.logger = logger;
    }


    public async void Start()
    {
        if (ClearJobsBeforeRegistering)
        {
            try
            {
                await this.jobManager
                    .CancelAll()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Unable to cancel all existing jobs");
            }
        }

        if (jobs.Count > 0)
        {
            var access = await this.jobManager
                .RequestAccess()
                .ConfigureAwait(false);

            if (access == AccessState.Available)
            {
                foreach (var job in jobs)
                { 
                    await this.jobManager
                        .Register(job)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                this.logger.LogWarning("Permissions to run jobs insufficient: " + access);
            }
        }
        jobs.Clear();
    }
}
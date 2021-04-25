Title: FAQ
---
# Frequently Asked Questions

* If Doze is enabled, the reschedule period is not guaranteed to be an average of 10 mins.  It may be much longer.
* 
Q. When should I schedule my defined jobs?  Should I do everytime I start the app.

> When your app starts (Xamarin Forms App.OnStart is a good place).  It is a good practice to schedule every start.  The framework will only update jobs if one already exists.

Q. How long does the background sync let me have on iOS

> 30 seconds and not a penny more

Q. How do I schedule periodic jobs?

> All jobs are considered periodic with or without criteria

Q. Why no job triggers? (ie. geofence, bluetooth, specific time)

> I am considering some triggers in the future. The current limitations on the time factored jobs is that iOS is in complete control of how/when things are run

Q. How many jobs can I run?

> Technically as many as you want... BUT this was built with mobile timeslicing in mind (ie. iOS).  Your job set needs to complete within that timeslice as we don't set job ordering currently

Q. Is there job priorization?

> Not yet - debating this one for the future


Q. Why can't I set the next runtime

> From a true runtime perspective you can't, however, inside you job you can set add/update the job parameters or check the last runtime on the job info to see if you want to run.  Example below:

```csharp
public class SampleJob : IJob
{
    public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
    {
        var runJob = false;
        if (jobInfo.LastRunutc == null) // job has never run
            runJob = true;

        else if (DateTime.UtcNow > jobInfo.LastRunUtc.AddHours(1))
            runJob = true;  // its been at least an hour since the last run

        if (runJob)
        {
            ... do your job
        }
    }
}
```

Q. Can I add/update job parameters inside my job for the next run

> Yes
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Windows.Input;
using Shiny.Jobs;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Models;


namespace Samples.Jobs
{
    public class LogViewModel : ViewModel
    {
        readonly IJobManager jobManager;


        public LogViewModel(IJobManager jobManager,
                            IUserDialogs dialogs,
                            SampleSqliteConnection conn)
        {
            this.jobManager = jobManager;

            this.Purge = ReactiveCommand.CreateFromTask(async () =>
            {
                var confirm = await dialogs.ConfirmAsync(
                    "Do you wish to clear the job logs?",
                    "Confirm",
                    "Yes",
                    "No"
                );
                if (confirm)
                {
                    await conn.DeleteAllAsync<JobLog>();
                    this.Load.Execute();
                }
            });

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var logs = await conn.JobLogs.ToListAsync();
                this.Logs = logs
                    .Select(x =>
                    {
                        var msg = x.Started ? "Started" : "Finished";
                        if (x.Error != null)
                            msg = $"ERROR - {x.Error}";

                        msg += $" @ {x.Timestamp:G}";
                        return new CommandItem
                        {
                            Text = $"{x.JobName} ({x.JobType})",
                            Detail = msg
                        };
                    })
                    .ToList();
            });
            this.BindBusyCommand(this.Load);
        }


        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Purge { get; }
        [Reactive] public List<CommandItem> Logs { get; private set; }


        public override void OnAppearing()
        {
            this.Load.Execute();
            this.jobManager.JobStarted += this.OnJobStarted;
            this.jobManager.JobFinished += this.OnJobFinished;
        }


        public override void OnDisappearing()
        {
            this.jobManager.JobStarted -= this.OnJobStarted;
            this.jobManager.JobFinished -= this.OnJobFinished;
        }


        void OnJobStarted(object sender, JobInfo job) => this.Load.Execute();
        void OnJobFinished(object sender, JobRunResult job) => this.Load.Execute();
    }
}

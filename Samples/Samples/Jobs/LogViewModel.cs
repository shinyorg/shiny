using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Shiny.Jobs;
using Acr.UserDialogs;
using Samples.Models;
using Samples.Infrastructure;


namespace Samples.Jobs
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly IJobManager jobManager;
        readonly SampleSqliteConnection conn;

        public LogViewModel(IJobManager jobManager,
                            IUserDialogs dialogs,
                            SampleSqliteConnection conn) : base(dialogs)
        {
            this.jobManager = jobManager;
            this.conn = conn;
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.jobManager.JobStarted += this.OnJobStarted;
            this.jobManager.JobFinished += this.OnJobFinished;
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.jobManager.JobStarted -= this.OnJobStarted;
            this.jobManager.JobFinished -= this.OnJobFinished;
        }


        void OnJobStarted(object sender, JobInfo job) => this.Load.Execute();
        void OnJobFinished(object sender, JobRunResult job) => this.Load.Execute();

        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var logs = await this.conn.JobLogs.ToListAsync();
            return logs.Select(x =>
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
            });
        }

        protected override Task ClearLogs() => this.conn.DeleteAllAsync<JobLog>();
    }
}

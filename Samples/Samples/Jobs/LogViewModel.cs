using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Shiny.Jobs;
using Acr.UserDialogs;
using Samples.Models;
using Samples.Infrastructure;
using Shiny.Infrastructure;
using ReactiveUI;
using Shiny;

namespace Samples.Jobs
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly IJobManager jobManager;
        readonly SampleSqliteConnection conn;
        readonly ISerializer serializer;

        public LogViewModel(IJobManager jobManager,
                            IUserDialogs dialogs,
                            ISerializer serializer,
                            SampleSqliteConnection conn) : base(dialogs)
        {
            this.jobManager = jobManager;
            this.serializer = serializer;
            this.conn = conn;
        }


        protected override void OnStart()
        {
            base.OnStart();
            this.jobManager.JobStarted += this.OnJobStarted;
            this.jobManager.JobFinished += this.OnJobFinished;
        }


        public override void Destroy()
        {
            base.Destroy();
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
                var title = $"{x.JobName} ({x.JobType})";
                var msg = x.Started ? "Started" : "Finished";
                if (x.Error != null)
                    msg = $"ERROR - {x.Error}";

                msg += $" @ {x.Timestamp:G}";

                return new CommandItem
                {
                    Text = title,
                    Detail = msg,
                    PrimaryCommand = ReactiveCommand.Create(() =>
                    {
                        var s = $"{msg}{Environment.NewLine}";
                        if (!x.Parameters.IsEmpty())
                        {
                            var parameters = this.serializer.Deserialize<Dictionary<string, object>>(x.Parameters);
                            foreach (var p in parameters)
                                s += $"{Environment.NewLine}{p.Key}: {p.Value}";
                        }
                        this.Dialogs.Alert(s, title);
                    })
                };
            });
        }

        protected override Task ClearLogs() => this.conn.DeleteAllAsync<JobLog>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Prism.Navigation;
using Shiny.Jobs;
using Shiny;
using Samples.Infrastructure;


namespace Samples.Jobs
{
    public class ListViewModel : ViewModel
    {
        readonly IJobManager jobManager;
        readonly IDialogs dialogs;


        public ListViewModel(IJobManager jobManager,
                             INavigationService navigator,
                             IDialogs dialogs)
        {
            this.jobManager = jobManager;
            this.dialogs = dialogs;

            this.Create = navigator.NavigateCommand("CreateJob");

            this.LoadJobs = ReactiveCommand.CreateFromTask(async () =>
            {
                var jobs = await jobManager.GetJobs();

                this.Jobs = jobs
                    .Select(x => new CommandItem
                    {
                        Text = x.Identifier,
                        Detail = x.LastRunUtc?.ToLocalTime().ToString("G") ?? "Never Run",
                        PrimaryCommand = ReactiveCommand.CreateFromTask(() => jobManager.Run(x.Identifier)),
                        SecondaryCommand = ReactiveCommand.CreateFromTask(async () =>
                        {
                            await jobManager.Cancel(x.Identifier);
                            this.LoadJobs.Execute(null);
                        })
                    })
                    .ToList();
            });
            this.BindBusyCommand(this.LoadJobs);

            this.RunAllJobs = ReactiveCommand.CreateFromTask(async () =>
            {
                if (!await this.AssertJobs())
                    return;

                if (this.jobManager.IsRunning)
                {
                    await dialogs.Alert("Job Manager is already running");
                }
                else
                {
                    await this.jobManager.RunAll();
                    await dialogs.Snackbar("Job Batch Started");
                }
            });

            this.CancelAllJobs = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (!await this.AssertJobs())
                    return;

                var confirm = await dialogs.Confirm("Are you sure you wish to cancel all jobs?");
                if (confirm)
                {
                    await this.jobManager.CancelAll();
                    this.LoadJobs.Execute(null);
                }
            });
        }


        public ICommand LoadJobs { get; }
        public ICommand CancelAllJobs { get; }
        public ICommand RunAllJobs { get; }
        public ICommand Create { get; }
        [Reactive] public List<CommandItem> Jobs { get; private set; }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadJobs.Execute(null);

            this.jobManager
                .JobStarted
                .Subscribe(x =>
                {
                    this.dialogs.Snackbar($"Job {x.Identifier} Started");
                    this.LoadJobs.Execute(null);
                })
                .DisposedBy(this.DeactivateWith);

            this.jobManager
                .JobFinished
                .Subscribe(x =>
                {
                    this.dialogs.Snackbar($"Job {x.Job?.Identifier} Finished");
                    this.LoadJobs.Execute(null);
                })
                .DisposedBy(this.DeactivateWith);
        }


        async Task<bool> AssertJobs()
        {
            var jobs = await this.jobManager.GetJobs();
            if (!jobs.Any())
            {
                await this.dialogs.Alert("There are no jobs");
                return false;
            }

            return true;
        }
    }
}

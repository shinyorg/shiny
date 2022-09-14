using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows.Input;
using Shiny.Jobs;
using Shiny;
using Xamarin.Forms;


namespace Sample
{
    public class ListViewModel : SampleViewModel
    {
        readonly IJobManager jobManager;
        CompositeDisposable? disposer;


        public ListViewModel()
        {
            this.jobManager = ShinyHost.Resolve<IJobManager>();

            this.Create = new Command(
                async () => await this.Navigation.PushAsync(new CreatePage())
            );

            this.LoadJobs = new Command(async () =>
            {
                this.Jobs = (await jobManager.GetJobs()).ToList();
                this.RaisePropertyChanged(nameof(this.Jobs));
            });

            this.RunAllJobs = new Command(async () =>
            {
                if (!await this.AssertJobs())
                    return;

                if (this.jobManager.IsRunning)
                {
                    await this.Alert("Job Manager is already running");
                }
                else
                {
                    await this.jobManager.RunAll();
                    this.RunningText = "Job Batch Started";
                }
            });

            this.CancelAllJobs = new Command(async _ =>
            {
                if (!await this.AssertJobs())
                    return;

                var confirm = await this.Confirm("Are you sure you wish to cancel all jobs?");
                if (confirm)
                {
                    await this.jobManager.CancelAll();
                    this.LoadJobs.Execute(null);
                }
            });
        }


        string runningText;
        public string RunningText
        {
            get => this.runningText;
            private set => this.Set(ref this.runningText, value);
        }
        public ICommand LoadJobs { get; }
        public ICommand CancelAllJobs { get; }
        public ICommand RunAllJobs { get; }
        public ICommand Create { get; }
        public List<JobInfo> Jobs { get; private set; }


        JobInfo? jobInfo;
        public JobInfo? SelectedJob
        {
            get => this.jobInfo;
            set
            {
                this.jobInfo = value;
                this.RaisePropertyChanged();
            }
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadJobs.Execute(null);

            this.disposer = new CompositeDisposable();

            this.jobManager
                .JobStarted
                .SubOnMainThread(x =>
                {
                    this.RunningText = $"{x.Identifier} Running";
                    this.LoadJobs.Execute(null);
                })
                .DisposedBy(this.disposer);

            this.jobManager
                .JobFinished
                .Subscribe(x =>
                {
                    var status = x.Success ? "Completed" : "Failed";
                    this.RunningText = $"{x.Job?.Identifier} {status}";
                    this.LoadJobs.Execute(null);
                })
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.SelectedJob)
                .Where(x => x != null)
                .SubscribeAsync(async x =>
                {
                    this.SelectedJob = null;
                    await this.jobManager.Run(x.Identifier);
                })
                .DisposedBy(this.disposer);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.disposer?.Dispose();
        }


        async Task<bool> AssertJobs()
        {
            var jobs = await this.jobManager.GetJobs();
            if (!jobs.Any())
            {
                await this.Alert("There are no jobs");
                return false;
            }

            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Push;


namespace Samples.Push
{
    public class TagsViewModel : ViewModel
    {
        readonly IPushManager push;
        readonly IDialogs dialogs;


        public TagsViewModel(IPushManager pushManager, IDialogs dialogs)
        {
            this.push = pushManager;
            this.dialogs = dialogs;

            var tags = pushManager as IPushTagSupport;
            var requirement = this.WhenAny(
                x => x.IsSupported,
                x => x.GetValue()
            );


            this.Add = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var result = await dialogs.Input("Name of tag?");
                    if (!result.IsEmpty())
                    {
                        await tags.AddTag(result);
                        this.Load.Execute(null);
                    }
                },
                requirement
            );

            this.Clear = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var result = await dialogs.Confirm("Are you sure you wish to clear all tags?");
                    if (result)
                    {
                        await tags!.ClearTags();
                        this.Load.Execute(null);
                    }
                },
                requirement
            );

            this.Load = ReactiveCommand.Create(
                () =>
                {
                    var regTags = tags!.RegisteredTags ?? new string[0];

                    this.Tags = regTags
                        .Select(tag => new CommandItem
                        {
                            Text = tag,
                            PrimaryCommand = ReactiveCommand.CreateFromTask(async () =>
                            {
                                var result = await dialogs.Confirm($"Are you sure you wish to remove tag '{tag}'?");
                                if (result)
                                {
                                    await tags.RemoveTag(tag);
                                    this.Load.Execute(null);
                                }
                            })
                        })
                        .ToList();

                    this.RaisePropertyChanged(nameof(this.Tags));
                },
                this.WhenAny(
                    x => x.IsSupported,
                    x => x.GetValue()
                )
            );
        }


        public ICommand Load { get; }
        public ICommand Add { get; }
        public ICommand Clear { get; }        
        public IList<CommandItem> Tags { get; private set; }
        [Reactive] public bool IsSupported { get; private set; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            if (this.push is not IPushTagSupport)
                await this.dialogs.Alert("Tags not supported");
            else if (this.push.CurrentRegistrationToken == null)
                await this.dialogs.Alert("Not Registered");
            else
            {
                this.IsSupported = true;
                this.Load.Execute(null);
            }
        }
    }
}

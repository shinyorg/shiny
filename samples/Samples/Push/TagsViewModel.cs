using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ReactiveUI;
using Samples.Infrastructure;
using Shiny;
using Shiny.Push;


namespace Samples.Push
{
    public class TagsViewModel : ViewModel
    {
        public TagsViewModel(IPushManager pushManager, IDialogs dialogs)
        {
            var tags = pushManager as IPushTagSupport;

            this.Add = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dialogs.Input("Name of tag?");
                if (!result.IsEmpty())
                {
                    await tags.AddTag(result);
                    this.Load.Execute(null);
                }
            });

            this.Clear = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await dialogs.Confirm("Are you sure you wish to clear all tags?");
                if (result)
                {
                    await tags.ClearTags();
                    this.Load.Execute(null);
                }
            });

            this.Load = ReactiveCommand.Create(() =>
            {
                this.Tags = tags
                    .RegisteredTags
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
            });
        }


        public ICommand Load { get; }
        public ICommand Add { get; }
        public ICommand Clear { get; }
        public IList<CommandItem> Tags { get; private set; }
    }
}

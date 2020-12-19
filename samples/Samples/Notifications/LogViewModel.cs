using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Samples.Infrastructure;
using Samples.Models;
using Shiny;
using MsgBus = Shiny.IMessageBus;


namespace Samples.Notifications
{
    public class LogViewModel : AbstractLogViewModel<CommandItem>
    {
        readonly SampleSqliteConnection conn;
        readonly MsgBus messageBus;


        public LogViewModel(SampleSqliteConnection conn,
                            MsgBus messageBus,
                            IDialogs dialogs) : base(dialogs)
        {
            this.conn = conn;
            this.messageBus = messageBus;
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.messageBus
                .Listener<NotificationEvent>()
                .Select(_ => Unit.Default)
                .InvokeCommand(this.Load)
                .DisposedBy(this.DeactivateWith);
        }

        protected override Task ClearLogs() => this.conn.DeleteAllAsync<NotificationEvent>();


        protected override async Task<IEnumerable<CommandItem>> LoadLogs()
        {
            var events = await conn
                .NotificationEvents
                .OrderByDescending(x => x.Timestamp)
                .ToListAsync();

            return events.Select(x =>
            {
                var mode = x.IsEntry ? "Entry" : "Received";
                var detail = $"[{mode}] {x.Timestamp}";
                if (!x.Action.IsEmpty())
                    detail += $" - {x.Action}";

                if (!x.ReplyText.IsEmpty())
                    detail += $" - {x.ReplyText}";

                return new CommandItem
                {
                    Text = $"({x.NotificationId}) {x.NotificationTitle}",
                    Detail = detail
                };
            });
        }
    }
}

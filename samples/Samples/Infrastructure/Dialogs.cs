using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XF.Material.Forms.UI.Dialogs;


namespace Samples.Infrastructure
{
    public interface IDialogs
    {
        Task Alert(string message, string title = "Error");
        Task ActionSheet(string title, bool allowCancel, params (string Key, Action Action)[] actions);
        Task ActionSheet(string title, IDictionary<string, Action> actions, bool allowCancel = false);
        Task<bool> Confirm(string message, string title = "Confirm", string okText = "OK", string cancelText = "Cancel");
        Task<string> Input(string question, string? title = null);
        Task Snackbar(string message);
        Task<T> LoadingTask<T>(Func<Task<T>> task, string message);
        Task LoadingTask(Func<Task> task, string message = "Loading");
    }


    public class Dialogs : IDialogs
    {
        public Task Alert(string message, string title = "Confirm")
            => MaterialDialog.Instance.AlertAsync(message, title);


        public async Task<bool> Confirm(string message, string title = "Confirm", string okText = "OK", string cancelText = "Cancel")
        {
            var result = await MaterialDialog.Instance.ConfirmAsync(message, title, okText, cancelText);
            return result ?? false;
        }


        public Task<string> Input(string message, string? title = null)
            => MaterialDialog.Instance.InputAsync(title, message);


        public Task ActionSheet(string title, bool allowCancel, params (string Key, Action Action)[] actions)
        {
            var dict = actions.ToDictionary(
                x => x.Key,
                x => x.Action
            );
            return this.ActionSheet(title, dict, allowCancel);
        }


        public async Task ActionSheet(string title, IDictionary<string, Action> actions, bool allowCancel = false)
        {
            var task = allowCancel
                ? await MaterialDialog.Instance.SelectChoiceAsync(title, actions.Keys.ToList())
                : await MaterialDialog.Instance.SelectActionAsync(title, actions.Keys.ToList());

            if (task >= 0)
                actions.Values.ElementAt(task).Invoke();
        }


        public async Task<T> LoadingTask<T>(Func<Task<T>> task, string message)
        {
            var result = default(T);
            IMaterialModalPage dialog = null;
            try
            {
                dialog = await MaterialDialog.Instance.LoadingDialogAsync(message);
                result = await task();
                await dialog.DismissAsync();
            }
            catch
            {
                await dialog?.DismissAsync();
                throw;
            }
            return result;
        }


        public Task LoadingTask(Func<Task> task, string message = "Loading")
            => this.LoadingTask<object>(async () =>
                {
                    await task();
                    return null;
                }, 
                message
             );


        public Task Snackbar(string message)
            => MaterialDialog.Instance.SnackbarAsync(message);
    }
}

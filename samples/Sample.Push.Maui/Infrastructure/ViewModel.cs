namespace Sample;


public abstract class ViewModel : Shiny.NotifyPropertyChanged
{
    public virtual void OnNavigatedTo() { }
    public virtual void OnNavigatedFrom() { }


    protected virtual Command LoadingCommand(Func<Task> taskFunc) => new Command(async () => await this.Loading(taskFunc));

    protected virtual async Task Loading(Func<Task> taskFunc)
    {
        this.IsBusy = true;
        try
        {
            await taskFunc.Invoke().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Write(ex);
            await this.Alert("Error");
        }
        finally
        {
            this.IsBusy = false;
        }
    }


    protected virtual ICommand ConfirmCommand(string question, Func<Task> taskFunc) => new Command(async () =>
    {
        var result = await this.Confirm(question);
        if (result)
            await taskFunc();
    });


    protected virtual Task Alert(string message, string title = "Info")
        => Device.InvokeOnMainThreadAsync(() => App.Current.MainPage.DisplayAlert(title, message, "OK"));

    protected virtual Task<bool> Confirm(string question, string title = "Question")
        => Device.InvokeOnMainThreadAsync(() => App.Current.MainPage.DisplayAlert(title, question, "Ok", "Cancel"));

    protected virtual Task<string> Prompt(string question)
        => App.Current.MainPage.DisplayPromptAsync("Question", question);


    bool isBusy;
    public bool IsBusy
    {
        get => this.isBusy;
        set => this.Set(ref this.isBusy, value);
    }
}

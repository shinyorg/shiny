using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Calendar;

namespace Sample.Shared.Maui.Pages.Calendar;

[ShellMap<CalendarListPage>("calendar")]
public partial class CalendarListViewModel(
    ICalendarStore calendarStore,
    INavigator navigator,
    IDialogs dialogs
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    bool isRefreshing;

    public List<Shiny.Calendar.Calendar> Calendars
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = [];

    public List<CalendarEvent> Events
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = [];

    [RelayCommand]
    async Task Load()
    {
        try
        {
            var access = await calendarStore.RequestAccess(CalendarAccessType.ReadWrite);
            if (access != AccessState.Available)
            {
                await dialogs.Alert("FAIL", $"Calendar access not granted ({access})", "OK");
                return;
            }
            IsRefreshing = true;

            Calendars = (await calendarStore.GetAll()).ToList();

            // Upcoming events across all calendars for the next 30 days.
            var now = DateTimeOffset.Now;
            Events = calendarStore
                .Query()
                .Where(e => e.Start >= now && e.End <= now.AddDays(30))
                .OrderBy(e => e.Start)
                .ToList();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    Task AddEvent() => navigator.NavigateTo<CalendarEditViewModel>();

    [RelayCommand]
    async Task EditEvent(CalendarEvent calendarEvent)
    {
        if (calendarEvent?.Id == null) return;
        await navigator.NavigateTo<CalendarEditViewModel>(vm => vm.EventId = calendarEvent.Id);
    }

    [RelayCommand]
    async Task DeleteEvent(CalendarEvent calendarEvent)
    {
        if (calendarEvent?.Id == null) return;

        var confirm = await dialogs.Confirm(
            "Delete Event",
            $"Delete \"{calendarEvent.Title}\"?",
            "Delete", "Cancel");

        if (!confirm) return;

        try
        {
            await calendarStore.DeleteEvent(calendarEvent.Id);
            await this.Load();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
    }

    public void OnAppearing() => _ = this.Load();
    public void OnDisappearing() { }
}

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

    /// <summary>
    /// The calendar pill currently tapped in the top strip. The "All" sentinel has a null Id, which
    /// means "don't filter by calendar".
    /// </summary>
    public Shiny.Calendar.Calendar? SelectedCalendar
    {
        get;
        set
        {
            if (ReferenceEquals(field, value))
                return;

            field = value;
            this.OnPropertyChanged();
            _ = this.LoadEvents();
        }
    }

    static Shiny.Calendar.Calendar CreateAllPill() => new() { Name = "All" };

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

            var cals = (await calendarStore.GetAll()).ToList();
            cals.Insert(0, CreateAllPill());
            Calendars = cals;

            // GetAll hands back fresh instances every time, so the previously selected pill is a dead
            // reference after a refresh - re-resolve it by Id and fall back to "All". Assign the field
            // directly so the setter doesn't kick off a second event load.
            this.selectedCalendar = cals.FirstOrDefault(c => c.Id == this.selectedCalendar?.Id) ?? cals[0];
            OnPropertyChanged(nameof(SelectedCalendar));

            await this.LoadEvents();
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

    async Task LoadEvents()
    {
        try
        {
            IsRefreshing = true;

            // Upcoming events for the next 30 days. Both the window and the calendar id are pushed
            // down to the native fetch by the query provider.
            var now = DateTimeOffset.Now;
            var query = calendarStore
                .Query()
                .Where(e => e.Start >= now && e.End <= now.AddDays(30));

            var calendarId = this.selectedCalendar?.Id;
            if (calendarId != null)
                query = query.Where(e => e.CalendarId == calendarId);

            Events = query.OrderBy(e => e.Start).ToList();
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

    const string DeleteThisEvent = "Delete This Event";
    const string DeleteAllFuture = "Delete All Future Events";

    [RelayCommand]
    async Task DeleteEvent(CalendarEvent calendarEvent)
    {
        if (calendarEvent?.Id == null) return;

        // A recurring event needs the occurrence-vs-series choice; a one-off just needs a yes/no.
        bool deleteSeries;
        if (calendarEvent.IsRecurring)
        {
            var choice = await dialogs.ActionSheet(
                $"Delete \"{calendarEvent.Title}\"?",
                "Cancel",
                DeleteAllFuture,
                DeleteThisEvent);

            if (choice != DeleteThisEvent && choice != DeleteAllFuture)
                return;

            deleteSeries = choice == DeleteAllFuture;
        }
        else
        {
            var confirm = await dialogs.Confirm(
                "Delete Event",
                $"Delete \"{calendarEvent.Title}\"?",
                "Delete", "Cancel");

            if (!confirm) return;
            deleteSeries = false;
        }

        try
        {
            await calendarStore.DeleteEvent(calendarEvent.Id, deleteSeries);
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

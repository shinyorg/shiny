using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiny;
using Shiny.Calendar;

namespace Sample.Shared.Maui.Pages.Calendar;

[ShellMap<CalendarEditPage>]
public partial class CalendarEditViewModel(
    ICalendarStore calendarStore,
    INavigator navigator,
    IDialogs dialogs
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string? eventId;
    [ObservableProperty] bool isNew = true;
    [ObservableProperty] bool isBusy;

    [ObservableProperty] string title = string.Empty;
    [ObservableProperty] string location = string.Empty;
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] bool isAllDay;
    [ObservableProperty] DateTime startDate = DateTime.Today;
    [ObservableProperty] TimeSpan startTime = DateTime.Now.TimeOfDay;
    [ObservableProperty] DateTime endDate = DateTime.Today;
    [ObservableProperty] TimeSpan endTime = DateTime.Now.AddHours(1).TimeOfDay;

    public List<Shiny.Calendar.Calendar> Calendars
    {
        get;
        private set { field = value; OnPropertyChanged(); }
    } = [];

    [ObservableProperty] Shiny.Calendar.Calendar? selectedCalendar;

    public string PageTitle => IsNew ? "Add Event" : "Edit Event";

    partial void OnEventIdChanged(string? value) => IsNew = string.IsNullOrEmpty(value);

    public async void OnAppearing()
    {
        try
        {
            IsBusy = true;
            Calendars = (await calendarStore.GetAll()).Where(c => !c.IsReadOnly).ToList();

            if (!IsNew && EventId != null)
            {
                var evt = await calendarStore.GetEvent(EventId);
                if (evt == null)
                {
                    await dialogs.Alert("Error", "Event not found", "OK");
                    await navigator.GoBack();
                    return;
                }

                Title = evt.Title;
                Location = evt.Location ?? string.Empty;
                Description = evt.Description ?? string.Empty;
                IsAllDay = evt.IsAllDay;
                StartDate = evt.Start.LocalDateTime.Date;
                StartTime = evt.Start.LocalDateTime.TimeOfDay;
                EndDate = evt.End.LocalDateTime.Date;
                EndTime = evt.End.LocalDateTime.TimeOfDay;
                SelectedCalendar = Calendars.FirstOrDefault(c => c.Id == evt.CalendarId);
            }

            SelectedCalendar ??= Calendars.FirstOrDefault();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void OnDisappearing() { }

    [RelayCommand]
    async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            await dialogs.Alert("Validation", "Please enter a title.", "OK");
            return;
        }

        var start = StartDate.Date + (IsAllDay ? TimeSpan.Zero : StartTime);
        var end = EndDate.Date + (IsAllDay ? TimeSpan.FromDays(1) : EndTime);
        if (end < start)
        {
            await dialogs.Alert("Validation", "End must be on or after start.", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            var evt = new CalendarEvent
            {
                Id = EventId,
                CalendarId = SelectedCalendar?.Id,
                Title = Title.Trim(),
                Location = NullIfEmpty(Location),
                Description = NullIfEmpty(Description),
                IsAllDay = IsAllDay,
                Start = new DateTimeOffset(start),
                End = new DateTimeOffset(end)
            };

            if (IsNew)
                await calendarStore.CreateEvent(evt);
            else
                await calendarStore.UpdateEvent(evt);

            await navigator.GoBack();
        }
        catch (Exception ex)
        {
            await dialogs.Alert("Error", ex.ToString(), "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    Task Cancel() => navigator.GoBack();

    static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

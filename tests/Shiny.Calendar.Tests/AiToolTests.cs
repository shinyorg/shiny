using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Calendar;
using Shiny.Calendar.Extensions.AI;
using Xunit;

namespace Shiny.Calendar.Tests;

public class AiToolTests
{
    static readonly DateTimeOffset Base = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    static (FakeCalendarStore Store, IReadOnlyList<AITool> Tools) Build(CalendarAICapabilities caps)
    {
        var store = new FakeCalendarStore();
        store.SeedCalendars(new Calendar("work", "Work"), new Calendar("personal", "Personal"));
        store.Seed(
            new CalendarEvent("Standup", Base, Base.AddMinutes(30)) { Id = "1", CalendarId = "work", Location = "Room 3" },
            new CalendarEvent("Lunch", Base.AddHours(3), Base.AddHours(4)) { Id = "2", CalendarId = "personal" }
        );
        var services = new ServiceCollection();
        services.AddSingleton<ICalendarStore>(store);
        services.AddCalendarAITools(b => b.AddCalendar(caps));
        var sp = services.BuildServiceProvider();
        var tools = sp.GetRequiredService<CalendarAITools>().Tools;
        return (store, tools);
    }

    static AIFunction Tool(IReadOnlyList<AITool> tools, string name)
        => (AIFunction)tools.First(t => ((AIFunction)t).Name == name);

    static async Task<JsonElement> Invoke(AIFunction fn, params (string Key, object? Value)[] args)
    {
        var arguments = new AIFunctionArguments();
        foreach (var (k, v) in args)
            arguments[k] = v;
        var result = await fn.InvokeAsync(arguments, CancellationToken.None);
        // Normalise to a JsonElement for easy assertions.
        return JsonSerializer.SerializeToElement(result);
    }

    [Fact]
    public void Read_Only_Exposes_Read_Tools()
    {
        var (_, tools) = Build(CalendarAICapabilities.Read);
        var names = tools.Select(t => ((AIFunction)t).Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "get_event", "list_calendars", "search_events" }, names);
    }

    [Fact]
    public void Read_Plus_Create_Adds_Only_Create()
    {
        var (_, tools) = Build(CalendarAICapabilities.Read | CalendarAICapabilities.Create);
        var names = tools.Select(t => ((AIFunction)t).Name).ToHashSet();
        Assert.Contains("create_event", names);
        Assert.DoesNotContain("update_event", names);
        Assert.DoesNotContain("delete_event", names);
    }

    [Fact]
    public void All_Exposes_Every_Tool()
    {
        var (_, tools) = Build(CalendarAICapabilities.All);
        Assert.Equal(6, tools.Count);
    }

    [Fact]
    public void Empty_Registration_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICalendarStore>(new FakeCalendarStore());
        Assert.Throws<InvalidOperationException>(() =>
            services.AddCalendarAITools(b => { /* nothing added */ }));
    }

    [Fact]
    public void Schemas_Are_Valid_Json_Objects()
    {
        var (_, tools) = Build(CalendarAICapabilities.All);
        foreach (var t in tools.Cast<AIFunction>())
            Assert.Equal(JsonValueKind.Object, t.JsonSchema.ValueKind);

        // create_event must declare its required fields
        var create = Tool(tools, "create_event");
        var required = create.JsonSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("title", required);
        Assert.Contains("start", required);
        Assert.Contains("end", required);
    }

    [Fact]
    public async Task ListCalendars_Returns_Seeded_Calendars()
    {
        var (_, tools) = Build(CalendarAICapabilities.Read);
        var json = await Invoke(Tool(tools, "list_calendars"));
        Assert.Equal(2, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task SearchEvents_FreeText_Filters()
    {
        var (_, tools) = Build(CalendarAICapabilities.Read);
        var json = await Invoke(Tool(tools, "search_events"), ("query", "Standup"));
        Assert.Equal(1, json.GetProperty("count").GetInt32());
        Assert.Equal("Standup", json.GetProperty("events")[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateEvent_Persists_And_Returns_Id()
    {
        var (store, tools) = Build(CalendarAICapabilities.Create);
        var json = await Invoke(Tool(tools, "create_event"),
            ("title", "New Meeting"),
            ("start", "2026-07-10T10:00:00Z"),
            ("end", "2026-07-10T11:00:00Z"),
            ("calendarId", "work"));

        Assert.True(json.GetProperty("success").GetBoolean());
        var id = json.GetProperty("id").GetString()!;
        var saved = await store.GetEvent(id);
        Assert.NotNull(saved);
        Assert.Equal("New Meeting", saved!.Title);
    }

    [Fact]
    public async Task CreateEvent_Missing_Required_Returns_Error()
    {
        var (_, tools) = Build(CalendarAICapabilities.Create);
        var json = await Invoke(Tool(tools, "create_event"), ("title", "No dates"));
        Assert.True(json.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task UpdateEvent_Changes_Only_Supplied_Fields()
    {
        var (store, tools) = Build(CalendarAICapabilities.Update);
        var json = await Invoke(Tool(tools, "update_event"),
            ("eventId", "1"),
            ("location", "Room 9"));

        Assert.True(json.GetProperty("success").GetBoolean());
        var updated = await store.GetEvent("1");
        Assert.Equal("Room 9", updated!.Location);
        Assert.Equal("Standup", updated.Title); // untouched
    }

    [Fact]
    public async Task DeleteEvent_Removes_It()
    {
        var (store, tools) = Build(CalendarAICapabilities.Delete);
        var json = await Invoke(Tool(tools, "delete_event"), ("eventId", "2"));
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Null(await store.GetEvent("2"));
    }

    [Fact]
    public async Task DeleteEvent_Defaults_To_Single_Occurrence()
    {
        var (store, tools) = Build(CalendarAICapabilities.Delete);
        var json = await Invoke(Tool(tools, "delete_event"), ("eventId", "2"));

        Assert.False(json.GetProperty("deletedSeries").GetBoolean());
        Assert.False(store.LastDeleteSeries);
    }

    [Fact]
    public async Task DeleteEvent_Passes_DeleteSeries_Through()
    {
        var (store, tools) = Build(CalendarAICapabilities.Delete);
        var json = await Invoke(Tool(tools, "delete_event"), ("eventId", "2"), ("deleteSeries", true));

        Assert.True(json.GetProperty("deletedSeries").GetBoolean());
        Assert.True(store.LastDeleteSeries);
    }
}

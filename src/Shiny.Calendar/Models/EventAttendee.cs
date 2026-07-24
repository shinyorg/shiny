namespace Shiny.Calendar;

/// <summary>
/// A participant on a calendar event. Native calendar stores expose attendees as name + email + status
/// (not contact references); use <c>EventAttendee.ResolveContact(IContactStore)</c> to look up the
/// matching device contact by email.
/// </summary>
public class EventAttendee
{
    public EventAttendee() { }

    public EventAttendee(string? name, string? email, AttendeeRole role = AttendeeRole.Required, AttendeeStatus status = AttendeeStatus.Unknown)
    {
        this.Name = name;
        this.Email = email;
        this.Role = role;
        this.Status = status;
    }

    /// <summary>The attendee's display name, when available.</summary>
    public string? Name { get; set; }

    /// <summary>The attendee's email address. This is the join key back to a device contact.</summary>
    public string? Email { get; set; }

    /// <summary>Whether the attendee is required, optional, or a resource (room/equipment).</summary>
    public AttendeeRole Role { get; set; } = AttendeeRole.Required;

    /// <summary>The attendee's response status for the invitation.</summary>
    public AttendeeStatus Status { get; set; } = AttendeeStatus.Unknown;

    /// <summary>True when this attendee is the event organizer.</summary>
    public bool IsOrganizer { get; set; }

    public override string ToString() => this.Name ?? this.Email ?? string.Empty;
}

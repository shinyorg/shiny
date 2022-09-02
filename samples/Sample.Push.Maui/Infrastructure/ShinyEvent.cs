using SQLite;

namespace Sample;


public class ShinyEvent
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }
    public string Text { get; set; }
    public string Detail { get; set; }
    public DateTime Timestamp { get; set; }
}

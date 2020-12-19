using System;
using SQLite;

namespace Samples.Models
{
    public class NotificationEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public string? NotificationTitle { get; set; }
        public string? Action { get; set; }
        public string? ReplyText { get; set; }
        public string? Payload { get; set; }
        public bool IsEntry { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

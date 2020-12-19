using System;
using SQLite;


namespace Samples.Models
{
    public class HttpEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public string Uri { get; set; }
        public bool IsUpload { get; set; }
        public long FileSize { get; set; }
        public string Identifier { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
    }
}

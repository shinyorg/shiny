using System;
using SQLite;


namespace Shiny.Models
{
    public class CacheStore
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Blob { get; set; }
        public string TypeName { get; set; }
        public DateTime ExpiryDateUtc { get; set; }
    }
}

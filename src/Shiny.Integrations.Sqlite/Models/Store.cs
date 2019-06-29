using System;
using SQLite;


namespace Shiny.Models
{
    public class Store
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string TypeName { get; set; }
        public string Key { get; set; }
    }
}

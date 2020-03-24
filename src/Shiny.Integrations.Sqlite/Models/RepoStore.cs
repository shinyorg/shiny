using System;
using SQLite;


namespace Shiny.Models
{
    public class RepoStore
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public string? TypeName { get; set; }
        public string? Key { get; set; }
        public string? Blob { get; set; }
    }
}

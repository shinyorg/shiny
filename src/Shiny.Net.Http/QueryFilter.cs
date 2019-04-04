using System;
using System.Collections.Generic;

namespace Shiny.Net.Http
{
    public class QueryFilter
    {
        public List<string> Ids { get; set; } = new List<string>();
        public DirectionFilter Direction { get; set; } = DirectionFilter.Both;
        public List<HttpTransferState> States { get; set; } = new List<HttpTransferState>();
    }
}

using System;
using System.Windows.Input;


namespace Sample
{
    public class ListItemViewModel
    {
        public bool IsBot { get; set; }
        public string? From { get; set; }
        public string? Text { get; set; }
        public ICommand? Command { get; set; }
    }
}

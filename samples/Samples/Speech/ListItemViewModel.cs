using System;
using Xamarin.Forms;


namespace Samples.Speech
{
    public class ListItemViewModel
    {
        public bool IsBot { get; set; }
        public string From { get; set; }
        public string Text { get; set; }
        public Command Command { get; set; }
    }
}

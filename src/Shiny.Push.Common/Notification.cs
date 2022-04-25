namespace Shiny.Push
{
    public struct Notification
    {
        public Notification(string? title, string? message)
        {
            this.Title = title;
            this.Message = message;
        }


        public string? Title { get; }
        public string? Message { get; }
    }
}

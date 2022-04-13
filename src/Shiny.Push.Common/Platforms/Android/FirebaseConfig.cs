using System;


namespace Shiny.Push
{
    public class FirebaseConfig
    {
        public void AssertValid()
        {
            if (this.AppId == null)
                throw new ArgumentNullException(nameof(this.AppId));

            if (this.SenderId == null)
                throw new ArgumentNullException(nameof(this.SenderId));

            if (this.ApiKey == null)
                throw new ArgumentNullException(nameof(this.ApiKey));

            if (this.ProjectId == null)
                throw new ArgumentNullException(nameof(this.ProjectId));
        }


        public string? AppId { get; set; }
        public string? SenderId { get; set; }
        public string? ApiKey { get; set; }
        public string? ProjectId { get; set; }
    }
}

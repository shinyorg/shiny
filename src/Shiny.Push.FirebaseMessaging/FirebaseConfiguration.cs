using System;


namespace Shiny.Push
{
    public class FirebaseConfiguration
    {

        public void AssertValid()
        {
            if (this.UseEmbeddedConfiguration)
                return;

            if (this.AppId == null)
                throw new ArgumentNullException(nameof(this.AppId));

            if (this.SenderId == null)
                throw new ArgumentNullException(nameof(this.SenderId));

            if (this.ApiKey == null)
                throw new ArgumentNullException(nameof(this.ApiKey));

            if (this.ProjectId == null)
                throw new ArgumentNullException(nameof(this.ProjectId));
        }


        /// <summary>
        /// If you have included a GoogleService-Info.plist (iOS) or google-services.json (Android)
        /// </summary>
        public bool UseEmbeddedConfiguration { get; set; }
        public string AppId { get; set; }
        public string SenderId { get; set; }
        public string ProjectId { get; set; }
        public string ApiKey { get; set; }
    }
}

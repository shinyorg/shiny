using System;


namespace Shiny.MediaSync
{
    public class MediaAsset
    {
        public MediaAsset(string identifier, string filePath, MediaTypes mediaType)
        {
            this.Identifier = identifier;
            this.FilePath = filePath;
            this.Type = mediaType;
        }


        public string Identifier { get; }
        public string FilePath { get; }
        public MediaTypes Type { get; }
        //DateCreated
        //public DateTimeOffset DateModified { get; }
        // TODO: pixel height & width for photo
        // TODO: Duration for audio & video
        // TODO: location coordinates
        // TODO: user hidden
    }
}

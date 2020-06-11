using System;


namespace Shiny.PhotoSync
{
    public class Photo
    {
        public Photo(string filePath)
        {
            this.FilePath = filePath;
        }


        public string FilePath { get; }
    }
}

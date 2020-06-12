using System;


namespace Shiny.MediaSync
{
    public class Media
    {
        public Media(string filePath)
        {
            this.FilePath = filePath;
        }


        public string FilePath { get; }
        public bool IsVideo { get; }
    }
}

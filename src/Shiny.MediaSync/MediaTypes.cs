using System;


namespace Shiny.MediaSync
{
    [Flags]
    public enum MediaTypes
    {
        None = 0,
        Audio = 1,
        Image = 2,
        Video = 4
    }
}

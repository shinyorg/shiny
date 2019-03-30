using System;


namespace Shiny.Settings
{
    public static class Extensions
    {
        static readonly object syncLock = new object();


        /// <summary>
        /// Thread safetied setting value incrementor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int IncrementValue(this ISettings settings, string name = "NextId")
        {
            var id = 0;

            lock (syncLock)
            {
                id = settings.Get(name, 0);
                id++;
                settings.Set(name, id);
            }
            return id;
        }
    }
}

using System;
using Shiny.Settings;

namespace Shiny
{
    public static class SettingsExtensions
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


        /// <summary>
        /// Gets a required value from settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="settings"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetRequired<T>(this ISettings settings, string key)
        {
            if (!settings.Contains(key))
                throw new ArgumentException($"Settings key '{key}' is not set");

            return settings.Get<T>(key);
        }



        /// <summary>
        /// This will only set the value if the setting is not currently set.  Will not fire Changed event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool SetDefault<T>(this ISettings settings, string key, T value)
        {
            if (settings.Contains(key))
                return false;

            settings.Set(key, value);
            return true;
        }
    }
}

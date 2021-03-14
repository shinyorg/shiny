using System;
using Shiny.Stores;


namespace Shiny
{
    public static class StoreExtensions
    {
        static readonly object syncLock = new object();


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="store"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(this IKeyValueStore store, string key)
        {
            var obj = store.Get(typeof(T), key);
            if (obj == null)
                return default;

            return (T)obj;
        }


        /// <summary>
        /// Thread safetied setting value incrementor
        /// </summary>
        /// <param name="store"></param>
        /// <returns></returns>
        public static int IncrementValue(this IKeyValueStore store, string key = "NextId")
        {
            var id = 0;

            lock (syncLock)
            {
                id = store.Get<int>(key);
                id++;
                store.Set(key, id);
            }
            return id;
        }


        /// <summary>
        /// Gets a required value from settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="store"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetRequired<T>(this IKeyValueStore store, string key)
        {
            if (!store.Contains(key))
                throw new ArgumentException($"Store key '{key}' is not set");

            return store.Get<T>(key)!;
        }



        /// <summary>
        /// This will only set the value if the setting is not currently set.  Will not fire Changed event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static bool SetDefault<T>(this IKeyValueStore store, string key, T value)
        {
            if (store.Contains(key))
                return false;

            store.Set(key, value);
            return true;
        }
    }
}

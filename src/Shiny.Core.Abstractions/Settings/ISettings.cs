using System;
using System.ComponentModel;


namespace Shiny.Settings
{
    public interface ISettings
    {
        /// <summary>
        /// Monitor setting events (Add, Update, Remove, Clear)
        /// </summary>
		IObservable<SettingChange> Changed { get; }

        /// <summary>
        /// Gets an object from settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// Loosely typed version of Get
        /// </summary>
        /// <param name="type"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        object? GetValue(Type type, string key, object? defaultValue = null);

        /// <summary>
        /// Send any value/object to the settings store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set<T>(string key, T value);

        /// <summary>
        /// Loosely typed version of Set
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetValue(string key, object value);

        /// <summary>
        /// Remove the setting by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool Remove(string key);

        /// <summary>
        /// Checks if the setting key is set
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool Contains(string key);

        /// <summary>
        /// Clears all setting values
        /// </summary>
        void Clear();

        /// <summary>
        /// Will create a bound object set FROM the settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        T Bind<T>() where T : INotifyPropertyChanged, new();

        /// <summary>
        /// Bind to existing instance
        /// </summary>
        /// <param name="obj"></param>
        void Bind(INotifyPropertyChanged obj);

        /// <summary>
        /// Unbinds an object from monitoring
        /// </summary>
        /// <param name="obj"></param>
        void UnBind(INotifyPropertyChanged obj);
    }
}

using System;


namespace Shiny.Settings
{
    public enum SettingChangeAction
    {
        Add,
        Update,
        Remove,
        Clear
    }


    public class SettingChange
    {
        public SettingChange(SettingChangeAction action, string? key, object? value)
        {
            this.Action = action;
            this.Key = key;
            this.Value = value;
        }


        public SettingChangeAction Action { get; }
        public string? Key { get; }
        public object? Value { get; }
    }
}


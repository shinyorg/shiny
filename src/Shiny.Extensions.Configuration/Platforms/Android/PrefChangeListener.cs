using Android.Content;
using System;

namespace Shiny.Extensions.Configuration;


public class PrefChangeListener : Java.Lang.Object, ISharedPreferencesOnSharedPreferenceChangeListener
{
    readonly Action action;
    public PrefChangeListener(Action action) => this.action = action;


    public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
        => this.action();
}

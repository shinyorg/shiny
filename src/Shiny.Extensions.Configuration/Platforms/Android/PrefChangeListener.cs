using Android.Content;
using System;

namespace Shiny.Extensions.Configuration;


public class PrefChangeListener(Action action) : Java.Lang.Object, ISharedPreferencesOnSharedPreferenceChangeListener
{
    public void OnSharedPreferenceChanged(ISharedPreferences? sharedPreferences, string? key)
        => action();
}

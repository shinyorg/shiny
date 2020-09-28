using System;
using Android.App;
using Android.OS;


namespace Shiny
{
    public enum ActivityState
    {
        Created,
        Resumed,
        Paused,
        Destroyed,
		SaveInstanceState,
        Started,
        Stopped
    }


    public class ActivityChanged
    {
        public ActivityChanged(Activity activity, ActivityState state, Bundle? stateBundle)
        {
            this.Activity = activity;
            this.Status = state;
            this.Bundle = stateBundle;
        }


        public Activity Activity { get; }
        public ActivityState Status { get; }
        public Bundle? Bundle { get; }
    }
}

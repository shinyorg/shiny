Title: Platform - Android
Order: 3
Xref: android
---

Shiny integrates well with Android, but requires several hook points.  Unlike Xamarin Forms applications where you only need to hook into an activity, Shiny
requires an Android application.  From an architectural standpoint, this is to ensure all of the Shiny services can be spun up before an Android broadcast receiver 
can run. 


# TODO

```csharp
using System;
using Shiny;
using Android.App;
using Android.Content;
using Android.Runtime;


namespace Samples.Android
{
	[global::Android.App.ApplicationAttribute]
	public partial class MainApplication : global::Android.App.Application
	{
		public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}

		public override void OnCreate()
		{
			this.ShinyOnCreate(new Samples.SampleStartup());
			global::Xamarin.Essentials.Platform.Init(this);
			base.OnCreate();
		}
	}
}



using System;
using Shiny;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;


namespace Samples.Droid
{
	public partial class MainActivity
	{
		partial void OnPreCreate(Bundle savedInstanceState);		
        partial void OnPostCreate(Bundle savedInstanceState);		
        
        protected override void OnCreate(Bundle savedInstanceState)
		{
			this.ShinyOnCreate();
			this.OnPreCreate(savedInstanceState);
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;
			base.OnCreate(savedInstanceState);
			global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
			this.LoadApplication(new Samples.App());
			global::XF.Material.Droid.Material.Init(this, savedInstanceState);
			this.OnPostCreate(savedInstanceState);
		}

		protected override void OnNewIntent(Intent intent)
		{
			base.OnNewIntent(intent);
			this.ShinyOnNewIntent(intent);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			this.ShinyOnActivityResult(requestCode, resultCode, data);
		}
	
		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);
			global::Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
	}
}

```
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;


namespace Samples.Gtk
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            global::Gtk.Application.Init();
            Forms.Init();

            var app = new App();
            var window = new FormsWindow();
            window.LoadApplication(app);
            window.SetApplicationTitle("Samples");
            //window.SetApplicationIcon("icon.png");
            //GtkThemes.Init();
            //GtkThemes.LoadCustomTheme("Themes/gtkrc");
            window.Show();

            global::Gtk.Application.Run();
        }
    }
}
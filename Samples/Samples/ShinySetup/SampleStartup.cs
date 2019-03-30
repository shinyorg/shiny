using System;
using Shiny;
using Shiny.BluetoothLE;
using Shiny.Logging;
using Shiny.Locations;
using Shiny.Notifications;
using Shiny.Sensors;
using Shiny.SpeechRecognition;
using Shiny.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Samples.Settings;
using Samples.Logging;


namespace Samples.ShinySetup
{
    public class SampleStartup : Startup
    {
        public override void ConfigureServices(IServiceCollection builder)
        {
            Log.UseConsole();
            Log.UseDebug();
            Log.AddLogger(new AppCenterLogger(), true, true);
            Log.AddLogger(new DbLogger(), true, false);

            // create your infrastructures
            // jobs, connectivity, power, filesystem, are installed automatically
            builder.AddSingleton<SampleSqliteConnection>();

            // startup tasks
            builder.RegisterStartupTask<StartupTask1>();
            builder.RegisterStartupTask<StartupTask2>();
            builder.RegisterStartupTask<JobLoggerTask>();

            // configuration
            builder.RegisterSettings<AppSettings>("AppSettings");

            // register all of the acr stuff you want to use
            builder.UseHttpTransfers<SampleAllDelegate>();
            //builder.UseBeacons<SampleAllDelegate>();
            builder.UseBleCentral<SampleAllDelegate>();
            builder.UseBlePeripherals();
            builder.UseGeofencing<SampleAllDelegate>();
            builder.UseGpsBackground<SampleAllDelegate>();
            builder.UseNotifications();
            builder.UseSpeechRecognition();

            builder.UseAccelerometer();
            builder.UseAmbientLightSensor();
            builder.UseBarometer();
            builder.UseCompass();
            builder.UseDeviceOrientationSensor();
            builder.UseMagnetometer();
            builder.UsePedometer();
            builder.UseProximitySensor();
        }
    }
}

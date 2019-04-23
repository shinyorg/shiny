using System;
using Android.Hardware;


namespace Shiny.Sensors
{
    public class ShinySensorManager : Java.Lang.Object, ISensorEventListener
    {
        readonly SensorManager sensorManager;
        Action<SensorEvent> action;


		public ShinySensorManager(SensorManager sensorManager)
		{
			this.sensorManager = sensorManager;
		}


        public bool Start(SensorType sensorType, SensorDelay delay, Action<SensorEvent> sensorAction)
        {
            this.action = sensorAction;
            var sensor = this.sensorManager.GetDefaultSensor(sensorType);
            var result = this.sensorManager.RegisterListener(this, sensor, delay);
            return result;
        }


        public void Stop() => this.sensorManager.UnregisterListener(this);


        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
        }


        public void OnSensorChanged(SensorEvent e) => this.action(e);
    }
}
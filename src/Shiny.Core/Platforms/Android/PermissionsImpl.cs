
//        //Android: Manifest checks for Bluetooth, BluetoothAdmin, BluetoothPrivileged, Location permissions
//        //<uses-permission android:name="android.permission.BLUETOOTH"/>
//        //<uses-permission android:name="android.permission.BLUETOOTH_ADMIN"/>

//        //<!--this is necessary for Android v6+ to get the peripheral name and address-->
//        //<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
//        //<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
//        public async Task<AccessState> BluetoothLe(BluetoothLeModes modes)
//        {
//            if (!this.context.AppContext.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
//                return AccessState.NotSupported;

//            if (this.IsInManifest(Manifest.Permission.Bluetooth, false))
//                return AccessState.NotSetup;

//            if (!this.IsInManifest(Manifest.Permission.BluetoothAdmin, false))
//                return AccessState.NotSetup;

//            //if (!this.IsInManifest(Manifest.Permission.BluetoothPrivileged, false))
//            //    permission = AccessState.NotSetup;

//            return await this.Location(false);
//        }


//        public Task<AccessState> Notifications()
//        {
//            var state = AccessState.Unknown;
//            var apiLevel = (int) Build.VERSION.SdkInt;

//            if (apiLevel >= 26)
//            {
//                var enabled = NotificationManager
//                    .FromContext(this.context.AppContext)
//                    .AreNotificationsEnabled();

//                state = enabled ? AccessState.Granted : AccessState.Disabled;
//            }
//            else if (apiLevel >= 24)
//            {
//                var enabled = NotificationManagerCompat
//                    .From(context.AppContext)
//                    .AreNotificationsEnabled();

//                state = enabled ? AccessState.Granted : AccessState.Disabled;
//            }

//            return Task.FromResult(state);
//        }



//        public async Task<AccessState> Speech()
//        {
//            if (!SpeechRecognizer.IsRecognitionAvailable(this.context.AppContext))
//                return AccessState.Unknown;

//            if (!this.IsInManifest(Manifest.Permission.RecordAudio, false))
//                return AccessState.Denied;

//            return AccessState.Granted;
//        }

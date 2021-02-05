using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.BluetoothLE.IBleDelegate", "UseBleClient", false)]

[assembly: StaticGeneration("Shiny.BluetoothLE.IBleManager", "ShinyBle")]


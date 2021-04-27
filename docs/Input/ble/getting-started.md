Title: Getting Started
Order: 1
---

Bluetooth LE is divided into 2 separate categories - the central manager (client) and the peripheral manager (server). 

<?# PackageInfo "Shiny.BluetoothLE" /?>

## Setup
1. Add the following to your [Shiny Startup](xref:startup) 
<?! Startup ?>
services.UseBleClient();
<?!/ Startup ?>
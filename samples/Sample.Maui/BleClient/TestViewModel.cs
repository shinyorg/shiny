using System;
using System.Collections.Generic;
using Shiny;
using Shiny.BluetoothLE;
using Xamarin.Forms;


namespace Sample
{
	public class TestViewModel : SampleViewModel
	{
		readonly IBleManager manager;


		public TestViewModel()
		{
			this.manager = ShinyHost.Resolve<IBleManager>();

			this.Tests = new List<CommandItem>
			{
				this.Test1()
			};
		}


		public IList<CommandItem> Tests { get; }


		CommandItem Test1()
        {
			var cmd = new CommandItem();
			cmd.Text = "Test1";
			cmd.PrimaryCommand = new Command(async () =>
			{
                //IGattCharacteristic gattCharacteristic;

                //ConnectionConfig config = new ConnectionConfig()
                //{
                //    AutoConnect = false,
                //};

                //try
                //{
                //    await peripheral.ConnectAsync(config);
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine($"ConnectAsync ex {ex}");
                //    return;
                //}

                //try
                //{
                //    /* Uuid must be changed to match the peripherials  */
                //    gattCharacteristic = await peripheral.GetKnownCharacteristicAsync(
                //        "86db8288-2144-4f43-802b-927c4cdda45c", "8d39449c-5cbd-4407-bf96-05b234be3ecd");
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine($"ConnectAsynce ex {ex}");
                //    return;
                //}

                ///* Just to check if we are on mainthread */
                //if (MainThread.IsMainThread)
                //{
                //    Debug.WriteLine("ConnectAsync on Mainthread");

                //}

                ///* Write out some information about Gatt Characteristic */
                //Debug.WriteLine($"CanWriteWithResponse: {gattCharacteristic.CanWriteWithResponse()}");
                //Debug.WriteLine($"CanWriteWithoutResponse: {gattCharacteristic.CanWriteWithoutResponse()}");
                //Debug.WriteLine($"CanRead: {gattCharacteristic.CanRead()}");


                //await Task.Delay(500);

                //try
                //{
                //    //Write some data - This will crash the debug session.
                //    for (var i = 0; i < 10; i++)
                //    {
                //        await Task.Delay(500);
                //        await gattCharacteristic.WriteAsync(Encoding.ASCII.GetBytes("led=on"), true);

                //        await Task.Delay(500);
                //        await gattCharacteristic.WriteAsync(Encoding.ASCII.GetBytes("led=off"), true);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex);
                //}
            });
			return cmd;
        }
	}
}


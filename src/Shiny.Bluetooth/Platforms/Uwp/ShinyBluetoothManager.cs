//using System;


//namespace Shiny.Bluetooth
//{
//    public class ShinyBluetoothManager : IBluetoothManager
//    {
//    }
//}
//public sealed partial class MainPage : Page
//{
//    private StreamSocket _socket;

//    private RfcommDeviceService _service;
//    public MainPage()
//    {
//        this.InitializeComponent();
//    }

//    private async void btnSend_Click(object sender,
//                                     RoutedEventArgs e)
//    {
//        int dummy;

//        if (!int.TryParse(tbInput.Text, out dummy))
//        {
//            tbError.Text = "Invalid input";
//        }

//        var noOfCharsSent = await Send(tbInput.Text);

//        if (noOfCharsSent != 0)
//        {
//            tbError.Text = noOfCharsSent.ToString();
//        }
//    }
//    private async Task<uint> Send(string msg)
//    {
//        tbError.Text = string.Empty;

//        try
//        {
//            var writer = new DataWriter(_socket.OutputStream);

//            writer.WriteString(msg);

//            // Launch an async task to 
//            //complete the write operation
//            var store = writer.StoreAsync().AsTask();

//            return await store;
//        }
//        catch (Exception ex)
//        {
//            tbError.Text = ex.Message;

//            return 0;
//        }
//    }

//    private async void btnConnect_Click(object sender,
//                                        RoutedEventArgs e)
//    {
//        tbError.Text = string.Empty;

//        try
//        {
//            var devices =
//                  await DeviceInformation.FindAllAsync(
//                    RfcommDeviceService.GetDeviceSelector(
//                      RfcommServiceId.SerialPort));

//            var device = devices.Single(x => x.Name == "HC-05");

//            _service = await RfcommDeviceService.FromIdAsync(
//                                                    device.Id);

//            _socket = new StreamSocket();

//            await _socket.ConnectAsync(
//                  _service.ConnectionHostName,
//                  _service.ConnectionServiceName,
//                  SocketProtectionLevel.
//                  BluetoothEncryptionAllowNullAuthentication);
//        }
//        catch (Exception ex)
//        {
//            tbError.Text = ex.Message;
//        }
//    }

//    private async void btnDisconnect_Click(object sender,
//                                         RoutedEventArgs e)
//    {
//        tbError.Text = string.Empty;

//        try
//        {
//            await _socket.CancelIOAsync();
//            _socket.Dispose();
//            _socket = null;
//            _service.Dispose();
//            _service = null;
//        }
//        catch (Exception ex)
//        {
//            tbError.Text = ex.Message;
//        }
//    }
//}
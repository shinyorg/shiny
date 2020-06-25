using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Locations.Infrastructure;
using Tizen.Location;
using Tizen.Location.Geofence;


namespace Shiny.Locations
{
    public class GeofenceManagerImpl : AbstractGeofenceManager
    {
        readonly GeofenceManager geofences = new GeofenceManager();
        public GeofenceManagerImpl(IRepository repository) : base(repository) {}


        public override AccessState Status => throw new NotImplementedException();
        public override Task<AccessState> RequestAccess() => Platform.RequestAccess(Platform.Location);


        public override Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task StartMonitoring(GeofenceRegion region)
        {
            //geofence = new GeofenceManager();
            // Create the VirtualPerimeter object
            var perimeter = new VirtualPerimeter(this.geofences);
            throw new NotImplementedException();
        }

        public override Task StopAllMonitoring()
        {
            throw new NotImplementedException();
        }

        public override Task StopMonitoring(string identifier)
        {
            throw new NotImplementedException();
        }

        public override IObservable<AccessState> WhenAccessStatusChanged()
        {
            throw new NotImplementedException();
        }
        //VirtualPerimeter virtualPerimeter
    }
}
/*
// * Copyright (c) 2016 Samsung Electronics Co., Ltd
// *
// * Licensed under the Flora License, Version 1.1 (the "License");
// * you may not use this file except in compliance with the License.
// * You may obtain a copy of the License at
// *
// * http://www.apache.org/licenses/LICENSE-2.0
// *
// * Unless required by applicable law or agreed to in writing, software
// * distributed under the License is distributed on an "AS IS" BASIS,
// * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// * See the License for the specific language governing permissions and
// * limitations under the License.
// */

//using System;
//using System.Collections.Generic;
//using Tizen.Location.Geofence;
//using Xamarin.Forms;
//using Tizen.Security;

//namespace Geofence
//{
//    /// <summary>
//    /// Geofence application main class.
//    /// <list>
//    /// <item>Create 4 labels for information. This labels are for functions, errors, geofence status and proximity status.</item>
//    /// <item>Create 8 buttons for the general functions of geofence and handlers for each buttons.</item>
//    /// </list>
//    /// </summary>
//    public class App : Application
//    {
//        /// <summary>
//        /// GeofenceManager object.
//        /// </summary>
//        static GeofenceManager geofence = null;

//        /// <summary>
//        /// VirtualPerimeter object.
//        /// </summary>
//        static VirtualPerimeter perimeter = null;

//        /// <summary>
//        /// A List for information labels.
//        /// </summary>
//        static List<Label> InfoLabelList = null;

//        /// <summary>
//        /// A List for buttons.
//        /// </summary>
//        static List<Button> ButtonList = null;

//        /// <summary>
//        /// A navigation page.
//        /// </summary>
//        NavigationPage page = null;

//        /// <summary>
//        /// Create the view and add event handlers
//        /// </summary>
//        public App()
//        {
//            // Create a list for Information labels
//            InfoLabelList = new List<Label>();
//            //// Create a label for general function of geofence
//            InfoLabelList.Add(new Label());
//            //// Create a label for errors
//            InfoLabelList.Add(new Label());
//            //// Create a label for geofence status
//            InfoLabelList.Add(new Label());
//            //// Create a label for proximity status
//            InfoLabelList.Add(new Label());

//            // Create a layout for labels
//            var InfoAbsoluteLayout = new AbsoluteLayout
//            {
//                VerticalOptions = LayoutOptions.CenterAndExpand,
//                // Set the childern
//                Children =
//                {
//                    InfoLabelList[0],
//                    InfoLabelList[1],
//                    InfoLabelList[2],
//                    InfoLabelList[3],
//                },
//                // Set the padding value
//                Padding = 10,
//                // Set the background color
//                BackgroundColor = Color.White
//            };

//            // Set the layout bounds
//            AbsoluteLayout.SetLayoutBounds(InfoLabelList[0], new Rectangle(0, 10, 1.0, 0.26));
//            AbsoluteLayout.SetLayoutBounds(InfoLabelList[1], new Rectangle(0, 180, 1.0, 0.26));
//            AbsoluteLayout.SetLayoutBounds(InfoLabelList[2], new Rectangle(0, 350, 1.0, 0.26));
//            AbsoluteLayout.SetLayoutBounds(InfoLabelList[3], new Rectangle(0, 520, 1.0, 0.26));
//            // Set the layout flags
//            AbsoluteLayout.SetLayoutFlags(InfoLabelList[0], AbsoluteLayoutFlags.SizeProportional);
//            AbsoluteLayout.SetLayoutFlags(InfoLabelList[1], AbsoluteLayoutFlags.SizeProportional);
//            AbsoluteLayout.SetLayoutFlags(InfoLabelList[2], AbsoluteLayoutFlags.SizeProportional);
//            AbsoluteLayout.SetLayoutFlags(InfoLabelList[3], AbsoluteLayoutFlags.SizeProportional);

//            // Create a list for buttons
//            ButtonList = new List<Button>();
//            // Create button and add it to list
//            ButtonList.Add(new Button { Text = "Add Place" });
//            ButtonList.Add(new Button { Text = "Remove Place" });
//            ButtonList.Add(new Button { Text = "Add Fence" });
//            ButtonList.Add(new Button { Text = "Remove Fence" });
//            ButtonList.Add(new Button { Text = "Start" });
//            ButtonList.Add(new Button { Text = "Stop" });
//            ButtonList.Add(new Button { Text = "Update Place" });
//            ButtonList.Add(new Button { Text = "Fence Status" });

//            // Display the inserting information page when "Add Place" button is selected
//            ButtonList[0].Clicked += async (sender, e) => await page.PushAsync(new InsertInfoPage(sender));
//            for (int i = 1; i < 8; i++)
//            {
//                // Display the selecting id page when the other button is selected
//                ButtonList[i].Clicked += async (sender, e) => await page.PushAsync(new SelectIDPage(sender));
//            }

//            // Create a layout for buttons
//            var ButtonGridLayout = new Grid
//            {
//                // Set the row definitions for grid layout
//                RowDefinitions =
//                {
//                    new RowDefinition { Height = GridLength.Star },
//                    new RowDefinition { Height = GridLength.Star },
//                    new RowDefinition { Height = GridLength.Star },
//                    new RowDefinition { Height = GridLength.Star },
//                },
//                // Set the column definitions for grid layout
//                ColumnDefinitions =
//                {
//                    new ColumnDefinition { Width = GridLength.Star },
//                    new ColumnDefinition { Width = GridLength.Star },
//                },
//                VerticalOptions = LayoutOptions.End,
//                // Set the background color
//                BackgroundColor = Color.White,
//                // Set the value of the padding
//                Padding = 10
//            };

//            // Add the buttons to layout
//            for (int i = 0; i < 2; i++)
//            {
//                for (int j = 0; j < 4; j++)
//                {
//                    ButtonGridLayout.Children.Add(ButtonList[i + j * 2], i, j);
//                }
//            }

//            // The root page of your application
//            MainPage = new NavigationPage(new ContentPage
//            {
//                // Set the title of this page
//                Title = "Geofence",
//                // Create a layout
//                Content = new StackLayout
//                {
//                    // Add the sub-layouts to this layout
//                    Children = { InfoAbsoluteLayout, ButtonGridLayout }
//                }
//            });
//            page = (NavigationPage)MainPage;

//            // Check the privilege
//            PrivilegeCheck();
//        }

//        /// <summary>
//        /// Permission check
//        /// </summary>
//        private void PrivilegeCheck()
//        {
//            try
//            {
//                /// Check location permission
//                CheckResult result = PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location");

//                switch (result)
//                {
//                    case CheckResult.Allow:
//                        break;
//                    case CheckResult.Deny:
//                        break;
//                    case CheckResult.Ask:
//                        /// Request to privacy popup
//                        PrivacyPrivilegeManager.RequestPermission("http://tizen.org/privilege/location");
//                        break;
//                }
//            }
//            catch (Exception ex)
//            {
//                /// Exception handling
//                InfoLabelList[1].Text = "[Status] Privilege : " + ex.Message;
//            }
//        }

//        /// <summary>
//        /// Handle when your app starts.
//        /// </summary>
//        protected override void OnStart()
//        {
//        }

//        /// <summary>
//        /// Handle when your app sleeps.
//        /// </summary>
//        protected override void OnSleep()
//        {
//            // Check the permission for location privilege
//            if (PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location") == CheckResult.Allow)
//            {
//                // Remove the handle for GeofenceEventChanged
//                geofence.GeofenceEventChanged -= GeofenceEventChanged;
//                // Remove the handle for StateChanged
//                geofence.StateChanged -= StateChanged;
//                // Remove the handle for ProximityChanged
//                geofence.ProximityChanged -= ProximityChanged;
//            }

//            // Dispose the GeofenceManager object
//            if (geofence != null)
//            {
//                geofence.Dispose();
//            }

//            perimeter = null;
//            geofence = null;

//            // Set the value to the labels
//            InfoLabelList[0].Text = "GeofenceManager.Dispose";
//            InfoLabelList[1].Text = "Success";
//        }

//        /// <summary>
//        /// Handle when your app resumes.
//        /// </summary>
//        protected override void OnResume()
//        {
//            /// Set the value to label
//            InfoLabelList[0].Text = "new GeofenceManager and VirtualPerimeter";
//            try
//            {
//                if (geofence == null)
//                {
//                    // Create the GeofenceManager object
//                    geofence = new GeofenceManager();
//                    // Create the VirtualPerimeter object
//                    perimeter = new VirtualPerimeter(geofence);
//                }

//                // Set the value to label
//                InfoLabelList[1].Text = "Success";

//                // Check the permission for location privilege
//                if (PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location") == CheckResult.Allow)
//                {
//                    // Add a handle for GeofenceEventChanged
//                    geofence.GeofenceEventChanged += GeofenceEventChanged;
//                    // Add a handle for StateChanged
//                    geofence.StateChanged += StateChanged;
//                    // Add a handle for ProximityChanged
//                    geofence.ProximityChanged += ProximityChanged;
//                }
//            }
//            catch (Exception e)
//            {
//                // Set the value to label about occured exception
//                InfoLabelList[1].Text = e.Message;
//            }
//        }

//        /// <summary>
//        /// Handle when GeofenceEventChanged event is occured.
//        /// </summary>
//        /// <param name="sender">Specifies the sender of this event</param>
//        /// <param name="args">Specifies the information of this event</param>
//        public void GeofenceEventChanged(object sender, GeofenceResponseEventArgs args)
//        {
//            // Set the value to label about an occured event
//            InfoLabelList[0].Text = args.EventType.ToString();
//            // Set the value to label about an occured error
//            switch (args.ErrorCode)
//            {
//                case GeofenceError.None:
//                    InfoLabelList[1].Text = "None";
//                    break;
//                case GeofenceError.OutOfMemory:
//                    InfoLabelList[1].Text = "OutOfMemory";
//                    break;
//                case GeofenceError.InvalidParameter:
//                    InfoLabelList[1].Text = "InvalidParameter";
//                    break;
//                case GeofenceError.PermissionDenied:
//                    InfoLabelList[1].Text = "PermissionDenied";
//                    break;
//                case GeofenceError.NotSupported:
//                    InfoLabelList[1].Text = "NotSupported";
//                    break;
//                case GeofenceError.NotInitialized:
//                    InfoLabelList[1].Text = "NotInitialized";
//                    break;
//                case GeofenceError.InvalidID:
//                    InfoLabelList[1].Text = "InvalidID";
//                    break;
//                case GeofenceError.Exception:
//                    InfoLabelList[1].Text = "Exception";
//                    break;
//                case GeofenceError.AlreadyStarted:
//                    InfoLabelList[1].Text = "AlreadyStarted";
//                    break;
//                case GeofenceError.TooManyGeofence:
//                    InfoLabelList[1].Text = "TooManyGeofence";
//                    break;
//                case GeofenceError.IPC:
//                    InfoLabelList[1].Text = "IPC Error";
//                    break;
//                case GeofenceError.DBFailed:
//                    InfoLabelList[1].Text = "DBFailed";
//                    break;
//                case GeofenceError.PlaceAccessDenied:
//                    InfoLabelList[1].Text = "PlaceAccessDenied";
//                    break;
//                case GeofenceError.GeofenceAccessDenied:
//                    InfoLabelList[1].Text = "GeofenceAccessDenied";
//                    break;
//                default:
//                    InfoLabelList[1].Text = "Unknown Error";
//                    break;
//            }
//        }

//        /// <summary>
//        /// Handle when GeofenceStateEventArgs event is occured.
//        /// </summary>
//        /// <param name="sender">Specifies the sender of this event</param>
//        /// <param name="args">Specifies the information of this event</param>
//        public void StateChanged(object sender, GeofenceStateEventArgs args)
//        {
//            // Set the value to label about a changed geofence state
//            InfoLabelList[2].Text = "FenceID: " + args.GeofenceId.ToString() + ", GeofenceState: ";
//            switch (args.State)
//            {
//                case GeofenceState.In:
//                    InfoLabelList[2].Text += "In";
//                    break;
//                case GeofenceState.Out:
//                    InfoLabelList[2].Text += "Out";
//                    break;
//                default:
//                    InfoLabelList[2].Text += "Uncertain";
//                    break;
//            }
//        }

//        /// <summary>
//        /// Handle when ProximityChanged event is occured.
//        /// </summary>
//        /// <param name="sender">Specifies the sender of this event</param>
//        /// <param name="args">Specifies the information of this event</param>
//        public void ProximityChanged(object sender, ProximityStateEventArgs args)
//        {
//            // Set the value to label about a changed proximity state
//            InfoLabelList[3].Text = "FenceID: " + args.GeofenceId.ToString() + ", ProximityState: ";
//            switch (args.State)
//            {
//                case ProximityState.Immediate:
//                    InfoLabelList[3].Text += "Immediate";
//                    break;
//                case ProximityState.Near:
//                    InfoLabelList[3].Text += "Near";
//                    break;
//                case ProximityState.Far:
//                    InfoLabelList[3].Text += "Far";
//                    break;
//                default:
//                    InfoLabelList[3].Text += "Uncertain";
//                    break;
//            }
//        }

//        /// <summary>
//        /// Geofence application sub class.
//        /// </summary>
//        private class InsertInfoPage : ContentPage
//        {
//            /// <summary>
//            /// Constructor of InsertInfoPage class.
//            /// </summary>
//            /// <param name="sender">Specifies the sender of this event</param>
//            public InsertInfoPage(object sender)
//            {
//                if (PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location") != CheckResult.Allow)
//                {
//                    ShowNoPermissionAlert();
//                    return;
//                }

//                CreatePage(-1, FenceType.GeoPoint, sender);
//            }

//            /// <summary>
//            /// Constructor of InsertInfoPage class.
//            /// </summary>
//            /// <param name="placeID">the place id</param>
//            /// <param name="sender">Specifies the sender of this event</param>
//            public InsertInfoPage(int placeID, object sender)
//            {
//                CreatePage(placeID, FenceType.GeoPoint, sender);
//            }

//            /// <summary>
//            /// Constructor of InsertInfoPage class.
//            /// </summary>
//            /// <param name="placeID">the place id</param>
//            /// <param name="fenceType">the fence type</param>
//            /// <param name="sender">Specifies the sender of this event</param>
//            public InsertInfoPage(int placeID, FenceType fenceType, object sender)
//            {
//                CreatePage(placeID, fenceType, sender);
//            }

//            /// <summary>
//            /// Create the view for InsertInfoPage class.
//            /// </summary>
//            /// <param name="placeID">the place id</param>
//            /// <param name="fenceType">the fence type</param>
//            /// <param name="sender">Specifies the sender of this event</param>
//            private void CreatePage(int placeID, FenceType fenceType, object sender)
//            {
//                // Set the title of this page
//                Title = ((Button)sender).Text;

//                // Create an entry
//                var FirstEntry = new Entry
//                {
//                    BackgroundColor = Color.White,
//                    VerticalOptions = LayoutOptions.Center
//                };

//                // Set the guide text for the entry
//                if (sender == ButtonList[0] || sender == ButtonList[6])
//                {
//                    FirstEntry.Placeholder = "Place Name";
//                }
//                else if (sender == ButtonList[2])
//                {
//                    if (fenceType == FenceType.GeoPoint)
//                    {
//                        FirstEntry.Placeholder = "Latitude";
//                    }
//                    else
//                    {
//                        FirstEntry.Placeholder = "BSSID";
//                    }
//                }

//                // Create a second entry
//                var SecondEntry = new Entry
//                {
//                    // Set the guide text
//                    Placeholder = "Longitude",
//                    // Set the background color
//                    BackgroundColor = Color.White,
//                    VerticalOptions = LayoutOptions.Center
//                };

//                // Create a cancel button
//                var cancelButton = new Button { Text = "Cancel" };
//                // Move to the main page when cancel button is selected
//                cancelButton.Clicked += async (o, e) => await Navigation.PopToRootAsync();

//                // Create a done button
//                var doneButton = new Button { Text = "Done" };
//                // Run the function about inserted information
//                doneButton.Clicked += async (o, e) =>
//                {
//                    try
//                    {
//                        // Check the value of the first entry
//                        if (string.IsNullOrEmpty(FirstEntry.Text))
//                        {
//                            // Throw an argument exception with message
//                            throw new ArgumentException("Content cannot be null or empty");
//                        }

//                        if (sender == ButtonList[0])
//                        {
//                            // Add place with inserted name
//                            perimeter.AddPlaceName(FirstEntry.Text);
//                        }
//                        else if (sender == ButtonList[2])
//                        {
//                            Fence fence = null;
//                            switch (fenceType)
//                            {
//                                case FenceType.GeoPoint:
//                                    // Check the value of the second entry
//                                    if (string.IsNullOrEmpty(SecondEntry.Text))
//                                    {
//                                        // Throw an argument exception with message
//                                        throw new ArgumentException("Content cannot be null or empty");
//                                    }

//                                    // Create a gps fence with inserted information
//                                    fence = Fence.CreateGPSFence(placeID, double.Parse(FirstEntry.Text), double.Parse(SecondEntry.Text), 100, "TestAddress");
//                                    break;
//                                case FenceType.Wifi:
//                                    // Create a wifi fence with inserted information
//                                    fence = Fence.CreateWifiFence(placeID, FirstEntry.Text, "TestAddress");
//                                    break;
//                                case FenceType.Bluetooth:
//                                    // Create a bt fence with inserted information
//                                    fence = Fence.CreateBTFence(placeID, FirstEntry.Text, "TestAddress");
//                                    break;
//                                default:
//                                    break;
//                            }

//                            if (fence != null)
//                            {
//                                // Add the fence
//                                perimeter.AddGeofence(fence);
//                            }
//                        }
//                        else if (sender == ButtonList[6])
//                        {
//                            // Update the place name
//                            perimeter.UpdatePlace(placeID, FirstEntry.Text);
//                        }
//                    }
//                    catch (Exception exception)
//                    {
//                        // Display the exception message
//                        await DisplayAlert("Alert", exception.Message, "OK");
//                    }

//                    // Move to the main page
//                    await Navigation.PopToRootAsync();
//                };

//                // Create a layout for buttons
//                var ButtonGridLayout = new Grid
//                {
//                    RowDefinitions =
//                    {
//                        new RowDefinition { Height = GridLength.Star },
//                    },
//                    ColumnDefinitions =
//                    {
//                        new ColumnDefinition { Width = GridLength.Star },
//                        new ColumnDefinition { Width = GridLength.Star },
//                    },
//                    VerticalOptions = LayoutOptions.End
//                };
//                ButtonGridLayout.Children.Add(cancelButton, 0, 0);
//                ButtonGridLayout.Children.Add(doneButton, 1, 0);

//                // Create a layout for this page
//                StackLayout parent = new StackLayout
//                {
//                    Margin = 10
//                };
//                parent.Children.Add(FirstEntry);
//                if (sender == ButtonList[2] && fenceType == FenceType.GeoPoint)
//                {
//                    parent.Children.Add(SecondEntry);
//                }

//                parent.Children.Add(ButtonGridLayout);

//                Content = parent;
//            }

//            /// <summary>
//            /// Display an alert for no permission.
//            /// </summary>
//            public async void ShowNoPermissionAlert()
//            {
//                // Display a alert
//                await this.DisplayAlert("Alert", "NoPermission", "OK");
//                // Move to the main page
//                await Navigation.PopToRootAsync();
//            }
//        }

//        /// <summary>
//        /// Geofence application sub class.
//        /// </summary>
//        private class SelectFenceTypePage : ContentPage
//        {
//            /// <summary>
//            /// Geofence application sub class.
//            /// </summary>
//            /// <param name="placeID">the place id</param>
//            /// <param name="sender">Specifies the sender of this event</param>
//            public SelectFenceTypePage(int placeID, object sender)
//            {
//                // Set the title of this page
//                Title = ((Button)sender).Text;

//                // Create a list view
//                var listView = new ListView
//                {
//                    VerticalOptions = LayoutOptions.Center,
//                    // Create a list about geofence type
//                    ItemsSource = new List<string>() { "GPS", "Wi-Fi", "Bluetooth" },
//                };

//                // Add a handler for list view
//                listView.ItemSelected += async (o, e) =>
//                {
//                    // Set the selected fence type
//                    FenceType type = FenceType.GeoPoint;
//                    if (e.SelectedItem.ToString() == "Wi-Fi")
//                    {
//                        type = FenceType.Wifi;
//                    }
//                    else if (e.SelectedItem.ToString() == "Bluetooth")
//                    {
//                        type = FenceType.Bluetooth;
//                    }
//                    else
//                    {
//                        type = FenceType.GeoPoint;
//                    }

//                    // Display a page for inserting information
//                    await Navigation.PushAsync(new InsertInfoPage(placeID, type, sender));
//                };

//                // Create a layout for this page
//                Content = new StackLayout
//                {
//                    Children = { listView },
//                    Margin = 10
//                };
//            }
//        }

//        /// <summary>
//        /// Geofence application sub class.
//        /// </summary>
//        private class SelectIDPage : ContentPage
//        {
//            /// <summary>
//            /// Display a list for selecting place or fence.
//            /// </summary>
//            /// <param name="sender">Specifies the object of selected button in main page</param>
//            public SelectIDPage(object sender)
//            {
//                if (PrivacyPrivilegeManager.CheckPermission("http://tizen.org/privilege/location") != CheckResult.Allow)
//                {
//                    ShowNoPermissionAlert();
//                    return;
//                }

//                // Clear some labels if button4 is selected.
//                if (sender != ButtonList[4])
//                {
//                    InfoLabelList[2].Text = "";
//                    InfoLabelList[3].Text = "";
//                }

//                // Set a title of this page
//                Title = ((Button)sender).Text;

//                // Create a list for the places or geofences
//                List<string> myList = new List<string>();
//                if (sender == ButtonList[1] || sender == ButtonList[2] || sender == ButtonList[6])
//                {
//                    // Get the information for all the places
//                    foreach (PlaceData data in perimeter.GetPlaceDataList())
//                    {
//                        // Add the place id to the list
//                        myList.Add(data.PlaceId.ToString());
//                    }
//                }
//                else if (sender == ButtonList[3] || sender == ButtonList[4] || sender == ButtonList[5] || sender == ButtonList[7])
//                {
//                    // Get the information for all the fences
//                    foreach (FenceData data in perimeter.GetFenceDataList())
//                    {
//                        // Add the geofence id to the list
//                        myList.Add(data.GeofenceId.ToString());
//                    }
//                }

//                // Create a list view
//                var listView = new ListView
//                {
//                    VerticalOptions = LayoutOptions.Center,
//                    ItemsSource = myList
//                };

//                // Add a handler for list view
//                listView.ItemSelected += async (o, e) =>
//                {
//                    if (sender == ButtonList[1])
//                    {
//                        // Remove a selected place
//                        perimeter.RemovePlace(int.Parse(e.SelectedItem.ToString()));
//                    }
//                    else if (sender == ButtonList[2])
//                    {
//                        // Display a page for selecting a fence type
//                        await Navigation.PushAsync(new SelectFenceTypePage(int.Parse(e.SelectedItem.ToString()), sender));
//                    }
//                    else if (sender == ButtonList[3])
//                    {
//                        // Remove a selected fence
//                        perimeter.RemoveGeofence(int.Parse(e.SelectedItem.ToString()));
//                    }
//                    else if (sender == ButtonList[4])
//                    {
//                        // Start a selected fence
//                        geofence.Start(int.Parse(e.SelectedItem.ToString()));
//                    }
//                    else if (sender == ButtonList[5])
//                    {
//                        // Stop a selected fence
//                        geofence.Stop(int.Parse(e.SelectedItem.ToString()));
//                    }
//                    else if (sender == ButtonList[6])
//                    {
//                        // Display a page for inserting information
//                        await Navigation.PushAsync(new InsertInfoPage(int.Parse(e.SelectedItem.ToString()), sender));
//                    }
//                    else if (sender == ButtonList[7])
//                    {
//                        // Create a fence status for sthe elected fence
//                        FenceStatus status = new FenceStatus(int.Parse(e.SelectedItem.ToString()));

//                        // Set the value to label about the fence status
//                        InfoLabelList[2].Text = "Fence ID: " + int.Parse(e.SelectedItem.ToString()) + ", GeofenceState: ";
//                        switch (status.State)
//                        {
//                            case GeofenceState.In:
//                                InfoLabelList[2].Text += "In";
//                                break;
//                            case GeofenceState.Out:
//                                InfoLabelList[2].Text += "Out";
//                                break;
//                            default:
//                                InfoLabelList[2].Text += "Uncertain";
//                                break;
//                        }

//                        // Add the duration to the label
//                        InfoLabelList[2].Text += ", Duration: " + status.Duration.ToString();
//                    }

//                    if (sender != ButtonList[2] && sender != ButtonList[6])
//                    {
//                        // Move to the main page
//                        await Navigation.PopToRootAsync();
//                    }
//                };

//                // Create a layout for this page
//                Content = new StackLayout
//                {
//                    Children = { listView },
//                    // Set the margin of the layout
//                    Margin = 10
//                };

//                // Check the count of the list is empty
//                if (myList.Count < 1)
//                {
//                    // Displace an alert
//                    this.ShowEmptyAlert();
//                }
//            }

//            /// <summary>
//            /// Display an alert for empty list.
//            /// </summary>
//            private async void ShowEmptyAlert()
//            {
//                // Display a alert
//                await DisplayAlert("Alert", "Empty", "OK");
//                // Move to the main page
//                await Navigation.PopToRootAsync();
//            }

//            /// <summary>
//            /// Display an alert for no permission.
//            /// </summary>
//            public async void ShowNoPermissionAlert()
//            {
//                // Display a alert
//                await this.DisplayAlert("Alert", "NoPermission", "OK");
//                // Move to the main page
//                await Navigation.PopToRootAsync();
//            }
//        }
//    }
//}
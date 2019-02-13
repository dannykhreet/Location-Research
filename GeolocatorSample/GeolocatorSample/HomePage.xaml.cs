using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeolocatorSample
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HomePage : TabbedPage
	{
		int count;
		bool tracking;
		Position savedPosition;
        private int totalChanges = 0;
        // Set speed delay for monitoring changes.
        SensorSpeed speed = SensorSpeed.UI;
        public int TotalChanges
        {
            get { return totalChanges; }
            set { totalChanges = value; }
        }
        private TimeSpan TotalTime;


        int TotalLoops = 0;
        public ObservableCollection<Position> Positions { get; } = new ObservableCollection<Position>();
        Position CurrentPositionOnTimer;
        TimeSpan totalSecond;
        Position NewPositionOnTimer;
        Stopwatch stopwatch;
        bool haveChanges;


        IMyLocation loc;
        public HomePage()
		{
			InitializeComponent();
			ListViewPositions.ItemsSource = Positions;
            //
            stopwatch = new Stopwatch();
            CurrentPositionOnTimer = new Position();
            NewPositionOnTimer = new Position();
            lblStopwatch.Text = "00:00:00.00000";
            NumderTotalChanges.Text = TotalChanges.ToString();
            NumderTotalLoops.Text = TotalLoops.ToString();


            LocationStatus.Text = DependencyService.Get<IGetLocationProvider>().GetProvider();

            loc = DependencyService.Get<IMyLocation>();
            loc.locationObtained += (object sender,
                ILocationEventArgs e) => {
                    var lat = e.lat;
                    var lng = e.lng;
                    lblLat.Text = lat.ToString();
                    lblLng.Text = lng.ToString();
                };
            loc.ObtainMyLocation();

        }

        private async void ButtonCached_Clicked(object sender, EventArgs e)
		{
			try
			{
				var hasPermission = await Utils.CheckPermissions(Permission.Location);
				if (!hasPermission)
					return;

				ButtonCached.IsEnabled = false;

				var locator = CrossGeolocator.Current;
				locator.DesiredAccuracy = DesiredAccuracy.Value;
				LabelCached.Text = "Getting gps...";

				var position = await locator.GetLastKnownLocationAsync();

				if (position == null)
				{
					LabelCached.Text = "null cached location :(";
					return;
				}

				savedPosition = position;
				ButtonAddressForPosition.IsEnabled = true;
				LabelCached.Text = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
					position.Timestamp, position.Latitude, position.Longitude,
					position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);

			}
			catch (Exception ex)
			{
				await DisplayAlert("Uh oh", "Something went wrong, but don't worry we captured for analysis! Thanks.", "OK");
			}
			finally
			{
				ButtonCached.IsEnabled = true;
			}
		}

		private async void ButtonGetGPS_Clicked()
		{
			try
			{
				var hasPermission = await Utils.CheckPermissions(Permission.Location);
				if (!hasPermission)
					return;

				ButtonGetGPS.IsEnabled = false;

				var locator = CrossGeolocator.Current;
				locator.DesiredAccuracy = DesiredAccuracy.Value;
				labelGPS.Text = "Getting gps...";

				var position = await locator.GetPositionAsync(TimeSpan.FromSeconds(Timeout.Value), null, IncludeHeading.IsToggled);

				if (position == null)
				{
					labelGPS.Text = "null gps :(";
					return;
				}
				savedPosition = position;
				ButtonAddressForPosition.IsEnabled = true;
				labelGPS.Text = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
					position.Timestamp, position.Latitude, position.Longitude,
					position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);

			}
			catch (Exception ex)
			{
				await DisplayAlert("Uh oh", "Something went wrong, but don't worry we captured for analysis! Thanks.", "OK");
			}
			finally
			{
				ButtonGetGPS.IsEnabled = true;
			}
		}

		private async void ButtonAddressForPosition_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (savedPosition == null)
					return;

				var hasPermission = await Utils.CheckPermissions(Permission.Location);
				if (!hasPermission)
					return;

				ButtonAddressForPosition.IsEnabled = false;

				var locator = CrossGeolocator.Current;

				var address = await locator.GetAddressesForPositionAsync(savedPosition, "RJHqIE53Onrqons5CNOx~FrDr3XhjDTyEXEjng-CRoA~Aj69MhNManYUKxo6QcwZ0wmXBtyva0zwuHB04rFYAPf7qqGJ5cHb03RCDw1jIW8l");
				if (address == null || address.Count() == 0)
				{
					LabelAddress.Text = "Unable to find address";
				}

				var a = address.FirstOrDefault();
				LabelAddress.Text = $"Address: Thoroughfare = {a.Thoroughfare}\nLocality = {a.Locality}\nCountryCode = {a.CountryCode}\nCountryName = {a.CountryName}\nPostalCode = {a.PostalCode}\nSubLocality = {a.SubLocality}\nSubThoroughfare = {a.SubThoroughfare}";

			}
			catch (Exception ex)
			{
				await DisplayAlert("Uh oh", "Something went wrong, but don't worry we captured for analysis! Thanks.", "OK");
			}
			finally
			{
				ButtonAddressForPosition.IsEnabled = true;
			}
		}

		private async void ButtonTrack_Clicked(object sender, EventArgs e)
		{
			try
			{
				var hasPermission = await Utils.CheckPermissions(Permission.Location);
				if (!hasPermission)
					return;

				if (tracking)
				{
					CrossGeolocator.Current.PositionChanged -= CrossGeolocator_Current_PositionChanged;
					CrossGeolocator.Current.PositionError -= CrossGeolocator_Current_PositionError;
				}
				else
				{
					CrossGeolocator.Current.PositionChanged += CrossGeolocator_Current_PositionChanged;
					CrossGeolocator.Current.PositionError += CrossGeolocator_Current_PositionError;
				}

				if (CrossGeolocator.Current.IsListening)
				{
					await CrossGeolocator.Current.StopListeningAsync();
					labelGPSTrack.Text = "Stopped tracking";
					ButtonTrack.Text = "Start Tracking";
					tracking = false;
					count = 0;
				}
				else
				{
					Positions.Clear();
					if (await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(TrackTimeout.Value), TrackDistance.Value,
						TrackIncludeHeading.IsToggled, new ListenerSettings
						{
							ActivityType = (ActivityType)ActivityTypePicker.SelectedIndex,
							AllowBackgroundUpdates = AllowBackgroundUpdates.IsToggled,
							DeferLocationUpdates = DeferUpdates.IsToggled,
							DeferralDistanceMeters = DeferalDistance.Value,
							DeferralTime = TimeSpan.FromSeconds(DeferalTIme.Value),
							ListenForSignificantChanges = ListenForSig.IsToggled,
							PauseLocationUpdatesAutomatically = PauseLocation.IsToggled
						}))
					{
						labelGPSTrack.Text = "Started tracking";
						ButtonTrack.Text = "Stop Tracking";
						tracking = true;
					}
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Uh oh", "Something went wrong, but don't worry we captured for analysis! Thanks.", "OK");
			}
		}
	



	void CrossGeolocator_Current_PositionError(object sender, PositionErrorEventArgs e)
	{

		labelGPSTrack.Text = "Location error: " + e.Error.ToString();
	}

	void CrossGeolocator_Current_PositionChanged(object sender, PositionEventArgs e)
	{

		Device.BeginInvokeOnMainThread(() =>
		{
			var position = e.Position;
			Positions.Add(position);
			count++;
			LabelCount.Text = $"{count} updates";
			labelGPSTrack.Text = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
				position.Timestamp, position.Latitude, position.Longitude,
				position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);

		});
	}
        private void btnStop_Clicked(object sender, EventArgs e)
        {
            btnStart.Text = "Resume";
            stopwatch.Stop();
        }

        private void btnReset_Clicked(object sender, EventArgs e)
        {
            lblStopwatch.Text = "00:00:00.00000";
            btnStart.Text = "Start";
            stopwatch.Reset();
        }
        private void ResetTotalChanges_Clicked(object sender, EventArgs e)
        {
            TotalChanges = 0;
            NumderTotalChanges.Text = TotalChanges.ToString();

        }
        private void ResetTotalLoops_Clicked(object sender, EventArgs e)
        {
            TotalLoops = 0;
            NumderTotalLoops.Text = TotalLoops.ToString();

        }
        public void ToggleAccelerometer(object sender, ToggledEventArgs e)
        {
            try
            {
                if (Accelerometer.IsMonitoring)
                    Accelerometer.Stop();
                else
                    Accelerometer.Start(speed);
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Feature not supported on device
            }
            catch (Exception ex)
            {
                // Other error has occurred.
            }
        }
        private async void btnStart_Clicked()
        {
            ButtonGetGPS_Clicked();
            var locator = CrossGeolocator.Current;
            locator.DesiredAccuracy = DesiredAccuracy.Value;
            CurrentPositionOnTimer = await locator.GetLastKnownLocationAsync();

            haveChanges = false;
            if (!stopwatch.IsRunning)
            {
                stopwatch.Start();

                Device.StartTimer(TimeSpan.FromMilliseconds(10), () =>
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        NewPositionOnTimer = await locator.GetLastKnownLocationAsync();
                    });
                    lblStopwatch.Text = stopwatch.Elapsed.ToString();
                    totalSecond = TimeSpan.FromSeconds(20);
                    if (stopwatch.Elapsed <= totalSecond)
                    {

                        if (NewPositionOnTimer.Latitude == 0 && NewPositionOnTimer.Longitude == 0)
                        {
                            NewPositionOnTimer = CurrentPositionOnTimer;
                        }

                        Location LocationNewPositionOnTimer = new Location(NewPositionOnTimer.Latitude, NewPositionOnTimer.Longitude);
                        Location LocationCurrentPositionOnTimer = new Location(CurrentPositionOnTimer.Latitude, CurrentPositionOnTimer.Longitude);
                        double Kilometers = Location.CalculateDistance(LocationNewPositionOnTimer, LocationCurrentPositionOnTimer, DistanceUnits.Kilometers);





                        bool LatitudeChanged = (CurrentPositionOnTimer.Latitude) != (NewPositionOnTimer.Latitude);
                        bool LongitudeChanged = (CurrentPositionOnTimer.Longitude) != (NewPositionOnTimer.Longitude);

                        double m = Kilometers * 1000;
                        if (m != 0)
                        {
                            cantimeter.Text = m.ToString();
                        }
                        haveChanges = ((LatitudeChanged || LongitudeChanged) && m > 1);

                        if (haveChanges)
                        {
                            lblStopwatch.Text = stopwatch.Elapsed.ToString(); //"الوقت المنقضي  " + string.Format("{0:mm\\:ss}", timer.CurrentTime);
                            haveChanges = false;
                            stopwatch.Stop();
                            stopwatch.Reset();
                            TotalChanges += 1;
                            NumderTotalChanges.Text = TotalChanges.ToString();
                            CurrentPositionOnTimer = NewPositionOnTimer;
                            stopwatch.Start();
                        }
                        else
                        {
                            TotalTime = stopwatch.Elapsed;
                            lblStopwatch.Text = stopwatch.Elapsed.ToString(); //"الوقت المنقضي  " + string.Format("{0:mm\\:ss}", timer.CurrentTime);
                        }

                    }
                    else
                    {
                        TotalLoops += 1;
                        NumderTotalLoops.Text = TotalLoops.ToString();
                        ButtonGetGPS_Clicked();
                        stopwatch.Stop();
                        stopwatch.Reset();
                        stopwatch.Start();
                        //goto firt;
                    }



                    if (!stopwatch.IsRunning)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }

                 );
            }
        }
    }
}
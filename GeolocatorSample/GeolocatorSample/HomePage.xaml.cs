using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeolocatorSample
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class HomePage : ContentPage
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
        int totgpsnumber = 0;
        public ObservableCollection<Position> Positions { get; } = new ObservableCollection<Position>();
        Position CurrentPositionOnTimer;
        TimeSpan totalSecond;
        int maxSecond =20;
        Position NewPositionOnTimer;
        Stopwatch stopwatch;
        bool haveChanges;
        IMyLocation loc;
        public HomePage()
		{
			InitializeComponent();
            stopwatch = new Stopwatch();
            CurrentPositionOnTimer = new Position();
            NewPositionOnTimer = new Position();
            lblStopwatch.Text = "00:00:00.00000";
            NumderTotalChanges.Text = TotalChanges.ToString();
            NumderTotalLoops.Text = TotalLoops.ToString();
            loc = DependencyService.Get<IMyLocation>();
            loc.locationObtained += (object sender,
                ILocationEventArgs e) => {
                    var lat = e.lat;
                    var lng = e.lng;
                   // lblLat.Text = lat.ToString();
                   // lblLng.Text = lng.ToString();
                };
            loc.ObtainMyLocation();
            // Register for reading changes, be sure to unsubscribe when finished
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;           
        }
        void EnryeMaxSecond_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (e.NewTextValue != "")
            {
                maxSecond = int.Parse(e.NewTextValue);
            }
            if (e.NewTextValue == "")
            {
                maxSecond = 20;
            }
        }
        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            AccelerometerLabel.Text= $"Reading: X: {data.Acceleration.X}, Y: {data.Acceleration.Y}, Z: {data.Acceleration.Z}";
        }

        private async Task ButtonGetGPS_Clicked()
		{
			try
			{
				var hasPermission = await Utils.CheckPermissions(Permission.Location);
				if (!hasPermission)
					return;

				ButtonGetGPS.IsEnabled = false;

				var locator = CrossGeolocator.Current;
				locator.DesiredAccuracy = 500;
				labelGPS.Text = "Getting gps...";
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var position = await locator.GetPositionAsync();
                watch.Stop();
                lasttimegps.Text = watch.ElapsedMilliseconds.ToString() + " ms ";
                if (position == null)
				{
					labelGPS.Text = "null gps :(";
					return;
				}
                totgpsnumber += 1;
                totalgps.Text = totgpsnumber.ToString();
                savedPosition = position;
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

        private void btnStop_Clicked(object sender, EventArgs e)
        {
            btnStart.Text = "Resume";
            EnryeMaxSecond.IsEnabled = true;
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
        public void ToggleAccelerometer()
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
            if (btnStart.Text == "Start" && !stopwatch.IsRunning)
            {
                await ButtonGetGPS_Clicked();
            }

            haveChanges = false;
            if (!stopwatch.IsRunning)
            {
                stopwatch.Start();
                Device.StartTimer(TimeSpan.FromMilliseconds(100), () => OnTimerTick());
            }
        }

        bool OnTimerTick()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                lblStopwatch.Text = stopwatch.Elapsed.ToString();
                totalSecond = TimeSpan.FromSeconds(maxSecond);
                if (stopwatch.Elapsed <= totalSecond)
                {
                    loc.ObtainMyLocation();
                    double lattosave = loc.GetLatPassive();
                    double lontosave = loc.GetLongPassive();
                    bool AltitudeChanged = savedPosition.Latitude != loc.GetLatPassive();
                    bool LongChanged = savedPosition.Longitude != loc.GetLongPassive();
                    haveChanges = AltitudeChanged && LongChanged;
                    if (haveChanges)
                    {
                        lblStopwatch.Text = stopwatch.Elapsed.ToString(); 
                        haveChanges = false;
                        stopwatch.Stop();
                        stopwatch.Reset();
                        TotalChanges += 1;
                        NumderTotalChanges.Text = TotalChanges.ToString();
                        savedPosition.Latitude = loc.GetLatPassive();
                        savedPosition.Longitude = loc.GetLongPassive();
                        stopwatch.Start();
                    }
                    else
                    {
                        TotalTime = stopwatch.Elapsed;
                        lblStopwatch.Text = stopwatch.Elapsed.ToString(); 
                    }
                }
                else
                {
                    TotalLoops += 1;
                    NumderTotalLoops.Text = TotalLoops.ToString();
                    totalgps.Text = totgpsnumber.ToString();
                    stopwatch.Stop();
                    stopwatch.Reset();
                    await ButtonGetGPS_Clicked();
                    stopwatch.Start();
                    
                }            
            });
            return true;
        }

        private void totalgps_Clicked(object sender, EventArgs e)
        {
            totgpsnumber = 0 ;
            totalgps.Text = totgpsnumber.ToString();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TouchPanels.Devices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WindowsIoT.TouchSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string CalibrationFilename = "TSC2046";
        private Tsc2046 tsc2046;
        private TouchPanels.TouchProcessor processor;
        private Point lastPosition = new Point(double.NaN, double.NaN);
        
        public MainPage()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Init();
            Status.Text = "Init Success";
            base.OnNavigatedTo(e);
        }

        private async void Init()
        {
            tsc2046 = await Tsc2046.GetDefaultAsync();
            bool successful = await tsc2046.TryLoadCalibrationAsync(CalibrationFilename);
            if (!successful)
            {
                await CalibrateTouch(); //Initiate calibration if we don't have a calibration on file
            }

            processor = new TouchPanels.TouchProcessor(tsc2046);
            processor.Initialize();
        }

        private async void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            await CalibrateTouch();
        }

        private async Task CalibrateTouch()
        {
            var calibration = await TouchPanels.UI.LcdCalibrationView.CalibrateScreenAsync(tsc2046);
            tsc2046.SetCalibration(calibration.A, calibration.B, calibration.C, calibration.D, calibration.E, calibration.F);
            try
            {
                await tsc2046.SaveCalibrationAsync(CalibrationFilename);
            }
            catch (Exception ex)
            {
                Status.Text = ex.Message;
            }
        }

        private void Red_Click(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Red);
        }
        private void Green_Click(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Green);
        }
        private void Blue_Click(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Background = new SolidColorBrush(Colors.Blue);
        }
    }
}

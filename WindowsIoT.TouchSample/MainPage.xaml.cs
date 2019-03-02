using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TouchPanels;
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
            await Manager.StartDevice();
            await Manager.LoadCalibrationMatrix();
        }

        private async void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            await CalibrateTouch();
        }

        private async Task CalibrateTouch()
        {
            await Manager.Calibrate(CalibrationStyle.CornersAndCenter);
            await Manager.SaveCalibrationMatrix();
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

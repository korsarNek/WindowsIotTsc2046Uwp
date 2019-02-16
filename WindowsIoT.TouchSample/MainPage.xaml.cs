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
        private InputInjector _inputInjector;
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
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (processor != null)
            {
                //Unhooking from all the touch events, will automatically shut down the processor.
                //Remember to do this, or you view could be staying in memory.
                processor.PointerDown -= Processor_PointerDown;
                processor.PointerMoved -= Processor_PointerMoved;
                processor.PointerUp -= Processor_PointerUp;
            }
            if (_inputInjector != null)
            {
                _inputInjector.UninitializeTouchInjection();
            }
            base.OnNavigatingFrom(e);
        }

        private async void Init()
        {
            tsc2046 = await TouchPanels.Devices.Tsc2046.GetDefaultAsync();
            try
            {
                await tsc2046.LoadCalibrationAsync(CalibrationFilename);
            }
            catch (System.IO.FileNotFoundException)
            {
                await CalibrateTouch(); //Initiate calibration if we don't have a calibration on file
            }
            catch(System.UnauthorizedAccessException)
            {
                //No access to documents folder
                await new Windows.UI.Popups.MessageDialog("Make sure the application manifest specifies access to the documents folder and declares the file type association for the calibration file.", "Configuration Error").ShowAsync();
                throw;
            }
            //Load up the touch processor and listen for touch events
            _inputInjector = InputInjector.TryCreate();
            if (_inputInjector == null)
                throw new InvalidOperationException("Unable to create InputInjector.");

            _inputInjector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);
            processor = new TouchPanels.TouchProcessor(tsc2046);
            processor.PointerDown += Processor_PointerDown;
            processor.PointerMoved += Processor_PointerMoved;
            processor.PointerUp += Processor_PointerUp;
        }

        private void Processor_PointerDown(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Down");
            var downInfos = new InjectedInputTouchInfo
            {
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions =
                        InjectedInputPointerOptions.PointerDown |
                        InjectedInputPointerOptions.InContact,
                    PixelLocation = new InjectedInputPoint
                    {
                        PositionX = (int)e.Position.X,
                        PositionY = (int)e.Position.Y
                    }
                }
            };

            // Inject the touch input. 
            _inputInjector.InjectTouchInput(new[] { downInfos });
        }
        private void Processor_PointerMoved(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Moved");
            var moveInfo = new InjectedInputTouchInfo
            {
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions =
                        InjectedInputPointerOptions.Update |
                        InjectedInputPointerOptions.InContact,
                    PixelLocation = new InjectedInputPoint
                    {
                        PositionX = (int)e.Position.X,
                        PositionY = (int)e.Position.Y
                    }
                }
            };
            _inputInjector.InjectTouchInput(new[] { moveInfo });
        }
        private void Processor_PointerUp(object sender, TouchPanels.PointerEventArgs e)
        {
            WriteStatus(e, "Up");
            var upInfo = new InjectedInputTouchInfo
            {
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions = InjectedInputPointerOptions.PointerUp
                }
            };
            _inputInjector.InjectTouchInput(new[] { upInfo });
        }

        private void WriteStatus(TouchPanels.PointerEventArgs args, string type)
        {
            Status.Text = $"{type}\nPosition: {args.Position.X},{args.Position.Y}\nPressure:{args.Pressure}";
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

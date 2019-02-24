using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TouchPanels;
using TouchPanels.Devices;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.Xaml;

namespace TouchPanels
{
	public sealed class TouchProcessor
	{
		private ITouchDevice device;
        private CancellationTokenSource threadCancelSource = new CancellationTokenSource();
		private bool penPressed;
        private InputInjector _inputInjector;

        public TouchProcessor(ITouchDevice device)
		{
            this.device = device ?? throw new ArgumentNullException(nameof(device));
		}

		private async void TouchProcessorLoop(CancellationToken cancellationToken)
		{
			while(!cancellationToken.IsCancellationRequested)
			{
				ReadTouchState();
				await Task.Delay(10);
			}

            _inputInjector.UninitializeTouchInjection();
        }

        private void PointerDown(Point position, double pressure)
        {
            var downInfos = new InjectedInputTouchInfo
            {
                TouchParameters = InjectedInputTouchParameters.Pressure,
                Pressure = pressure,
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions = InjectedInputPointerOptions.PointerDown | InjectedInputPointerOptions.InContact,
                    PixelLocation = new InjectedInputPoint
                    {
                        PositionX = (int)position.X,
                        PositionY = (int)position.Y
                    }
                }
            };

            // Inject the touch input. 
            _inputInjector.InjectTouchInput(new[] { downInfos });
        }

        private void PointerMoved(Point position, double pressure)
        {
            var moveInfo = new InjectedInputTouchInfo
            {
                TouchParameters = InjectedInputTouchParameters.Pressure,
                Pressure = pressure,
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions = InjectedInputPointerOptions.Update | InjectedInputPointerOptions.InContact,
                    PixelLocation = new InjectedInputPoint
                    {
                        PositionX = (int)position.X,
                        PositionY = (int)position.Y
                    }
                }
            };
            _inputInjector.InjectTouchInput(new[] { moveInfo });
        }

        private void PointerUp(Point position)
        {
            var upInfo = new InjectedInputTouchInfo
            {
                PointerInfo = new InjectedInputPointerInfo
                {
                    PointerOptions = InjectedInputPointerOptions.PointerUp,
                    PixelLocation = new InjectedInputPoint()
                    {
                        PositionX = (int)position.X,
                        PositionY = (int)position.Y,
                    }
                }
            };
            _inputInjector.InjectTouchInput(new[] { upInfo });
        }

        private void ReadTouchState()
		{
			device.ReadTouchpoints();

			var pressure = device.Pressure;
			if (pressure > 0.1d)
			{
				if (!penPressed)
				{
					penPressed = true;
                    PointerDown(device.TouchPosition, pressure);
				}
				else
				{
                    PointerMoved(device.TouchPosition, pressure);
				}
			}
			else if (pressure < 2 && penPressed == true)
			{
				penPressed = false;
                PointerUp(device.TouchPosition);
			}
		}

		public void Initialize()
		{
            //Load up the touch processor and listen for touch events
            _inputInjector = InputInjector.TryCreate();
            if (_inputInjector == null)
                throw new InvalidOperationException("Unable to create InputInjector.");

            _inputInjector.InitializeTouchInjection(InjectedInputVisualizationMode.Default);

            threadCancelSource = new CancellationTokenSource();
            Task.Run(() => TouchProcessorLoop(threadCancelSource.Token));
        }

		public void Uninitialize()
		{
			if(threadCancelSource != null)
			{
				threadCancelSource.Cancel();
				threadCancelSource = null;
            }
        }
	}
}

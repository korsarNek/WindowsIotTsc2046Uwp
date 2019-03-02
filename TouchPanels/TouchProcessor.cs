using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace TouchPanels
{
	public sealed class TouchProcessor
	{
		private Tsc2046 device;
        private CancellationTokenSource threadCancelSource = new CancellationTokenSource();
		private bool penPressed;
        private InputInjector _inputInjector;

        public TouchProcessor(Tsc2046 device)
		{
            this.device = device ?? throw new ArgumentNullException(nameof(device));
		}

        /// <summary>
        /// The minimum pressure until a touch is registered.
        /// Has to be a value between 0 and 1.
        /// </summary>
        public double PressedThreshold { get; set; } = 5d / 255;
        /// <summary>
        /// The pressure value distance from the threshold to unregister a touch.
        /// If a touch is registered at 5%, a distance of 3% means the touch is unregistered
        /// at 2%. Meaning in the range from 2% to 5% would be no change registered.
        /// Has to be a value between 0 and 1.
        /// </summary>
        public double PressedThresholdDistance { get; set; } = 3d / 255;

        public event Action<PointerEventArgs> PointerDown;
        public event Action<PointerEventArgs> PointerMove;
        public event Action<PointerEventArgs> PointerhUp;

        public struct PointerEventArgs
        {
            public readonly Point Location;
            public readonly double Pressure;

            public PointerEventArgs(Point location, double pressure)
            {
                Location = location;
                Pressure = pressure;
            }
        }

        /// <summary>
        /// Initializes the touch processor and starts listening to input.
        /// </summary>
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
            if (threadCancelSource != null)
            {
                threadCancelSource.Cancel();
                threadCancelSource = null;
            }
        }

        private void TriggerPointerDown(in Point position, in double pressure)
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

            _inputInjector.InjectTouchInput(new[] { downInfos });

            PointerDown?.Invoke(new PointerEventArgs(position, pressure));
        }

        private void TriggerPointerMoved(in Point position, in double pressure)
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

            PointerMove?.Invoke(new PointerEventArgs(position, pressure));
        }

        private void TriggerPointerUp(in Point position, in double pressure)
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

            PointerhUp?.Invoke(new PointerEventArgs(position, pressure));
        }

        private async void TouchProcessorLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ReadTouchState();
                await Task.Delay(10);
            }

            _inputInjector.UninitializeTouchInjection();
        }

        private void ReadTouchState()
		{
			device.ReadTouchData();

			var pressure = device.Pressure;
			if (pressure > PressedThreshold)
			{
				if (!penPressed)
				{
					penPressed = true;
                    TriggerPointerDown(device.TouchPosition, pressure);
				}
				else
				{
                    TriggerPointerMoved(device.TouchPosition, pressure);
				}
			}
			else if (pressure < PressedThreshold - PressedThresholdDistance && penPressed == true)
			{
				penPressed = false;
                TriggerPointerUp(device.TouchPosition, pressure);
			}
		}
	}
}

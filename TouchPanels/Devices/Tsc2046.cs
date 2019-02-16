using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using System.IO;
using Windows.Storage;
using Windows.Foundation;
using System.Globalization;

namespace TouchPanels.Devices
{
    /// <summary>
    /// The touch processor chip on the LCD device
    /// </summary>
	public sealed class Tsc2046 : ITouchDevice
	{
		private CalibrationMatrix CalibrationMatrix = new CalibrationMatrix();        //calibrate matrix
		private const int MIN_PRESSURE = 5;     //minimum pressure 1...254
		private const int CS_PIN = 1;

		private const byte CMD_START = 0x80;
		private const byte CMD_12BIT = 0x00;
		private const byte CMD_8BIT = 0x08;
		private const byte CMD_DIFF = 0x00;
		private const byte CMD_X_POS = 0x10;
		private const byte CMD_Z1_POS = 0x30;
		private const byte CMD_Z2_POS = 0x40;
		private const byte CMD_Y_POS = 0x50;

		public static SpiDevice touchSPI;

		private static Tsc2046 _DefaultInstance;
		private static Task _initTask;

		public static async Task<Tsc2046> GetDefaultAsync()
		{
			if (_DefaultInstance == null)
			{
				_DefaultInstance = new Tsc2046();
				_initTask = _DefaultInstance.InitTSC2046SPI();
			}
			await _initTask.ConfigureAwait(false);
			return _DefaultInstance;
		}

		public bool IsCalibrated
		{
			get
			{
				return CalibrationMatrix.IsValid;
			}
		}

		public Task LoadCalibrationAsync(string fileName)
		{
			return CalibrationMatrix.LoadCalDataAsync(fileName);
		}

		public Task SaveCalibrationAsync(string fileName)
		{
			return CalibrationMatrix.SaveCalDataAsync(fileName);
		}

		private async Task InitTSC2046SPI()
        {
            var touchSettings = new SpiConnectionSettings(CS_PIN);
            touchSettings.ClockFrequency = 125000;
            touchSettings.Mode = SpiMode.Mode0; //Mode0,1,2,3;  MCP23S17 needs mode 0
            string DispspiAqs = SpiDevice.GetDeviceSelector( "SPI0");
            var DispdeviceInfo = await DeviceInformation.FindAllAsync(DispspiAqs);
            touchSPI = await SpiDevice.FromIdAsync(DispdeviceInfo[0].Id, touchSettings);
            //set vars
            _lastTouchPosition = new Point(double.NaN, double.NaN);
            _currentPressure = 0;
        }

        /// <summary>
        /// Updates the calibration matrix using the 6 Affine Transformation parameters
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="e"></param>
        /// <param name="f"></param>
        public void SetCalibration(double a, double b, double c, double d, double e, double f)
        {
			CalibrationMatrix = new CalibrationMatrix(DateTime.Now, a, b, c, d, e, f);
        }

        private Point _lastTouchPosition;
        /// <summary>
        /// Last known touch position calculated with the calibration matrix
        /// </summary>
		Point ITouchDevice.TouchPosition { get { return _lastTouchPosition; } }

        private Point _lastRawTouchPosition;
        /// <summary>
		/// Raw touch position
		/// </summary>
		Point ITouchDevice.RawTouchPosition { get { return _lastRawTouchPosition; } }

        private int _currentPressure;
		/// <summary>
		/// Touch pressure
		/// </summary>
		int ITouchDevice.Pressure { get { return _currentPressure; } } 

        void ITouchDevice.ReadTouchpoints()
        {
            int p, a1, a2, b1, b2;
            int x, y;
            byte[] writeBuffer24 = new byte[3];
            byte[] readBuffer24 = new byte[3];
            
            //get pressure first to see if the screen is being touched
            writeBuffer24[0] = (byte)(CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z1_POS);
            touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
            a1 = readBuffer24[1] & 0x7F;

            writeBuffer24[0] = (byte)(CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z2_POS);
            touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
            b1 = 255 - readBuffer24[1] & 0x7F;
            p = a1 + b1;

            if (p > MIN_PRESSURE)
            {
                //using 2 samples for x and y position
                //get X data
                writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                a1 = readBuffer24[1];
                b1 = readBuffer24[2];
                writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                a2 = readBuffer24[1]; 
                b2 = readBuffer24[2]; 

                if (a1 == a2)
                {
                    x = ((a2 << 4) | (b2 >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))

                    //get Y data
                    writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS);
                    touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                    a1 = readBuffer24[1];
                    b1 = readBuffer24[2];
                    writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS);
                    touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                    a2 = readBuffer24[1];
                    b2 = readBuffer24[2];

                    if (a1 == a2)
                    {
                        y = ((a2 << 4) | (b2 >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
                        if ( x >0 && y >0)
                        {
                            _lastRawTouchPosition = new Point(x, y);
                        }
                        _currentPressure = p;
                    }
                }
            }
            else
            {
                _currentPressure = 0;
            }
			//Update display location
			if (!CalibrationMatrix.IsValid)
			{
				// we have not calibrated the PEN
				_lastTouchPosition = new Point(double.NaN, double.NaN);
			}
			else
			{
				//calc pos
				var tp = CalibrationMatrix.Transform(_lastRawTouchPosition);
                _lastTouchPosition = new Point(tp.X, tp.Y);
			}
		}
    }
}

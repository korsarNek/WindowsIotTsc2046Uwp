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
		private CalibrationMatrix CalibrationMatrix = new CalibrationMatrix();
        private const int MAX_PRESSURE = 255;
        private const int MIN_PRESSURE = 1;
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

		public async Task<bool> TryLoadCalibrationAsync(string fileName)
		{
			var matrix = await CalibrationMatrix.LoadCalDataAsync(fileName);
            if (matrix != null)
            {
                CalibrationMatrix = matrix;
                return true;
            }

            return false;
		}

		public Task SaveCalibrationAsync(string fileName)
		{
			return CalibrationMatrix.SaveCalDataAsync(fileName);
		}

		private async Task InitTSC2046SPI()
        {
            var touchSettings = new SpiConnectionSettings(CS_PIN)
            {
                ClockFrequency = 125000,
                Mode = SpiMode.Mode0 //Mode0,1,2,3;  MCP23S17 needs mode 0
            };
            string DispspiAqs = SpiDevice.GetDeviceSelector( "SPI0");
            var DispdeviceInfo = await DeviceInformation.FindAllAsync(DispspiAqs);
            if (DispdeviceInfo.Count == 0)
            {
                throw new PlatformNotSupportedException("No SPI device exists for SPI0.");
            }
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
		double ITouchDevice.Pressure { get { return (double)(_currentPressure - MIN_PRESSURE) / (MAX_PRESSURE - MIN_PRESSURE); } } 

        void ITouchDevice.ReadTouchpoints()
        {
            byte[] writeBuffer24 = new byte[3];
            byte[] readBuffer24 = new byte[3];

            var x = ReadXPosition();
            var y = ReadYPosition();

            if (x > 0 && y > 0)
            {
                _lastRawTouchPosition = new Point(x, y);
            }
            _currentPressure = ReadPressure();

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

            int ReadPressure()
            {
                writeBuffer24[0] = CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z1_POS;
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                var a = readBuffer24[1] & 0x7F;

                writeBuffer24[0] = CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z2_POS;
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                var b = 255 - readBuffer24[1] & 0x7F;
                return a + b;
            }

            int ReadXPosition()
            {
                writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                var a = readBuffer24[1];
                var b = readBuffer24[2];

                return ((a << 4) | (b >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
            }

            int ReadYPosition()
            {
                writeBuffer24[0] = (byte)(CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS);
                touchSPI.TransferFullDuplex(writeBuffer24, readBuffer24);
                var a = readBuffer24[1];
                var b = readBuffer24[2];

                return ((a << 4) | (b >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
            }
		}
    }
}

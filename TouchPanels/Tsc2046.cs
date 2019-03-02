using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Foundation;
using System.Numerics;
using System.Threading;

namespace TouchPanels
{
	public sealed class Tsc2046
	{
        private const int MAX_PRESSURE = 254;
		private const int CS_PIN = 1;

		private const byte CMD_START = 0x80;
		private const byte CMD_12BIT = 0x00;
		private const byte CMD_8BIT = 0x08;
		private const byte CMD_DIFF = 0x00;
		private const byte CMD_X_POS = 0x10;
		private const byte CMD_Z1_POS = 0x30;
		private const byte CMD_Z2_POS = 0x40;
		private const byte CMD_Y_POS = 0x50;

		private SpiDevice _touchSPI;

		private static Tsc2046 _defaultInstance;

		public static async Task<Tsc2046> GetDefaultAsync()
		{
			if (_defaultInstance == null)
			{
				_defaultInstance = new Tsc2046();
                await _defaultInstance.InitTSC2046SPI().ConfigureAwait(false);
            }
			
			return _defaultInstance;
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
                throw new PlatformNotSupportedException("No SPI device exists for SPI0 on this platform.");
            }
            _touchSPI = await SpiDevice.FromIdAsync(DispdeviceInfo[0].Id, touchSettings);
        }

        //Default calibration.
        public Matrix3x2 CalibrationMatrix { get; set; } = new Matrix3x2(-0.0215403477860236f, 0.317089232808541f, 37.3956514238408f, 0.233169889203826f, -0.0124920484301682f, 31.7292597060892f);

        /// <summary>
        /// Last known touch position calculated with the calibration matrix
        /// </summary>
		public Point TouchPosition => CalibrationMatrix.Transform(RawTouchPosition);

        /// <summary>
		/// Raw touch position
		/// </summary>
		public Point RawTouchPosition { get; private set; } = new Point(double.NaN, double.NaN);

        /// <summary>
        /// Touch pressure between 0 and 1.
        /// </summary>
        public double Pressure => (double)RawPressure / MAX_PRESSURE;

        public int RawPressure { get; private set; } = 0;

        /// <summary>
        /// Communicates with the touch processor chip on the LCD device.
        /// 
        /// <see cref="RawTouchPosition"/> and <see cref="RawPressure"/> will be updated afterwards, together
        /// with their not raw counterparts.
        /// </summary>
        public void ReadTouchData()
        {
            //TODO: Maybe we can reuse some buffers in a thread safe manner
            var writeBuffer = new byte[3];
            var readBuffer = new byte[3];

            // Do 2 samples to increase the accuracy.
            var firstSampleX = ReadXPosition();
            var firstSampleY = ReadYPosition();
            var firstSamplePressure = ReadPressure();

            var secondSampleX = ReadXPosition();
            var secondSampleY = ReadYPosition();
            var secondSamplePressure = ReadPressure();

            // Check the distance between the two samples to cancel out the noise and general inaccuracy.
            // This is necessary on the tested TSC2046.
            // On the Tsc2007 this is probably not necessary because the chip already samples and averages 7 times on a single request.
            // The Tsc2007 removes the 2 highest and 2 lowest values and averages the middle 3 values.
            if (Math.Abs(firstSampleX - secondSampleX) < 40 && Math.Abs(firstSampleY - secondSampleY) < 40 && Math.Abs(firstSamplePressure - secondSamplePressure) < 10)
            {
                // The first sample usually has a higher amount of noise, that's why we use the second one.
                RawTouchPosition = new Point(secondSampleX, secondSampleY);
                RawPressure = secondSamplePressure;
            }

            int ReadPressure()
            {
                writeBuffer[0] = CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z1_POS;
                _touchSPI.TransferFullDuplex(writeBuffer, readBuffer);
                var a = readBuffer[1] & 0x7F;

                //TODO: Reading the pressure doesn't work right, I often get the value 127
                writeBuffer[0] = CMD_START | CMD_8BIT | CMD_DIFF | CMD_Z2_POS;
                _touchSPI.TransferFullDuplex(writeBuffer, readBuffer);
                var original = readBuffer[1];
                var b = 255 - readBuffer[1] & 0x7F;
                return a + b;
            }

            int ReadXPosition()
            {
                writeBuffer[0] = CMD_START | CMD_12BIT | CMD_DIFF | CMD_X_POS;
                _touchSPI.TransferFullDuplex(writeBuffer, readBuffer);
                var a = readBuffer[1];
                var b = readBuffer[2];

                return ((a << 4) | (b >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
            }

            int ReadYPosition()
            {
                writeBuffer[0] = CMD_START | CMD_12BIT | CMD_DIFF | CMD_Y_POS;
                _touchSPI.TransferFullDuplex(writeBuffer, readBuffer);
                var a = readBuffer[1];
                var b = readBuffer[2];

                return ((a << 4) | (b >> 4)); //12bit: ((a<<4)|(b>>4)) //10bit: ((a<<2)|(b>>6))
            }
		}
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TouchPanels.UI;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace TouchPanels
{
    //TODO: Move this class out to another project and make this project a .net core library?
    public static class Manager
    {
        private static Tsc2046 _device;
        private static TouchProcessor _processor;

        public static async Task LoadCalibrationMatrix()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalCacheFolder;

            var item = await storageFolder.TryGetItemAsync("tsc2046.cal");
            if (item == null)
                return; //TODO: error handling, maybe throw exceptions?

            if (!(item is StorageFile calFile))
                return; //TODO: error handling, maybe throw exceptions?

            string CalData = await FileIO.ReadTextAsync(calFile);
            string[] data = CalData.Split(new char[] { ',' });
            if (data.Length != 6)
                return; //TODO: error handling, maybe throw exceptions?

            var matrix = new Matrix3x2(
                Convert.ToSingle(data[0], CultureInfo.InvariantCulture),
                Convert.ToSingle(data[1], CultureInfo.InvariantCulture),
                Convert.ToSingle(data[2], CultureInfo.InvariantCulture),
                Convert.ToSingle(data[3], CultureInfo.InvariantCulture),
                Convert.ToSingle(data[4], CultureInfo.InvariantCulture),
                Convert.ToSingle(data[5], CultureInfo.InvariantCulture)
            );

            _device.CalibrationMatrix = matrix;
        }

        public static async Task SaveCalibrationMatrix()
        {
            var matrix = _device.CalibrationMatrix;

            string[] lines = {
                matrix.M11.ToString(CultureInfo.InvariantCulture),
                matrix.M12.ToString(CultureInfo.InvariantCulture),
                matrix.M21.ToString(CultureInfo.InvariantCulture),
                matrix.M22.ToString(CultureInfo.InvariantCulture),
                matrix.M31.ToString(CultureInfo.InvariantCulture),
                matrix.M32.ToString(CultureInfo.InvariantCulture)
            };

            var storageFolder = ApplicationData.Current.LocalCacheFolder;
            var file = await storageFolder.CreateFileAsync("tsc2046.cal", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, String.Join(",", lines));
        }

        public static async Task Calibrate(CalibrationStyle style)
        {
            var bounds = Windows.UI.Core.CoreWindow.GetForCurrentThread().Bounds;
            LcdCalibrationView view = new LcdCalibrationView(_device)
            {
                Width = bounds.Width,
                Height = bounds.Height
            };

            Popup p = new Popup() { Child = view, Margin = new Thickness(0), IsOpen = true };
            // Disable normal touch events, they interfere with the calibration.
            _processor.Uninitialize();
            try
            {
                var calibrationParameters = await view.CalibrateAsync(style);
                _device.CalibrationMatrix = calibrationParameters.ToMatrix3x2();
            }
            finally
            {
                p.IsOpen = false;
                _processor.Initialize();
            }
        }

        public static async Task StartDevice()
        {
            //TODO: do we need to throw an error if the processor has already been initialized?
            _device = await Tsc2046.GetDefaultAsync();
            _processor = new TouchProcessor(_device);
            _processor.Initialize();
        }

        public static void StopDevice()
        {
            _processor.Uninitialize();
        }   
    }
}

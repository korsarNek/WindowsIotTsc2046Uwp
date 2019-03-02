using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TouchPanels.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    internal sealed partial class LcdCalibrationView : Page
	{
        private Tsc2046 _device;
        private TaskCompletionSource<bool> _loaded = new TaskCompletionSource<bool>();

		public LcdCalibrationView(Tsc2046 device)
		{
			this._device = device;

			this.InitializeComponent();
            this.Loaded += (s, e) => _loaded.SetResult(true);
        }

        public async Task<AffineTransformationParameters> CalibrateAsync(CalibrationStyle style)
		{
            await _loaded.Task;

			double margin = 50;
            List<Point> references = new List<Point>();
			references.Add(new Point(margin, margin)); //Top left
            if (style == CalibrationStyle.SevenPoint)
			    references.Add(new Point(this.ActualWidth * .5, margin)); //Top center
			references.Add(new Point(this.ActualWidth - margin, margin)); //Top right
			references.Add(new Point(this.ActualWidth - margin, this.ActualHeight - margin)); //Bottom right
            if (style == CalibrationStyle.SevenPoint)
                references.Add(new Point(this.ActualWidth * .5, this.ActualHeight - margin)); //Bottom center
			references.Add(new Point(margin, this.ActualHeight - margin)); //Bottom left
            if (style == CalibrationStyle.CornersAndCenter || style == CalibrationStyle.SevenPoint)
                references.Add(new Point(this.ActualWidth * .5, this.ActualHeight * .5)); //Center

            List<Point> measurements = new List<Point>();
            foreach (var reference in references)
			{
				progress.Value = measurements.Count * 100d / references.Count;
				CalibrationMarker.RenderTransform = new TranslateTransform() { X = reference.X, Y = reference.Y };
				var measurement = await GetRawTouchEventAsync();

                measurements.Add(measurement);
            }
			return LeastSquaresAdjustment.GetTransformation(references, measurements);
		}

		private async Task<Point> GetRawTouchEventAsync()
		{
            while (_device.Pressure >= 0.1d) //Ensure user has let go
			{
				await Task.Delay(5);
				_device.ReadTouchData();
			}
			//wait for pen pressure
			while (_device.Pressure < 0.1d) {
				await Task.Delay(5);
				_device.ReadTouchData();
			}
			Point p = _device.RawTouchPosition;
            while (_device.Pressure >= 0.1d) //Ensure user has let go again
			{
				p = _device.RawTouchPosition;
				await Task.Delay(5);
				_device.ReadTouchData();
			}
			return p;
		}
	}
}

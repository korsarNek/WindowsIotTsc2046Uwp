using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouchPanels.Algorithms;
using TouchPanels.Devices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TouchPanels.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LcdCalibrationView : Page
	{
        public enum CalibrationStyle
        {
            FourCorners = 4,
            CornersAndCenter = 5,
            SevenPoint = 7
        }
        private ITouchDevice device;
        public static async Task<AffineTransformationParameters> CalibrateScreenAsync(ITouchDevice device, CalibrationStyle style = CalibrationStyle.CornersAndCenter)
		{
			var bounds = Windows.UI.Core.CoreWindow.GetForCurrentThread().Bounds;
			LcdCalibrationView view = new LcdCalibrationView(device);
			view.Width = bounds.Width;
			view.Height = bounds.Height;

			Popup p = new Popup() { Child = view, Margin = new Thickness(0), IsOpen = true };
            try
			{
				return await view.CalibrateAsync(style);
			}
			finally
			{
				p.IsOpen = false;
			}
		}
		TaskCompletionSource<object> _LoadTask = new TaskCompletionSource<object>();
		private LcdCalibrationView(ITouchDevice device)
		{
			this.device = device;
			this.InitializeComponent();
			_LoadTask = new TaskCompletionSource<object>();
			this.Loaded += (s, e) => _LoadTask.SetResult(null);
		}
		private async Task<AffineTransformationParameters> CalibrateAsync(CalibrationStyle style)
		{
			await _LoadTask.Task;

			double margin = 50;
			List<Tuple<Point, Point>> measurements = new List<Tuple<Point, Point>>();
			measurements.Add(new Tuple<Point, Point>(new Point(margin, margin), new Point())); //Top left
            if(style == CalibrationStyle.SevenPoint)
			    measurements.Add(new Tuple<Point, Point>(new Point(this.ActualWidth * .5, margin), new Point())); //Top center
			measurements.Add(new Tuple<Point, Point>(new Point(this.ActualWidth - margin, margin), new Point())); //Top right
			measurements.Add(new Tuple<Point, Point>(new Point(this.ActualWidth - margin, this.ActualHeight - margin), new Point())); //Bottom right
            if (style == CalibrationStyle.SevenPoint)
                measurements.Add(new Tuple<Point, Point>(new Point(this.ActualWidth * .5, this.ActualHeight - margin), new Point())); //Bottom center
			measurements.Add(new Tuple<Point, Point>(new Point(margin, this.ActualHeight - margin), new Point())); //Bottom left
            if (style == CalibrationStyle.CornersAndCenter || style == CalibrationStyle.SevenPoint)
                measurements.Add(new Tuple<Point, Point>(new Point(this.ActualWidth * .5, this.ActualHeight * .5), new Point())); //Center

			for (int i = 0; i < measurements.Count; i++)
			{
				progress.Value = i * 100d / measurements.Count;
				var p = measurements[i].Item1;
				CalibrationMarker.RenderTransform = new TranslateTransform() { X = p.X, Y = p.Y };
				var p1 = await GetRawTouchEventAsync();
				measurements[i] = new Tuple<Point, Point>(p, p1);
			}
			var lsa = new LeastSquaresAdjustment(measurements.Select(t => t.Item1), measurements.Select(t => t.Item2));
			
			return lsa.GetTransformation();
		}

		private async Task<Point> GetRawTouchEventAsync()
		{
            while (device.Pressure >= 1) //Ensure user has let go
			{
				await Task.Delay(5);
				device.ReadTouchpoints();
			}
			//wait for pen pressure
			while (device.Pressure < 5) {
				await Task.Delay(5);
				device.ReadTouchpoints();
			}
			Point p = device.RawTouchPosition;
            while (device.Pressure >= 1) //Ensure user has let go again
			{
				p = device.RawTouchPosition;
				await Task.Delay(5);
				device.ReadTouchpoints();
			}
			return p;
		}
	}
}

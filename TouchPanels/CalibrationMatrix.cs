using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace TouchPanels
{
	/// <summary>
	/// Calibration matrix for touchpanel
	/// </summary>
	internal class CalibrationMatrix
	{
		private object datalock = new object();
		public Point Transform(Point input)
		{
			return new Point(
			 a * input.X + b * input.Y + c,
			 d * input.X + e * input.Y + f
			 );
		}
		public DateTime LastUpdated { get; internal set; } = DateTime.MinValue;
		public double a { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		public double b { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		public double c { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		public double d { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		public double e { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		public double f { get; private set; } = double.NaN; //TODO: Returns should be locked on 'lockdata'
		
		public bool IsValid { get { return !double.IsNaN(a * b * c * d * e * f); } }

		public CalibrationMatrix()
		{
		}
		public CalibrationMatrix(DateTime cal_LastUpdated, double cal_a, double cal_b, double cal_c, double cal_d, double cal_e, double cal_f)
		{
			LastUpdated = cal_LastUpdated;
			a = cal_a;
			b = cal_b;
			c = cal_c;
			d = cal_d;
			e = cal_e;
			f = cal_f;
		}
		/// <summary>
		/// Stores an array of Cal parameters to a given file name
		/// </summary>
		/// <param name="fileName"></param>
		public async Task SaveCalDataAsync(string fileName)
		{
			// We change file extension here to make sure it's a .csv file.
			string[] lines = {
				LastUpdated.ToString(CultureInfo.InvariantCulture),
				a.ToString(CultureInfo.InvariantCulture),
				b.ToString(CultureInfo.InvariantCulture),
				c.ToString(CultureInfo.InvariantCulture),
				d.ToString(CultureInfo.InvariantCulture),
				e.ToString(CultureInfo.InvariantCulture),
				f.ToString(CultureInfo.InvariantCulture)
			};
			
			var storageFolder = KnownFolders.DocumentsLibrary;
			var file = await storageFolder.CreateFileAsync(Path.ChangeExtension(fileName, ".cal"), CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, String.Join(",", lines));
		}
		/// <summary>
		/// Reads the touch screen calibration data and applies it to the touch routines
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns><c>true</c> if the file was found</returns>
		public async Task LoadCalDataAsync(string fileName)
		{
			// We change file extension here to make sure it's a .csv file.
			StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
			StorageFile calFile = await storageFolder.GetFileAsync(System.IO.Path.ChangeExtension(fileName, ".cal"));
			string CalData = await FileIO.ReadTextAsync(calFile);
			string[] data = CalData.Split(new char[] { ',' });
			// We return a calibration data file with the data in order.
			// we first store in local variables in case an exception happens and we end up in a bad half-loaded state
			var lastUpdated = Convert.ToDateTime(data[0], CultureInfo.InvariantCulture);
			var a = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
			var b = Convert.ToDouble(data[2], CultureInfo.InvariantCulture);
			var c = Convert.ToDouble(data[3], CultureInfo.InvariantCulture);
			var d = Convert.ToDouble(data[4], CultureInfo.InvariantCulture);
			var e = Convert.ToDouble(data[5], CultureInfo.InvariantCulture);
			var f = Convert.ToDouble(data[6], CultureInfo.InvariantCulture);
			lock(datalock)
			{
				this.LastUpdated = lastUpdated;
				this.a = a;
				this.b = b;
				this.c = c;
				this.d = d;
				this.e = e;
				this.f = f;
			}
		}
	};
}

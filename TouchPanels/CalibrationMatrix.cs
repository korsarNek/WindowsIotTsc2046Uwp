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
		public Point Transform(Point input)
		{
			return new Point(
			     A * input.X + B * input.Y + C,
			     D * input.X + E * input.Y + F
			 );
		}
		public DateTime LastUpdated { get; internal set; } = DateTime.MinValue;
		public double A { get; private set; } = double.NaN;
		public double B { get; private set; } = double.NaN;
		public double C { get; private set; } = double.NaN;
		public double D { get; private set; } = double.NaN;
		public double E { get; private set; } = double.NaN;
		public double F { get; private set; } = double.NaN;
		
		public bool IsValid { get { return !double.IsNaN(A * B * C * D * E * F); } }

		public CalibrationMatrix()
		{
		}
		public CalibrationMatrix(DateTime cal_LastUpdated, double cal_a, double cal_b, double cal_c, double cal_d, double cal_e, double cal_f)
		{
			LastUpdated = cal_LastUpdated;
			A = cal_a;
			B = cal_b;
			C = cal_c;
			D = cal_d;
			E = cal_e;
			F = cal_f;
		}

        //TODO: Redo the save and load calibrations stuff, this should be configurable and maybe not internal.
		/// <summary>
		/// Stores an array of Cal parameters to a given file name
		/// </summary>
		/// <param name="fileName"></param>
		public async Task SaveCalDataAsync(string fileName)
		{
			// We change file extension here to make sure it's a .csv file.
			string[] lines = {
				LastUpdated.ToString(CultureInfo.InvariantCulture),
				A.ToString(CultureInfo.InvariantCulture),
				B.ToString(CultureInfo.InvariantCulture),
				C.ToString(CultureInfo.InvariantCulture),
				D.ToString(CultureInfo.InvariantCulture),
				E.ToString(CultureInfo.InvariantCulture),
				F.ToString(CultureInfo.InvariantCulture)
			};
			
			var storageFolder = KnownFolders.DocumentsLibrary;
			var file = await storageFolder.CreateFileAsync(Path.ChangeExtension(fileName, ".cal"), CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(file, String.Join(",", lines));
		}
		/// <summary>
		/// Reads the touch screen calibration data and applies it to the touch routines
		/// </summary>
		/// <param name="fileName"></param>
		public static async Task<CalibrationMatrix> LoadCalDataAsync(string fileName)
		{
			// We change file extension here to make sure it's a .csv file.
			StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
            var path = Path.ChangeExtension(fileName, ".cal");

            var item = await storageFolder.TryGetItemAsync(path);
            if (item == null)
                return null;

			var calFile = item as StorageFile;
            if (calFile == null)
                return null;

			string CalData = await FileIO.ReadTextAsync(calFile);
			string[] data = CalData.Split(new char[] { ',' });
            // We return a calibration data file with the data in order.

            return new CalibrationMatrix(
                cal_LastUpdated: Convert.ToDateTime(data[0], CultureInfo.InvariantCulture),
                cal_a: Convert.ToDouble(data[1], CultureInfo.InvariantCulture),
                cal_b: Convert.ToDouble(data[2], CultureInfo.InvariantCulture),
                cal_c: Convert.ToDouble(data[3], CultureInfo.InvariantCulture),
                cal_d: Convert.ToDouble(data[4], CultureInfo.InvariantCulture),
                cal_e: Convert.ToDouble(data[5], CultureInfo.InvariantCulture),
                cal_f: Convert.ToDouble(data[6], CultureInfo.InvariantCulture)
            );
		}
	};
}

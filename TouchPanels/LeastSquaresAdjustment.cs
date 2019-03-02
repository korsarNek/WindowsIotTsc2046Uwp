using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;

namespace TouchPanels
{
	/// <summary>
	/// Performs a least squares adjustment between input and output points and
	/// returns the conversion parameters.
	/// </summary>
	internal static class LeastSquaresAdjustment
	{
		/// <summary>
		/// Returns transformation parameters to coordinatesystem
		/// Return an array with the six affine transformation parameters {a,b,c,d,e,f}
		/// a,b defines vector 1 of coordinate system, d,e vector 2.
		/// c,f defines image center.
		/// Converting from input (X,Y) to output coordinate system (X',Y') is done by:
		/// X' = a*X + b*Y + c, Y' = d*X + e*Y + f
		/// Transformation based on Mikhail "Introduction to Modern Photogrammetry" p. 399-300
		/// Extended to arbitrary number of points by M. Nielsen
		/// </summary>
		/// <returns>Six transformation parameters a,b,c,d,e,f for the affine transformation</returns>
		public static AffineTransformationParameters GetTransformation(IEnumerable<Point> inputElements, IEnumerable<Point> outputElements)
		{
            var inputs = inputElements.ToArray();
            var outputs = outputElements.ToArray();
            if (inputs.Length != outputs.Length)
                throw new ArgumentException("Inputs and Outputs collections must contain the same number of items");

            if (inputs.Length < 3)
				throw new ArgumentException("At least 3 measurements required to calculate affine transformation");

			int count = inputs.Length;
			double[,] N = new double[3,3];
			//Create normal equation: transpose(B)*B
			//B: matrix of calibrated values. Example of row in B: [x , y , -1]
			for (int i = 0; i < count; i++)
			{
				N[0,0] += Math.Pow(outputs[i].X, 2);
				N[0,1] += outputs[i].X * outputs[i].Y;
				N[0,2] += -outputs[i].X;
				N[1,1] += Math.Pow(outputs[i].Y, 2);
				N[1,2] += -outputs[i].Y;
			}
			N[2,2] = count;

			double[] t1 = new double[3];
			double[] t2 = new double[3];

			for (int i = 0; i < count; i++)
			{
				t1[0] += outputs[i].X * inputs[i].X;
				t1[1] += outputs[i].Y * inputs[i].X;
				t1[2] += -inputs[i].X;

				t2[0] += outputs[i].X * inputs[i].Y;
				t2[1] += outputs[i].Y * inputs[i].Y;
				t2[2] += -inputs[i].Y;
			}
			
			// Solve equation N = transpose(B)*t1
			var result = new AffineTransformationParameters();
            double frac = 1 / (-N[0,0] * N[1,1] * N[2,2] + N[0,0] * Math.Pow(N[1,2], 2) + Math.Pow(N[0,1], 2) * N[2,2] - 2 * N[1,2] * N[0,1] * N[0,2] + N[1,1] * Math.Pow(N[0,2], 2));
			result.A = (-N[0,1] * N[1,2] * t1[2] + N[0,1] * t1[1] * N[2,2] - N[0,2] * N[1,2] * t1[1] + N[0,2] * N[1,1] * t1[2] - t1[0] * N[1,1] * N[2,2] + t1[0] * Math.Pow(N[1,2], 2)) * frac;
			result.B = (-N[0,1] * N[0,2] * t1[2] + N[0,1] * t1[0] * N[2,2] + N[0,0] * N[1,2] * t1[2] - N[0,0] * t1[1] * N[2,2] - N[0,2] * N[1,2] * t1[0] + Math.Pow(N[0,2], 2) * t1[1]) * frac;
			result.C = -(-N[1,2] * N[0,1] * t1[0] + Math.Pow(N[0,1], 2) * t1[2] + N[0,0] * N[1,2] * t1[1] - N[0,0] * N[1,1] * t1[2] - N[0,2] * N[0,1] * t1[1] + N[1,1] * N[0,2] * t1[0]) * frac;
			// Solve equation N = transpose(B)*t2
			result.D = (-N[0,1] * N[1,2] * t2[2] + N[0,1] * t2[1] * N[2,2] - N[0,2] * N[1,2] * t2[1] + N[0,2] * N[1,1] * t2[2] - t2[0] * N[1,1] * N[2,2] + t2[0] * Math.Pow(N[1,2], 2)) * frac;
			result.E = (-N[0,1] * N[0,2] * t2[2] + N[0,1] * t2[0] * N[2,2] + N[0,0] * N[1,2] * t2[2] - N[0,0] * t2[1] * N[2,2] - N[0,2] * N[1,2] * t2[0] + Math.Pow(N[0,2], 2) * t2[1]) * frac;
			result.F = -(-N[1,2] * N[0,1] * t2[0] + Math.Pow(N[0,1], 2) * t2[2] + N[0,0] * N[1,2] * t2[1] - N[0,0] * N[1,1] * t2[2] - N[0,2] * N[0,1] * t2[1] + N[1,1] * N[0,2] * t2[0]) * frac;
			
			//Calculate s0
			double s0 = 0;
			for (int i = 0; i < count; i++)
			{
				var tt = result.Transform(outputs[i]);
				s0 += Math.Pow(tt.X - inputs[i].X, 2) + Math.Pow(tt.Y - inputs[i].Y, 2);
			}
			result.s0 = Math.Sqrt(s0) / count;
			return result;
		}
	}

    internal class AffineTransformationParameters
	{
		public double A { get; set; }
		public double B { get; set; }
		public double C { get; set; }
		public double D { get; set; }
		public double E { get; set; }
		public double F { get; set; }
		public double s0 { get; set; }

		public Point Transform(Point input)
		{
			return new Point(
			     A * input.X + B * input.Y + C,
			     D * input.X + E * input.Y + F
			 );
		}

        public Matrix3x2 ToMatrix3x2() => new Matrix3x2((float)A, (float)B, (float)C, (float)D, (float)E, (float)F);
	}
}

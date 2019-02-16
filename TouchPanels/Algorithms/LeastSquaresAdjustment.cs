using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace TouchPanels.Algorithms
{
	/// <summary>
	/// Performs a least squares adjustment between input and output points and
	/// returns the conversion parameters.
	/// </summary>
	internal class LeastSquaresAdjustment
	{
		private List<Point> _inputs;
		private List<Point> _outputs;

		/// <summary>
		/// Initialize Least Squares transformations
		/// </summary>
		public LeastSquaresAdjustment() : this(Enumerable.Empty<Point>(), Enumerable.Empty<Point>())
		{
		}

		/// <summary>
		/// Initialize Least Squares transformations
		/// </summary>
		public LeastSquaresAdjustment(IEnumerable<Point> inputs, IEnumerable<Point> outputs)
		{
			_inputs = new List<Point>(inputs);
			_outputs = new List<Point>(outputs);
			if (_inputs.Count != _outputs.Count)
				throw new ArgumentException("Inputs and Outputs collections must contain the same number of items");
		}

		/// <summary>
		/// Adds an input and output value pair to the collection
		/// </summary>
		/// <param name="input"></param>
		/// <param name="output"></param>
		public void AddInputOutputPoint(Point input, Point output)
		{
			_inputs.Add(input);
			_outputs.Add(output);
		}

		/// <summary>
		/// Removes input and output value pair at the specified index
		/// </summary>
		/// <param name="i"></param>
		public void RemoveInputOutputPointAt(int i)
		{
			_inputs.RemoveAt(i);
			_outputs.RemoveAt(i);
		}

		/// <summary>
		/// Gets the input point value at the specified index
		/// </summary>
		/// <param name="i">index</param>
		/// <returns>Input point value a index 'i'</returns>
		public Point GetInputPoint(int i)
		{
			return _inputs[i];
		}

		/// <summary>
		/// Sets the input point value at the specified index
		/// </summary>
		/// <param name="p">Point value</param>
		/// <param name="i">index</param>
		public void SetInputPointAt(Point p, int i)
		{
			_inputs[i] = p;
		}

		/// <summary>
		/// Gets the output point value at the specified index
		/// </summary>
		/// <param name="i">index</param>
		/// <returns>Output point value a index 'i'</returns>
		public Point GetOutputPoint(int i)
		{
			return _outputs[i];
		}

		/// <summary>
		/// Sets the output point value at the specified index
		/// </summary>
		/// <param name="p">Point value</param>
		/// <param name="i">index</param>
		public void SetOutputPointAt(Point p, int i)
		{
			_outputs[i] = p;
		}

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
		public AffineTransformationParameters GetTransformation()
		{
			if (_inputs.Count < 3)
				throw (new System.Exception("At least 3 measurements required to calculate affine transformation"));

			int count = _inputs.Count;
            Point[] outputs = _outputs.ToArray();
			Point[] inputs = _inputs.ToArray();
			double[][] N = CreateMatrix(3, 3);
			//Create normal equation: transpose(B)*B
			//B: matrix of calibrated values. Example of row in B: [x , y , -1]
			for (int i = 0; i < count; i++)
			{
				N[0][0] += Math.Pow(outputs[i].X, 2);
				N[0][1] += outputs[i].X * outputs[i].Y;
				N[0][2] += -outputs[i].X;
				N[1][1] += Math.Pow(outputs[i].Y, 2);
				N[1][2] += -outputs[i].Y;
			}
			N[2][2] = count;

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
            double frac = 1 / (-N[0][0] * N[1][1] * N[2][2] + N[0][0] * Math.Pow(N[1][2], 2) + Math.Pow(N[0][1], 2) * N[2][2] - 2 * N[1][2] * N[0][1] * N[0][2] + N[1][1] * Math.Pow(N[0][2], 2));
			result.A = (-N[0][1] * N[1][2] * t1[2] + N[0][1] * t1[1] * N[2][2] - N[0][2] * N[1][2] * t1[1] + N[0][2] * N[1][1] * t1[2] - t1[0] * N[1][1] * N[2][2] + t1[0] * Math.Pow(N[1][2], 2)) * frac;
			result.B = (-N[0][1] * N[0][2] * t1[2] + N[0][1] * t1[0] * N[2][2] + N[0][0] * N[1][2] * t1[2] - N[0][0] * t1[1] * N[2][2] - N[0][2] * N[1][2] * t1[0] + Math.Pow(N[0][2], 2) * t1[1]) * frac;
			result.C = -(-N[1][2] * N[0][1] * t1[0] + Math.Pow(N[0][1], 2) * t1[2] + N[0][0] * N[1][2] * t1[1] - N[0][0] * N[1][1] * t1[2] - N[0][2] * N[0][1] * t1[1] + N[1][1] * N[0][2] * t1[0]) * frac;
			// Solve equation N = transpose(B)*t2
			result.D = (-N[0][1] * N[1][2] * t2[2] + N[0][1] * t2[1] * N[2][2] - N[0][2] * N[1][2] * t2[1] + N[0][2] * N[1][1] * t2[2] - t2[0] * N[1][1] * N[2][2] + t2[0] * Math.Pow(N[1][2], 2)) * frac;
			result.E = (-N[0][1] * N[0][2] * t2[2] + N[0][1] * t2[0] * N[2][2] + N[0][0] * N[1][2] * t2[2] - N[0][0] * t2[1] * N[2][2] - N[0][2] * N[1][2] * t2[0] + Math.Pow(N[0][2], 2) * t2[1]) * frac;
			result.F = -(-N[1][2] * N[0][1] * t2[0] + Math.Pow(N[0][1], 2) * t2[2] + N[0][0] * N[1][2] * t2[1] - N[0][0] * N[1][1] * t2[2] - N[0][2] * N[0][1] * t2[1] + N[1][1] * N[0][2] * t2[0]) * frac;
			
			//Calculate s0
			double s0 = 0;
			for (int i = 0; i < this._inputs.Count; i++)
			{
				var tt = result.Transform(_outputs[i]);
				s0 += Math.Pow(tt.X - _inputs[i].X, 2) + Math.Pow(tt.Y - _inputs[i].Y, 2);
			}
			result.s0 = Math.Sqrt(s0) / (this._inputs.Count);
			return result;
		}
		/// <summary>
		/// Calculates the four helmert transformation parameters {a,b,c,d} and the sum of the squares of the residuals (s0)
		/// </summary>
		/// <remarks>
		/// <para>
		/// a,b defines scale vector 1 of coordinate system, d,e scale vector 2.
		/// c,f defines offset.
		/// </para>
		/// <para>
		/// Converting from input (X,Y) to output coordinate system (X',Y') is done by:
		/// X' = a*X + b*Y + c, Y' = -b*X + a*Y + d
		/// </para>
		/// <para>This is a transformation initially based on the affine transformation but slightly simpler.</para>
		/// </remarks>
		/// <returns>Array with the four transformation parameters, and sum of squared residuals: a,b,c,d,s0</returns>
		public HelmertTransformationParameters GetHelmertTransformation() 
		{
			throw new NotSupportedException("This method returns an incorrect transformation - do not use");
			if (_inputs.Count < 2)
				throw(new System.Exception("At least 2 measurements required to calculate helmert transformation"));
			
			double b00=0;
			double b02=0;
			double b03=0;
			double[] t = new double[4];
			for (int i = 0; i < _inputs.Count; i++)
			{
				//Calculate summed values
				b00 += Math.Pow(_inputs[i].X, 2) + Math.Pow(_inputs[i].Y, 2);
				b02 -= _inputs[i].X;
				b03 -= _inputs[i].Y;
				t[0] += -(_inputs[i].X * _outputs[i].X) - (_inputs[i].Y * _outputs[i].Y);
				t[1] += -(_inputs[i].Y * _outputs[i].X) + (_inputs[i].X * _outputs[i].Y);
				t[2] += _outputs[i].X;
				t[3] += _outputs[i].Y;
			}
			double frac = 1 / (-_inputs.Count * b00 + Math.Pow(b02, 2) + Math.Pow(b03, 2));
			var result = new HelmertTransformationParameters();
			result.A = (-_inputs.Count * t[0] + b02 * t[2] + b03 * t[3]) * frac;
			result.B = (-_inputs.Count * t[1] + b03 * t[2] - b02 * t[3]) * frac;
			result.C = (b02 * t[0] + b03 * t[1] - t[2] * b00) * frac;
			result.D = (b03 * t[0] - b02 * t[1] - t[3] * b00) * frac;
			
			//Calculate s0
			double s0=0;
			for (int i = 0; i < _inputs.Count; i++) 
			{
				var tt = result.Transform(_outputs[i]);
				s0 += Math.Pow(tt.X - _inputs[i].X, 2) + Math.Pow(tt.Y - _inputs[i].Y, 2);
			}
			result.s0 = Math.Sqrt(s0) / (_inputs.Count);
			return result;
		}

		/// <summary>
		/// Creates an n x m matrix of doubles
		/// </summary>
		/// <param name="n">width of matrix</param>
		/// <param name="m">height of matrix</param>
		/// <returns>n*m matrix</returns>
		private double[][] CreateMatrix(int n, int m) 
		{
			double[][] N = new double[n][];
			for(int i=0;i<n;i++) 
			{
				N[i] = new double[m];
			}
			return N;
		}
	}
    
    internal class HelmertTransformationParameters
	{
		public double A { get; set; }
		public double B { get; set; }
		public double C { get; set; }
		public double D { get; set; }
		public double s0 { get; set; }
		public Point Transform(Point input)
		{
			return new Point(
			 A * input.X + B * input.Y + C,
			 -B * input.X + A * input.Y + D
			 );
		}
	}

    public class AffineTransformationParameters
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
	}
}

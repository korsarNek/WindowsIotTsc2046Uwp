using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace TouchPanels
{
    internal static class Extensions
    {
        public static Point Transform(in this Matrix3x2 matrix, in Point point)
        {
            return new Point(
                matrix.M11 * point.X + matrix.M12 * point.Y + matrix.M21,
                matrix.M22 * point.X + matrix.M31 * point.Y + matrix.M32
            );
        }
    }
}

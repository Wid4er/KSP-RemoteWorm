using System;

namespace RemoteTechWormholeBridge.Core.Geometry
{
    public struct Vector3Value
    {
        public Vector3Value(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public double Magnitude
        {
            get { return Math.Sqrt(X * X + Y * Y + Z * Z); }
        }

        public Vector3Value Normalized()
        {
            double magnitude = Magnitude;
            if (magnitude <= 0 || Double.IsNaN(magnitude) || Double.IsInfinity(magnitude))
                throw new ArgumentException("A radial vector must be finite and non-zero.");

            return new Vector3Value(X / magnitude, Y / magnitude, Z / magnitude);
        }

        public Vector3Value Scale(double scalar)
        {
            return new Vector3Value(X * scalar, Y * scalar, Z * scalar);
        }

        public static double Dot(Vector3Value first, Vector3Value second)
        {
            return first.X * second.X + first.Y * second.Y + first.Z * second.Z;
        }
    }
}

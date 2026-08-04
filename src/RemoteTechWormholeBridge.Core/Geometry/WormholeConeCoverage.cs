using System;

namespace RemoteTechWormholeBridge.Core.Geometry
{
    public sealed class DirectionalCoverage
    {
        internal DirectionalCoverage(double angularErrorDegrees, double halfAngleDegrees, bool covers)
        {
            AngularErrorDegrees = angularErrorDegrees;
            HalfAngleDegrees = halfAngleDegrees;
            Covers = covers;
        }

        public double AngularErrorDegrees { get; private set; }
        public double HalfAngleDegrees { get; private set; }
        public bool Covers { get; private set; }
    }

    public sealed class BidirectionalCoverage
    {
        internal BidirectionalCoverage(DirectionalCoverage aToB, DirectionalCoverage bToA)
        {
            AToB = aToB;
            BToA = bToA;
        }

        public DirectionalCoverage AToB { get; private set; }
        public DirectionalCoverage BToA { get; private set; }
        public bool Active { get { return AToB.Covers && BToA.Covers; } }
    }

    public static class WormholeConeCoverage
    {
        private const double RadiansToDegrees = 180.0 / Math.PI;

        public static DirectionalCoverage Evaluate(
            Vector3Value expectedOutputRadial,
            Vector3Value receiverRadial,
            double cosHalfAngle)
        {
            Vector3Value expected = expectedOutputRadial.Normalized();
            Vector3Value receiver = receiverRadial.Normalized();
            double clampedCosHalfAngle = Clamp(cosHalfAngle, -1.0, 1.0);
            double alignment = Clamp(Vector3Value.Dot(expected, receiver), -1.0, 1.0);
            double error = Math.Acos(alignment) * RadiansToDegrees;
            double halfAngle = Math.Acos(clampedCosHalfAngle) * RadiansToDegrees;

            return new DirectionalCoverage(error, halfAngle, alignment >= clampedCosHalfAngle);
        }

        public static BidirectionalCoverage EvaluateIdentityPair(
            Vector3Value radialA,
            double cosHalfAngleA,
            Vector3Value radialB,
            double cosHalfAngleB)
        {
            return new BidirectionalCoverage(
                Evaluate(radialA, radialB, cosHalfAngleA),
                Evaluate(radialB, radialA, cosHalfAngleB));
        }

        public static Vector3Value PointOnTransitionSurface(Vector3Value radial, double transitionRadius)
        {
            if (transitionRadius <= 0 || Double.IsNaN(transitionRadius) || Double.IsInfinity(transitionRadius))
                throw new ArgumentOutOfRangeException("transitionRadius");

            return radial.Normalized().Scale(transitionRadius);
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            if (value < minimum)
                return minimum;
            if (value > maximum)
                return maximum;
            return value;
        }
    }
}

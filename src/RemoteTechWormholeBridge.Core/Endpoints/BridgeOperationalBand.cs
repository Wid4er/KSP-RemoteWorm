using System;

namespace RemoteTechWormholeBridge.Core.Endpoints
{
    public sealed class BridgeOperationalBand
    {
        public const double MaximumDesiredLocalDistance = 300000.0;
        public const double MinimumSafeLocalDistance = 5000.0;
        public const double UsableSpaceFraction = 0.80;

        private BridgeOperationalBand(
            double transitionRadius,
            double sphereOfInfluence,
            double availableSpace,
            double minimumLocalDistance,
            double maximumLocalDistance)
        {
            TransitionRadius = transitionRadius;
            SphereOfInfluence = sphereOfInfluence;
            AvailableSpace = availableSpace;
            MinimumLocalDistance = minimumLocalDistance;
            MaximumLocalDistance = maximumLocalDistance;
        }

        public double TransitionRadius { get; private set; }
        public double SphereOfInfluence { get; private set; }
        public double AvailableSpace { get; private set; }
        public double MinimumLocalDistance { get; private set; }
        public double MaximumLocalDistance { get; private set; }

        public double InnerRadius
        {
            get { return TransitionRadius + MinimumLocalDistance; }
        }

        public double OuterRadius
        {
            get { return TransitionRadius + MaximumLocalDistance; }
        }

        public static bool TryCreate(
            double transitionRadius,
            double sphereOfInfluence,
            out BridgeOperationalBand band)
        {
            band = null;
            if (!FinitePositive(transitionRadius) || !FinitePositive(sphereOfInfluence) ||
                sphereOfInfluence <= transitionRadius)
                return false;

            double availableSpace = sphereOfInfluence - transitionRadius;
            if (!FinitePositive(availableSpace))
                return false;

            double maximumLocalDistance = Math.Min(
                MaximumDesiredLocalDistance,
                availableSpace * UsableSpaceFraction);
            double minimumLocalDistance = Math.Max(
                MinimumSafeLocalDistance,
                maximumLocalDistance / 3.0);

            if (!FinitePositive(minimumLocalDistance) || !FinitePositive(maximumLocalDistance) ||
                minimumLocalDistance >= maximumLocalDistance ||
                maximumLocalDistance >= availableSpace)
                return false;

            double innerRadius = transitionRadius + minimumLocalDistance;
            double outerRadius = transitionRadius + maximumLocalDistance;
            if (!FinitePositive(innerRadius) || !FinitePositive(outerRadius) ||
                innerRadius >= outerRadius || outerRadius >= sphereOfInfluence)
                return false;

            band = new BridgeOperationalBand(
                transitionRadius,
                sphereOfInfluence,
                availableSpace,
                minimumLocalDistance,
                maximumLocalDistance);
            return true;
        }

        public bool Contains(double localDistance)
        {
            return !Double.IsNaN(localDistance) &&
                   !Double.IsInfinity(localDistance) &&
                   localDistance >= MinimumLocalDistance &&
                   localDistance <= MaximumLocalDistance;
        }

        private static bool FinitePositive(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value) && value > 0;
        }
    }
}

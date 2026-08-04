using System;

namespace RemoteTechWormholeBridge.Core.Endpoints
{
    public static class BridgeOperationalBand
    {
        public const double MinimumLocalDistance = 100000.0;
        public const double MaximumLocalDistance = 300000.0;

        public static bool Contains(double localDistance)
        {
            return !Double.IsNaN(localDistance) &&
                   !Double.IsInfinity(localDistance) &&
                   localDistance >= MinimumLocalDistance &&
                   localDistance <= MaximumLocalDistance;
        }
    }
}

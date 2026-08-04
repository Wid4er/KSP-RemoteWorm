using System;
using System.Collections.Generic;

namespace RemoteTechWormholeBridge.Core.Routing
{
    public static class BridgeRouteVisibility
    {
        public static bool ContainsUndirectedEdge(
            Guid bridgeFirst,
            Guid bridgeSecond,
            Guid routeStart,
            IEnumerable<Guid> routeTargets)
        {
            if (bridgeFirst == Guid.Empty || bridgeSecond == Guid.Empty ||
                bridgeFirst == bridgeSecond || routeTargets == null)
                return false;

            Guid source = routeStart;
            foreach (Guid target in routeTargets)
            {
                if ((source == bridgeFirst && target == bridgeSecond) ||
                    (source == bridgeSecond && target == bridgeFirst))
                    return true;

                source = target;
            }

            return false;
        }
    }
}

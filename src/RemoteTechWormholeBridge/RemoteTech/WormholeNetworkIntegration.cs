using System;
using System.Collections.Generic;
using System.Linq;
using RemoteTech;
using RemoteTech.SimpleTypes;
using RemoteTechWormholeBridge.Core.Routing;

namespace RemoteTechWormholeBridge
{
    internal sealed class RuntimeBridgeLink
    {
        internal RuntimeEndpoint Source;
        internal RuntimeEndpoint Target;
        internal double EffectiveDistance;
        internal Guid SourceGuid;
        internal Guid TargetGuid;

        internal string Key
        {
            get { return PairKey(SourceGuid, TargetGuid); }
        }

        internal static string PairKey(Guid source, Guid target)
        {
            return source.ToString("D") + ">" + target.ToString("D");
        }
    }

    internal static class WormholeNetworkIntegration
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, RuntimeBridgeLink> ActiveLinks =
            new Dictionary<string, RuntimeBridgeLink>(StringComparer.Ordinal);
        private static readonly Dictionary<Guid, HashSet<Guid>> InjectedTargets =
            new Dictionary<Guid, HashSet<Guid>>();
        private static readonly HashSet<string> LoggedInjections =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedCosts =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<Guid, string> LastRouteStates =
            new Dictionary<Guid, string>();

        [ThreadStatic]
        private static int pathfindingDepth;

        internal static bool HasActiveLinks
        {
            get
            {
                lock (Sync)
                    return ActiveLinks.Count != 0;
            }
        }

        internal static List<RuntimeBridgeLink> SnapshotVisualLinks()
        {
            lock (Sync)
            {
                return ActiveLinks.Values
                    .Where(link => IsUsable(link) &&
                                   String.CompareOrdinal(
                                       link.Source.Descriptor.Key,
                                       link.Target.Descriptor.Key) < 0)
                    .ToList();
            }
        }

        internal static bool IsUsedByRouteFrom(RuntimeBridgeLink bridge, Vessel selected)
        {
            if (!IsUsable(bridge) || selected == null || RTCore.Instance == null ||
                RTCore.Instance.Satellites == null || RTCore.Instance.Network == null)
                return false;

            ISatellite start = RTCore.Instance.Satellites[selected];
            if (start == null)
                return false;

            foreach (NetworkRoute<ISatellite> route in RTCore.Instance.Network[start] ??
                     new List<NetworkRoute<ISatellite>>())
            {
                if (route == null || !route.Exists)
                    continue;

                IEnumerable<Guid> targets = (route.Links ?? new List<NetworkLink<ISatellite>>())
                    .Where(link => link != null && link.Target != null)
                    .Select(link => link.Target.Guid);
                if (BridgeRouteVisibility.ContainsUndirectedEdge(
                        bridge.SourceGuid,
                        bridge.TargetGuid,
                        start.Guid,
                        targets))
                    return true;
            }

            return false;
        }

        internal static void Replace(IEnumerable<RuntimeBridgeLink> links)
        {
            var replacement = new Dictionary<string, RuntimeBridgeLink>(StringComparer.Ordinal);
            foreach (RuntimeBridgeLink link in links ?? Enumerable.Empty<RuntimeBridgeLink>())
            {
                if (!IsUsable(link))
                    continue;

                RuntimeBridgeLink existing;
                if (!replacement.TryGetValue(link.Key, out existing) ||
                    link.EffectiveDistance < existing.EffectiveDistance)
                    replacement[link.Key] = link;
            }

            lock (Sync)
            {
                ActiveLinks.Clear();
                foreach (KeyValuePair<string, RuntimeBridgeLink> entry in replacement)
                    ActiveLinks.Add(entry.Key, entry.Value);

                LoggedInjections.RemoveWhere(key => !ActiveLinks.ContainsKey(key));
                LoggedCosts.RemoveWhere(key => !ActiveLinks.ContainsKey(key));
            }
        }

        internal static void BeforeGraphUpdate(NetworkManager manager, ISatellite source)
        {
            if (manager == null || source == null)
                return;

            HashSet<Guid> targets;
            lock (Sync)
            {
                if (!InjectedTargets.TryGetValue(source.Guid, out targets))
                    return;

                targets = new HashSet<Guid>(targets);
                InjectedTargets.Remove(source.Guid);
            }

            List<NetworkLink<ISatellite>> neighbors;
            if (!manager.Graph.TryGetValue(source.Guid, out neighbors) || neighbors == null)
                return;

            neighbors.RemoveAll(link =>
                link != null && link.Target != null && targets.Contains(link.Target.Guid));
        }

        internal static void AfterGraphUpdate(NetworkManager manager, ISatellite source)
        {
            if (manager == null || source == null)
                return;

            List<NetworkLink<ISatellite>> neighbors;
            if (!manager.Graph.TryGetValue(source.Guid, out neighbors) || neighbors == null)
                return;

            foreach (RuntimeBridgeLink bridge in Outgoing(source.Guid))
            {
                if (!IsLive(bridge, source))
                    continue;

                ISatellite target = RTCore.Instance == null
                    ? null
                    : RTCore.Instance.Satellites[bridge.Target.Vessel];
                if (target == null || target.Guid != bridge.TargetGuid)
                    continue;

                if (neighbors.Any(link => link != null && link.Target != null &&
                                          link.Target.Guid == target.Guid))
                    continue;

                neighbors.Add(new NetworkLink<ISatellite>(
                    target,
                    new List<IAntenna> { bridge.Source.Antenna },
                    LinkType.Dish));

                lock (Sync)
                {
                    HashSet<Guid> targets;
                    if (!InjectedTargets.TryGetValue(source.Guid, out targets))
                    {
                        targets = new HashSet<Guid>();
                        InjectedTargets.Add(source.Guid, targets);
                    }
                    targets.Add(target.Guid);

                    if (LoggedInjections.Add(bridge.Key))
                    {
                        Log.Info("graph-edge-injected source=" + source.Guid.ToString("D") +
                                 " target=" + target.Guid.ToString("D") +
                                 " channel=" + bridge.Source.Descriptor.Channel +
                                 " effectiveDistance=" +
                                 RemoteTechEndpointScanner.Format(bridge.EffectiveDistance) +
                                 " rendererEvent=false");
                    }
                }
            }
        }

        internal static bool TryGetEffectiveDistance(
            ISatellite source,
            NetworkLink<ISatellite> link,
            out double effectiveDistance)
        {
            effectiveDistance = 0;
            if (source == null || link == null || link.Target == null)
                return false;

            string key = RuntimeBridgeLink.PairKey(source.Guid, link.Target.Guid);
            lock (Sync)
            {
                RuntimeBridgeLink bridge;
                if (!ActiveLinks.TryGetValue(key, out bridge) || !IsUsable(bridge))
                    return false;

                effectiveDistance = bridge.EffectiveDistance;
                if (LoggedCosts.Add(key))
                {
                    Log.Info("path-cost-overridden source=" + source.Guid.ToString("D") +
                             " target=" + link.Target.Guid.ToString("D") +
                             " effectiveDistance=" +
                             RemoteTechEndpointScanner.Format(effectiveDistance));
                }
                return true;
            }
        }

        internal static void EnterPathfinding(out bool state)
        {
            state = HasActiveLinks;
            if (state)
                ++pathfindingDepth;
        }

        internal static void ExitPathfinding(bool state)
        {
            if (state && pathfindingDepth > 0)
                --pathfindingDepth;
        }

        internal static bool ShouldUseZeroHeuristic
        {
            get { return pathfindingDepth > 0; }
        }

        internal static void ObserveRoutes(NetworkManager manager, ISatellite start, bool state)
        {
            if (!state || manager == null || start == null)
                return;

            var bridgedGoals = new List<string>();
            var bridgedRoutes = new List<string>();
            foreach (NetworkRoute<ISatellite> route in manager[start] ??
                     new List<NetworkRoute<ISatellite>>())
            {
                if (route == null || !route.Exists || !UsesBridge(start.Guid, route.Links))
                    continue;

                string goal = route.Goal.Guid.ToString("D");
                bridgedGoals.Add(goal);
                bridgedRoutes.Add(goal + ":" +
                                  RemoteTechEndpointScanner.Format(route.Length) + ":" +
                                  RemoteTechEndpointScanner.Format(route.Delay));
            }

            bridgedGoals.Sort(StringComparer.Ordinal);
            bridgedRoutes.Sort(StringComparer.Ordinal);
            string routeState = String.Join(",", bridgedGoals.ToArray());
            string routeDetails = String.Join(",", bridgedRoutes.ToArray());
            string previous;
            lock (Sync)
            {
                LastRouteStates.TryGetValue(start.Guid, out previous);
                if (String.Equals(previous, routeState, StringComparison.Ordinal))
                    return;
                LastRouteStates[start.Guid] = routeState;
            }

            if (routeState.Length == 0 && String.IsNullOrEmpty(previous))
                return;

            Log.Info("path-bridge-routes start=" + start.Guid.ToString("D") +
                     " count=" + bridgedRoutes.Count +
                     " goalLengthDelay=" +
                     (routeDetails.Length == 0 ? "<none>" : routeDetails));
        }

        private static bool UsesBridge(Guid start, IEnumerable<NetworkLink<ISatellite>> links)
        {
            Guid source = start;
            foreach (NetworkLink<ISatellite> link in links ?? Enumerable.Empty<NetworkLink<ISatellite>>())
            {
                if (link == null || link.Target == null)
                    continue;

                string key = RuntimeBridgeLink.PairKey(source, link.Target.Guid);
                lock (Sync)
                {
                    if (ActiveLinks.ContainsKey(key))
                        return true;
                }
                source = link.Target.Guid;
            }
            return false;
        }

        private static List<RuntimeBridgeLink> Outgoing(Guid source)
        {
            lock (Sync)
                return ActiveLinks.Values.Where(link => link.SourceGuid == source).ToList();
        }

        private static bool IsLive(RuntimeBridgeLink bridge, ISatellite source)
        {
            return IsUsable(bridge) && source.Guid == bridge.SourceGuid &&
                   bridge.Source.Antenna.Activated && bridge.Source.Antenna.Powered &&
                   bridge.Target.Antenna.Activated && bridge.Target.Antenna.Powered &&
                   source.Powered;
        }

        private static bool IsUsable(RuntimeBridgeLink bridge)
        {
            return bridge != null && bridge.Source != null && bridge.Target != null &&
                   bridge.SourceGuid != Guid.Empty && bridge.TargetGuid != Guid.Empty &&
                   bridge.Source.Satellite != null && bridge.Target.Satellite != null &&
                   bridge.Source.Antenna != null && bridge.Target.Antenna != null &&
                   !Double.IsNaN(bridge.EffectiveDistance) &&
                   !Double.IsInfinity(bridge.EffectiveDistance) &&
                   bridge.EffectiveDistance > 0;
        }
    }

    internal static class NetworkGraphPatch
    {
        internal static void Prefix(NetworkManager __instance, ISatellite __0)
        {
            try
            {
                WormholeNetworkIntegration.BeforeGraphUpdate(__instance, __0);
            }
            catch (Exception exception)
            {
                Log.Error("graph prefix failed open: " + exception);
            }
        }

        internal static void Postfix(NetworkManager __instance, ISatellite __0)
        {
            try
            {
                WormholeNetworkIntegration.AfterGraphUpdate(__instance, __0);
            }
            catch (Exception exception)
            {
                Log.Error("graph postfix failed open: " + exception);
            }
        }
    }

    internal static class NetworkPathPatch
    {
        internal static void Prefix(out bool __state)
        {
            __state = false;
            try
            {
                WormholeNetworkIntegration.EnterPathfinding(out __state);
            }
            catch (Exception exception)
            {
                __state = false;
                Log.Error("path prefix failed open: " + exception);
            }
        }

        internal static void Postfix(NetworkManager __instance, ISatellite __0, bool __state)
        {
            try
            {
                WormholeNetworkIntegration.ObserveRoutes(__instance, __0, __state);
            }
            catch (Exception exception)
            {
                Log.Error("path diagnostics failed open: " + exception);
            }
        }

        internal static Exception Finalizer(bool __state, Exception __exception)
        {
            try
            {
                WormholeNetworkIntegration.ExitPathfinding(__state);
            }
            catch (Exception exception)
            {
                Log.Error("path finalizer cleanup failed: " + exception);
            }
            return __exception;
        }
    }

    internal static class NetworkCostPatch
    {
        internal static bool Prefix(
            ISatellite __0,
            NetworkLink<ISatellite> __1,
            ref double __result)
        {
            try
            {
                return !WormholeNetworkIntegration.TryGetEffectiveDistance(__0, __1, out __result);
            }
            catch (Exception exception)
            {
                Log.Error("path cost patch failed open: " + exception);
                return true;
            }
        }
    }

    internal static class NetworkHeuristicPatch
    {
        internal static bool Prefix(ref double __result)
        {
            if (!WormholeNetworkIntegration.ShouldUseZeroHeuristic)
                return true;

            __result = 0;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Kopernicus.Components;
using KSPAchievements;
using KSP.Localization;
using RemoteTechWormholeBridge.Core.Contracts;
using RemoteTechWormholeBridge.Core.Wormholes;

namespace RemoteTechWormholeBridge.API
{
    public sealed class RTWBWormholePairSnapshot
    {
        internal RTWBWormholePairSnapshot()
        {
        }

        public string PairId { get; internal set; }
        public bool SystemsResolved { get; internal set; }
        public string MouthAId { get; internal set; }
        public string MouthAName { get; internal set; }
        public string ParentAId { get; internal set; }
        public string SystemAId { get; internal set; }
        public string SystemAName { get; internal set; }
        public int DepthA { get; internal set; }
        public string MouthBId { get; internal set; }
        public string MouthBName { get; internal set; }
        public string ParentBId { get; internal set; }
        public string SystemBId { get; internal set; }
        public string SystemBName { get; internal set; }
        public int DepthB { get; internal set; }
        public int Tier { get; internal set; }
        public bool ParentAReached { get; internal set; }
        public bool ParentBReached { get; internal set; }
    }

    public sealed class RTWBEndpointVesselSnapshot
    {
        internal RTWBEndpointVesselSnapshot(string pairId, string mouthId, Guid vesselId, string vesselName)
        {
            PairId = pairId;
            MouthId = mouthId;
            VesselId = vesselId;
            VesselName = vesselName;
        }

        public string PairId { get; private set; }
        public string MouthId { get; private set; }
        public Guid VesselId { get; private set; }
        public string VesselName { get; private set; }
    }

    public sealed class RTWBActiveLinkSnapshot
    {
        internal RTWBActiveLinkSnapshot(
            string pairId,
            string mouthAId,
            Guid vesselAId,
            string mouthBId,
            Guid vesselBId)
        {
            PairId = pairId;
            MouthAId = mouthAId;
            VesselAId = vesselAId;
            MouthBId = mouthBId;
            VesselBId = vesselBId;
        }

        public string PairId { get; private set; }
        public string MouthAId { get; private set; }
        public Guid VesselAId { get; private set; }
        public string MouthBId { get; private set; }
        public Guid VesselBId { get; private set; }
    }

    public static class RTWBAPI
    {
        public static bool RuntimeStateAvailable
        {
            get { return WormholeNetworkIntegration.RuntimeSnapshotAvailable; }
        }

        public static IReadOnlyList<RTWBWormholePairSnapshot> GetWormholePairs()
        {
            KexWormholeCatalog.EnsureSnapshot("api-query");
            List<MouthPair> resolved = ResolvePairs();
            CelestialBody homeBody = FlightGlobals.Bodies == null
                ? null
                : FlightGlobals.Bodies.FirstOrDefault(body => body != null && body.isHomeWorld);
            CelestialBody homeStar = HostSystemResolver.Resolve(homeBody);
            string homeSystem = homeStar == null ? null : homeStar.name;

            InterstellarTopology topology = String.IsNullOrEmpty(homeSystem)
                ? null
                : InterstellarTopology.Build(homeSystem, resolved
                    .Where(pair => pair.SystemsResolved)
                    .Select(pair => new WormholeTopologyEdge(
                        pair.PairId,
                        pair.First.System.name,
                        pair.Second.System.name)));

            return resolved.Select(pair => CreateSnapshot(pair, topology))
                .OrderBy(pair => pair.PairId, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        public static RTWBWormholePairSnapshot GetPair(string pairId)
        {
            return GetWormholePairs().FirstOrDefault(pair =>
                String.Equals(pair.PairId, pairId, StringComparison.Ordinal));
        }

        public static bool IsPairLinked(string pairId)
        {
            return GetActiveLinks(pairId).Count != 0;
        }

        public static IReadOnlyList<RTWBActiveLinkSnapshot> GetActiveLinks(string pairId)
        {
            if (!RuntimeStateAvailable || String.IsNullOrEmpty(pairId))
                return new List<RTWBActiveLinkSnapshot>().AsReadOnly();

            var snapshots = new List<RTWBActiveLinkSnapshot>();
            foreach (RuntimeBridgeLink link in WormholeNetworkIntegration.SnapshotVisualLinks())
            {
                string sourceMouth = link.Source.Wormhole.Body.name;
                string targetMouth = link.Target.Wormhole.Body.name;
                string linkPairId = WormholePairId.Create(sourceMouth, targetMouth);
                if (!String.Equals(linkPairId, pairId, StringComparison.Ordinal))
                    continue;

                snapshots.Add(new RTWBActiveLinkSnapshot(
                    linkPairId,
                    sourceMouth,
                    link.SourceGuid,
                    targetMouth,
                    link.TargetGuid));
            }
            return snapshots.AsReadOnly();
        }

        public static IReadOnlyList<RTWBEndpointVesselSnapshot> GetEndpointVessels(string pairId)
        {
            RTWBWormholePairSnapshot pair = GetPair(pairId);
            if (!RuntimeStateAvailable || pair == null)
                return new List<RTWBEndpointVesselSnapshot>().AsReadOnly();

            return RemoteTechEndpointScanner.SnapshotAccepted()
                .Where(endpoint => endpoint != null && endpoint.Vessel != null &&
                    (String.Equals(endpoint.Wormhole.Body.name, pair.MouthAId, StringComparison.Ordinal) ||
                     String.Equals(endpoint.Wormhole.Body.name, pair.MouthBId, StringComparison.Ordinal)))
                .GroupBy(endpoint => endpoint.Wormhole.Body.name + "|" + endpoint.Vessel.id.ToString("D"))
                .Select(group => group.First())
                .Select(endpoint => new RTWBEndpointVesselSnapshot(
                    pairId,
                    endpoint.Wormhole.Body.name,
                    endpoint.Vessel.id,
                    endpoint.Vessel.vesselName))
                .OrderBy(endpoint => endpoint.MouthId, StringComparer.Ordinal)
                .ThenBy(endpoint => endpoint.VesselId)
                .ToList()
                .AsReadOnly();
        }

        private static List<MouthPair> ResolvePairs()
        {
            var result = new List<MouthPair>();
            foreach (WormholePairDescriptor descriptor in KexWormholeCatalog.SnapshotPairs())
            {
                KexBodyInfo firstInfo;
                KexBodyInfo secondInfo;
                if (!KexWormholeCatalog.TryGetInfo(descriptor.BodyA.BodyId, out firstInfo) ||
                    !KexWormholeCatalog.TryGetInfo(descriptor.BodyB.BodyId, out secondInfo))
                    continue;

                var first = new MouthInfo(firstInfo.Body);
                var second = new MouthInfo(secondInfo.Body);
                result.Add(new MouthPair
                {
                    PairId = WormholePairId.Create(first.Body.name, second.Body.name),
                    First = first,
                    Second = second
                });
            }
            return result;
        }

        private static RTWBWormholePairSnapshot CreateSnapshot(
            MouthPair pair,
            InterstellarTopology topology)
        {
            int firstDepth = Depth(topology, pair.First.System);
            int secondDepth = Depth(topology, pair.Second.System);
            MouthInfo a = pair.First;
            MouthInfo b = pair.Second;
            int depthA = firstDepth;
            int depthB = secondDepth;
            if (ComesAfter(a, depthA, b, depthB))
            {
                a = pair.Second;
                b = pair.First;
                depthA = secondDepth;
                depthB = firstDepth;
            }

            int tier = 0;
            bool hasTier = topology != null && topology.Tiers.TryGetValue(pair.PairId, out tier);
            return new RTWBWormholePairSnapshot
            {
                PairId = pair.PairId,
                SystemsResolved = pair.SystemsResolved,
                MouthAId = a.Body.name,
                MouthAName = DisplayName(a.Body),
                ParentAId = a.Parent == null ? null : a.Parent.name,
                SystemAId = a.System == null ? null : a.System.name,
                SystemAName = DisplayName(a.System),
                DepthA = depthA,
                MouthBId = b.Body.name,
                MouthBName = DisplayName(b.Body),
                ParentBId = b.Parent == null ? null : b.Parent.name,
                SystemBId = b.System == null ? null : b.System.name,
                SystemBName = DisplayName(b.System),
                DepthB = depthB,
                Tier = hasTier ? tier : 0,
                ParentAReached = IsReached(a.Parent),
                ParentBReached = IsReached(b.Parent)
            };
        }

        private static int Depth(InterstellarTopology topology, CelestialBody system)
        {
            int depth;
            return topology != null && system != null && topology.Depths.TryGetValue(system.name, out depth)
                ? depth
                : -1;
        }

        private static bool ComesAfter(MouthInfo first, int firstDepth, MouthInfo second, int secondDepth)
        {
            if (firstDepth != secondDepth)
                return firstDepth < 0 || secondDepth >= 0 && firstDepth > secondDepth;

            int systems = String.CompareOrdinal(
                first.System == null ? "" : first.System.name,
                second.System == null ? "" : second.System.name);
            return systems > 0 || systems == 0 &&
                   String.CompareOrdinal(first.Body.name, second.Body.name) > 0;
        }

        private static bool IsReached(CelestialBody body)
        {
            if (body == null || ProgressTracking.Instance == null ||
                ProgressTracking.Instance.celestialBodyNodes == null)
                return false;

            CelestialBodySubtree node = ProgressTracking.Instance.celestialBodyNodes
                .FirstOrDefault(candidate => candidate != null && candidate.Body == body);
            return node != null && node.IsReached;
        }

        private static string DisplayName(CelestialBody body)
        {
            if (body == null)
                return "";
            string display = Localizer.Format(body.displayName ?? body.bodyName ?? body.name);
            int grammar = display.IndexOf('^');
            return grammar < 0 ? display : display.Substring(0, grammar);
        }

        private sealed class MouthInfo
        {
            internal MouthInfo(CelestialBody body)
            {
                Body = body;
                Parent = body == null ? null : body.referenceBody;
                System = HostSystemResolver.Resolve(Parent);
            }

            internal CelestialBody Body;
            internal CelestialBody Parent;
            internal CelestialBody System;
        }

        private sealed class MouthPair
        {
            internal string PairId;
            internal MouthInfo First;
            internal MouthInfo Second;
            internal bool SystemsResolved
            {
                get { return First.System != null && Second.System != null; }
            }
        }
    }

    internal static class HostSystemResolver
    {
        private static readonly HashSet<string> LoggedFailures =
            new HashSet<string>(StringComparer.Ordinal);

        internal static CelestialBody Resolve(CelestialBody body)
        {
            if (body == null)
                return null;

            try
            {
                CelestialBody star = KopernicusStar.GetLocalStar(body);
                if (star != null)
                    return star;
            }
            catch (Exception exception)
            {
                if (LoggedFailures.Add(body.name))
                    Log.Warning("host-system unresolved body=" + body.name +
                                " kopernicus=" + exception.GetType().Name);
            }

            return null;
        }
    }
}

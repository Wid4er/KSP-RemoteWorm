using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using RemoteTech;
using RemoteTech.Modules;
using RemoteTechWormholeBridge.Core.Endpoints;
using RemoteTechWormholeBridge.Core.Geometry;

namespace RemoteTechWormholeBridge
{
    internal sealed class RuntimeEndpoint
    {
        internal EndpointDescriptor Descriptor;
        internal Vessel Vessel;
        internal ISatellite Satellite;
        internal IAntenna Antenna;
        internal KexBodyInfo Wormhole;
        internal Vector3Value Radial;
        internal double LocalDistance;
    }

    internal static class RemoteTechEndpointScanner
    {
        private static readonly EndpointRegistry Registry = new EndpointRegistry();
        private static readonly List<RuntimeEndpoint> AcceptedEndpoints = new List<RuntimeEndpoint>();
        private static string lastState;
        private static bool protoBindingWarningLogged;

        internal static IReadOnlyList<RuntimeEndpoint> Accepted
        {
            get { return AcceptedEndpoints; }
        }

        internal static List<RuntimeEndpoint> SnapshotAccepted()
        {
            return AcceptedEndpoints.ToList();
        }

        internal static void Refresh(string reason, bool logUnchanged)
        {
            var candidates = new List<EndpointDescriptor>();
            var runtimeByKey = new Dictionary<string, RuntimeEndpoint>(StringComparer.Ordinal);

            if (FlightGlobals.Vessels != null)
            {
                foreach (Vessel vessel in FlightGlobals.Vessels)
                    ScanVessel(vessel, candidates, runtimeByKey);
            }

            Registry.Refresh(candidates);
            AcceptedEndpoints.Clear();
            foreach (EndpointDescriptor descriptor in Registry.Endpoints.Values.OrderBy(value => value.Key))
            {
                RuntimeEndpoint runtime;
                if (runtimeByKey.TryGetValue(descriptor.Key, out runtime))
                    AcceptedEndpoints.Add(runtime);
            }

            string currentState = BuildState(candidates);
            bool changed = !String.Equals(currentState, lastState, StringComparison.Ordinal);
            lastState = currentState;
            if (!logUnchanged && !changed)
                return;

            Log.Info("endpoint-scan reason=" + reason + " candidates=" + candidates.Count +
                     " accepted=" + Registry.Endpoints.Count + " rejected=" + Registry.Rejected.Count);

            foreach (EndpointDescriptor candidate in candidates.OrderBy(value => value.Key))
            {
                RuntimeEndpoint runtime;
                if (!runtimeByKey.TryGetValue(candidate.Key, out runtime))
                    continue;

                IAntenna antenna = runtime.Antenna;
                Log.Info("endpoint-observed key=" + candidate.Key +
                         " body=" + candidate.WormholeBodyId +
                         " channel=" + candidate.Channel +
                         " active=" + antenna.Activated +
                         " powered=" + antenna.Powered +
                         " canTarget=" + antenna.CanTarget +
                         " target=" + antenna.Target.ToString("D") +
                         " localTarget=" + runtime.Vessel.mainBody.Guid().ToString("D") +
                         " dishRange=" + Format(antenna.Dish) +
                         " dishAngleDeg=" + Format(HalfAngleDegrees(antenna.CosAngle) * 2) +
                         " cosHalfAngle=" + Format(antenna.CosAngle) +
                         " localDistance=" + Format(runtime.LocalDistance) +
                         " operationalBand=" + Format(BridgeOperationalBand.MinimumLocalDistance) +
                         "-" + Format(BridgeOperationalBand.MaximumLocalDistance) +
                         " transitionRadius=" + Format(runtime.Wormhole.TransitionRadius) +
                         " altitude=" + Format(runtime.Vessel.altitude));
            }

            foreach (KeyValuePair<string, EndpointFailureReason> rejected in Registry.Rejected)
                Log.Info("endpoint-rejected key=" + rejected.Key + " reason=" + rejected.Value);
        }

        private static void ScanVessel(
            Vessel vessel,
            ICollection<EndpointDescriptor> candidates,
            IDictionary<string, RuntimeEndpoint> runtimeByKey)
        {
            if (vessel == null || vessel.orbit == null || RTCore.Instance == null)
                return;

            KexBodyInfo wormhole;
            if (!KexWormholeCatalog.TryGetInfo(vessel.mainBody, out wormhole))
                return;

            Vector3d relativePosition = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
            double relativeRadius = relativePosition.magnitude;
            bool hasRadial = IsFinite(relativeRadius) && relativeRadius > 0;
            double localDistance = hasRadial
                ? Math.Max(0, relativeRadius - wormhole.TransitionRadius)
                : Double.PositiveInfinity;

            IEnumerable<IAntenna> antennas = RTCore.Instance.Antennas[vessel];
            ISatellite satellite = RTCore.Instance.Satellites[vessel];
            if (antennas == null || satellite == null)
                return;

            foreach (IAntenna antenna in antennas)
            {
                if (antenna == null)
                    continue;

                bool bridgeEnabled;
                int channel;
                uint partFlightId;
                if (!TryGetBridgeSettings(antenna, out bridgeEnabled, out channel, out partFlightId))
                    continue;

                var candidate = new EndpointDescriptor(
                    vessel.id.ToString("D"), partFlightId.ToString(CultureInfo.InvariantCulture),
                    vessel.mainBody.name, channel)
                {
                    IsRemoteTechVessel = true,
                    IsDirectional = antenna.CanTarget && antenna.Dish > 0,
                    Activated = antenna.Activated,
                    Powered = antenna.Powered,
                    TargetsLocalWormhole = antenna.Target == vessel.mainBody.Guid(),
                    BridgeCapabilityEnabled = bridgeEnabled,
                    IsInOperationalRegion = hasRadial,
                    LocalDistance = localDistance,
                    HasLocalRange = hasRadial && antenna.Dish >= localDistance
                };
                candidates.Add(candidate);

                runtimeByKey[candidate.Key] = new RuntimeEndpoint
                {
                    Descriptor = candidate,
                    Vessel = vessel,
                    Satellite = satellite,
                    Antenna = antenna,
                    Wormhole = wormhole,
                    Radial = new Vector3Value(relativePosition.x, relativePosition.y, relativePosition.z),
                    LocalDistance = localDistance
                };
            }
        }

        private static bool TryGetBridgeSettings(
            IAntenna antenna,
            out bool bridgeEnabled,
            out int channel,
            out uint partFlightId)
        {
            bridgeEnabled = false;
            channel = 0;
            partFlightId = 0;

            ModuleRTAntenna loadedAntenna = antenna as ModuleRTAntenna;
            if (loadedAntenna != null)
            {
                ModuleRTWormholeBridge bridge = loadedAntenna.part == null
                    ? null
                    : loadedAntenna.part.FindModuleImplementing<ModuleRTWormholeBridge>();
                if (bridge == null)
                    return false;

                partFlightId = loadedAntenna.part.flightID;
                bridgeEnabled = bridge.bridgeEnabled;
                channel = bridge.channel;
                return true;
            }

            ProtoPartSnapshot protoPart = TryGetProtoPart(antenna);
            if (protoPart == null || protoPart.partPrefab == null)
                return false;

            ModuleRTWormholeBridge prefabBridge =
                protoPart.partPrefab.FindModuleImplementing<ModuleRTWormholeBridge>();
            if (prefabBridge == null)
                return false;

            partFlightId = protoPart.flightID;
            bridgeEnabled = prefabBridge.bridgeEnabled;
            channel = prefabBridge.channel;

            ProtoPartModuleSnapshot snapshot = protoPart.FindModule("ModuleRTWormholeBridge");
            if (snapshot == null || snapshot.moduleValues == null)
                return true;

            bool savedEnabled;
            if (Boolean.TryParse(snapshot.moduleValues.GetValue("bridgeEnabled"), out savedEnabled))
                bridgeEnabled = savedEnabled;

            int savedChannel;
            if (Int32.TryParse(snapshot.moduleValues.GetValue("channel"), out savedChannel))
                channel = savedChannel;

            return true;
        }

        private static ProtoPartSnapshot TryGetProtoPart(IAntenna antenna)
        {
            Type antennaType = antenna.GetType();
            if (!String.Equals(antennaType.FullName, "RemoteTech.Modules.ProtoAntenna", StringComparison.Ordinal))
                return null;

            FieldInfo field = antennaType.GetField("mProtoPart", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && typeof(ProtoPartSnapshot).IsAssignableFrom(field.FieldType))
                return field.GetValue(antenna) as ProtoPartSnapshot;

            if (!protoBindingWarningLogged)
            {
                protoBindingWarningLogged = true;
                Log.Warning("RemoteTech ProtoAntenna.mProtoPart unavailable; unloaded endpoints disabled.");
            }

            return null;
        }

        private static string BuildState(IEnumerable<EndpointDescriptor> candidates)
        {
            var builder = new StringBuilder();
            foreach (EndpointDescriptor candidate in candidates.OrderBy(value => value.Key))
            {
                builder.Append(candidate.Key).Append('|')
                    .Append((int)EndpointRegistry.Validate(candidate)).Append('|')
                    .Append(candidate.Channel).Append(';');
            }

            return builder.ToString();
        }

        private static bool IsFinite(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        internal static string Format(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static double HalfAngleDegrees(double cosHalfAngle)
        {
            double clamped = Math.Max(-1, Math.Min(1, cosHalfAngle));
            return Math.Acos(clamped) * 180.0 / Math.PI;
        }
    }
}

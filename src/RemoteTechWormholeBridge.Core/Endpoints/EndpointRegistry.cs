using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTechWormholeBridge.Core.Endpoints
{
    public sealed class EndpointRegistry
    {
        private readonly Dictionary<string, EndpointDescriptor> _endpoints =
            new Dictionary<string, EndpointDescriptor>(StringComparer.Ordinal);
        private readonly Dictionary<string, EndpointFailureReason> _rejected =
            new Dictionary<string, EndpointFailureReason>(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, EndpointDescriptor> Endpoints { get { return _endpoints; } }
        public IReadOnlyDictionary<string, EndpointFailureReason> Rejected { get { return _rejected; } }

        public void Refresh(IEnumerable<EndpointDescriptor> candidates)
        {
            _endpoints.Clear();
            _rejected.Clear();

            foreach (EndpointDescriptor candidate in candidates ?? Enumerable.Empty<EndpointDescriptor>())
            {
                EndpointFailureReason reason = Validate(candidate);
                string key = candidate == null ? "<null>" : candidate.Key;
                if (reason != EndpointFailureReason.None)
                {
                    _rejected[key] = reason;
                    continue;
                }

                _endpoints[key] = candidate;
            }
        }

        public IEnumerable<EndpointDescriptor> ForBodyAndChannel(string bodyId, int channel)
        {
            return _endpoints.Values.Where(endpoint =>
                String.Equals(endpoint.WormholeBodyId, bodyId, StringComparison.Ordinal) &&
                endpoint.Channel == channel);
        }

        public static EndpointFailureReason Validate(EndpointDescriptor candidate)
        {
            if (candidate == null || String.IsNullOrWhiteSpace(candidate.VesselId) ||
                String.IsNullOrWhiteSpace(candidate.AntennaId) || String.IsNullOrWhiteSpace(candidate.WormholeBodyId))
                return EndpointFailureReason.InvalidIdentity;
            if (!candidate.IsRemoteTechVessel)
                return EndpointFailureReason.NotRemoteTechVessel;
            if (!candidate.Activated)
                return EndpointFailureReason.Inactive;
            if (!candidate.Powered)
                return EndpointFailureReason.Unpowered;
            if (!candidate.IsDirectional)
                return EndpointFailureReason.NotDirectional;
            if (!candidate.TargetsLocalWormhole)
                return EndpointFailureReason.WrongTarget;
            if (!candidate.BridgeCapabilityEnabled)
                return EndpointFailureReason.BridgeCapabilityMissing;
            BridgeOperationalBand band = candidate.OperationalBand;
            if (!candidate.IsInOperationalRegion || band == null)
                return EndpointFailureReason.UnsafeRegion;
            if (candidate.LocalDistance < band.MinimumLocalDistance)
                return EndpointFailureReason.TooCloseToWormhole;
            if (candidate.LocalDistance > band.MaximumLocalDistance)
                return EndpointFailureReason.TooFarFromWormhole;
            if (!band.Contains(candidate.LocalDistance))
                return EndpointFailureReason.UnsafeRegion;
            if (!candidate.HasLocalRange)
                return EndpointFailureReason.InsufficientLocalRange;
            if (candidate.Channel < 0)
                return EndpointFailureReason.InvalidChannel;
            return EndpointFailureReason.None;
        }
    }
}

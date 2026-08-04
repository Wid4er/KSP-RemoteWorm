using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTechWormholeBridge.Core.Wormholes
{
    public sealed class WormholeRegistry
    {
        private readonly Dictionary<string, WormholeBodyDescriptor> _bodies =
            new Dictionary<string, WormholeBodyDescriptor>(StringComparer.Ordinal);
        private readonly List<WormholePairDescriptor> _pairs = new List<WormholePairDescriptor>();
        private readonly List<RegistryIssue> _issues = new List<RegistryIssue>();

        public IReadOnlyDictionary<string, WormholeBodyDescriptor> Bodies { get { return _bodies; } }
        public IReadOnlyList<WormholePairDescriptor> Pairs { get { return _pairs; } }
        public IReadOnlyList<RegistryIssue> Issues { get { return _issues; } }

        public void Refresh(IEnumerable<WormholeBodyDescriptor> discoveredBodies)
        {
            _bodies.Clear();
            _pairs.Clear();
            _issues.Clear();

            foreach (WormholeBodyDescriptor body in discoveredBodies ?? Enumerable.Empty<WormholeBodyDescriptor>())
            {
                if (!IsValid(body))
                    continue;

                if (_bodies.ContainsKey(body.BodyId))
                {
                    _issues.Add(new RegistryIssue(body.BodyId, "duplicate wormhole body"));
                    continue;
                }

                _bodies.Add(body.BodyId, body);
            }

            var seenPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (WormholeBodyDescriptor body in _bodies.Values)
            {
                WormholeBodyDescriptor partner;
                if (!_bodies.TryGetValue(body.PartnerBodyId, out partner))
                {
                    _issues.Add(new RegistryIssue(body.BodyId, "missing partner " + body.PartnerBodyId));
                    continue;
                }

                if (!String.Equals(partner.PartnerBodyId, body.BodyId, StringComparison.Ordinal))
                {
                    _issues.Add(new RegistryIssue(body.BodyId, "partner relationship is not reciprocal"));
                    continue;
                }

                string key = PairKey(body.BodyId, partner.BodyId);
                if (seenPairs.Add(key))
                    _pairs.Add(new WormholePairDescriptor(body, partner));
            }
        }

        public bool TryGetPartner(string bodyId, out WormholeBodyDescriptor partner)
        {
            partner = null;
            WormholeBodyDescriptor body;
            return bodyId != null && _bodies.TryGetValue(bodyId, out body) &&
                   _bodies.TryGetValue(body.PartnerBodyId, out partner) &&
                   String.Equals(partner.PartnerBodyId, body.BodyId, StringComparison.Ordinal);
        }

        private bool IsValid(WormholeBodyDescriptor body)
        {
            if (body == null)
            {
                _issues.Add(new RegistryIssue("wormhole", "null descriptor"));
                return false;
            }

            if (String.IsNullOrWhiteSpace(body.BodyId) || String.IsNullOrWhiteSpace(body.PartnerBodyId))
            {
                _issues.Add(new RegistryIssue(body.BodyId, "body and partner identifiers are required"));
                return false;
            }

            if (!FiniteNonNegative(body.BodyRadius) || !FiniteNonNegative(body.InfluenceAltitude) ||
                !FiniteNonNegative(body.JumpMinAltitude) || !FiniteNonNegative(body.JumpMaxAltitude))
            {
                _issues.Add(new RegistryIssue(body.BodyId, "radii and altitudes must be finite and non-negative"));
                return false;
            }

            if (body.JumpMinAltitude > body.JumpMaxAltitude || body.JumpMaxAltitude > body.InfluenceAltitude)
            {
                _issues.Add(new RegistryIssue(body.BodyId, "expected jumpMin <= jumpMax <= influenceAltitude"));
                return false;
            }

            return true;
        }

        private static bool FiniteNonNegative(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value) && value >= 0;
        }

        private static string PairKey(string a, string b)
        {
            return String.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }
    }
}

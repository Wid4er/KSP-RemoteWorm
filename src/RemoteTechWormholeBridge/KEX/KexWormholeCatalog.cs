using System;
using System.Collections.Generic;
using KopernicusExpansion.Wormholes;
using RemoteTechWormholeBridge.Core.Wormholes;

namespace RemoteTechWormholeBridge
{
    internal sealed class KexBodyInfo
    {
        internal CelestialBody Body;
        internal WormholeComponent Component;

        internal double TransitionRadius
        {
            get { return Body.Radius + Component.influenceAltitude; }
        }
    }

    internal static class KexWormholeCatalog
    {
        private static readonly WormholeRegistry Registry = new WormholeRegistry();
        private static readonly Dictionary<string, KexBodyInfo> Bodies =
            new Dictionary<string, KexBodyInfo>(StringComparer.Ordinal);

        internal static void Refresh(string reason)
        {
            Bodies.Clear();
            var descriptors = new List<WormholeBodyDescriptor>();
            List<CelestialBody> localBodies = PSystemManager.Instance == null
                ? null
                : PSystemManager.Instance.localBodies;

            if (localBodies == null)
            {
                Registry.Refresh(descriptors);
                Log.Warning("wormhole-scan reason=" + reason + " bodies=unavailable");
                return;
            }

            foreach (CelestialBody body in localBodies)
            {
                if (body == null)
                    continue;

                WormholeComponent component = body.GetComponent<WormholeComponent>();
                if (component == null || String.IsNullOrWhiteSpace(component.partnerBody))
                    continue;

                CelestialBody partner = localBodies.Find(candidate =>
                    candidate != null && candidate.transform != null &&
                    String.Equals(candidate.transform.name, component.partnerBody, StringComparison.Ordinal));
                string partnerId = partner == null ? component.partnerBody : partner.name;

                Bodies[body.name] = new KexBodyInfo { Body = body, Component = component };
                descriptors.Add(new WormholeBodyDescriptor(
                    body.name,
                    partnerId,
                    body.Radius,
                    component.influenceAltitude,
                    component.jumpMinAltitude,
                    component.jumpMaxAltitude));
            }

            Registry.Refresh(descriptors);
            Log.Info("wormhole-scan reason=" + reason + " bodies=" + Registry.Bodies.Count +
                     " pairs=" + Registry.Pairs.Count + " issues=" + Registry.Issues.Count);

            foreach (WormholePairDescriptor pair in Registry.Pairs)
            {
                Log.Info("wormhole-pair a=" + FormatBody(pair.BodyA) + " b=" + FormatBody(pair.BodyB) +
                         " transform=orbital-radial-identity-verified");
            }

            foreach (Core.RegistryIssue issue in Registry.Issues)
                Log.Warning("wormhole-invalid subject=" + issue.Subject + " reason=" + issue.Message);
        }

        internal static bool TryGetInfo(CelestialBody body, out KexBodyInfo info)
        {
            info = null;
            return body != null && Bodies.TryGetValue(body.name, out info);
        }

        internal static bool ArePartners(CelestialBody first, CelestialBody second)
        {
            if (first == null || second == null)
                return false;

            WormholeBodyDescriptor partner;
            return Registry.TryGetPartner(first.name, out partner) &&
                   String.Equals(partner.BodyId, second.name, StringComparison.Ordinal);
        }

        internal static bool TryGetPartner(KexBodyInfo source, out KexBodyInfo partner)
        {
            partner = null;
            if (source == null || source.Body == null)
                return false;

            WormholeBodyDescriptor partnerDescriptor;
            return Registry.TryGetPartner(source.Body.name, out partnerDescriptor) &&
                   Bodies.TryGetValue(partnerDescriptor.BodyId, out partner);
        }

        private static string FormatBody(WormholeBodyDescriptor body)
        {
            return body.BodyId + "->" + body.PartnerBodyId +
                   ",radius=" + body.BodyRadius.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                   ",influence=" + body.InfluenceAltitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                   ",jumpMin=" + body.JumpMinAltitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture) +
                   ",jumpMax=" + body.JumpMaxAltitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

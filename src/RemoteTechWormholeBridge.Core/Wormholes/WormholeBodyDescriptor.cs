using System;
using RemoteTechWormholeBridge.Core.Endpoints;

namespace RemoteTechWormholeBridge.Core.Wormholes
{
    public sealed class WormholeBodyDescriptor
    {
        public WormholeBodyDescriptor(
            string bodyId,
            string partnerBodyId,
            double bodyRadius,
            double sphereOfInfluence,
            double influenceAltitude,
            double jumpMinAltitude,
            double jumpMaxAltitude)
        {
            BodyId = bodyId ?? String.Empty;
            PartnerBodyId = partnerBodyId ?? String.Empty;
            BodyRadius = bodyRadius;
            SphereOfInfluence = sphereOfInfluence;
            InfluenceAltitude = influenceAltitude;
            JumpMinAltitude = jumpMinAltitude;
            JumpMaxAltitude = jumpMaxAltitude;

            BridgeOperationalBand band;
            OperationalBand = BridgeOperationalBand.TryCreate(
                SafetySurfaceRadius,
                sphereOfInfluence,
                out band)
                ? band
                : null;
        }

        public string BodyId { get; private set; }
        public string PartnerBodyId { get; private set; }
        public double BodyRadius { get; private set; }
        public double SphereOfInfluence { get; private set; }
        public double InfluenceAltitude { get; private set; }
        public double JumpMinAltitude { get; private set; }
        public double JumpMaxAltitude { get; private set; }
        public BridgeOperationalBand OperationalBand { get; private set; }

        public double SafetySurfaceRadius
        {
            get { return BodyRadius + InfluenceAltitude; }
        }
    }
}

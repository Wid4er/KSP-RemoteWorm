using System;

namespace RemoteTechWormholeBridge.Core.Endpoints
{
    public sealed class EndpointDescriptor
    {
        public EndpointDescriptor(string vesselId, string antennaId, string wormholeBodyId, int channel)
        {
            VesselId = vesselId ?? String.Empty;
            AntennaId = antennaId ?? String.Empty;
            WormholeBodyId = wormholeBodyId ?? String.Empty;
            Channel = channel;
        }

        public string VesselId { get; private set; }
        public string AntennaId { get; private set; }
        public string WormholeBodyId { get; private set; }
        public int Channel { get; private set; }
        public bool IsRemoteTechVessel { get; set; }
        public bool IsDirectional { get; set; }
        public bool Activated { get; set; }
        public bool Powered { get; set; }
        public bool TargetsLocalWormhole { get; set; }
        public bool BridgeCapabilityEnabled { get; set; }
        public bool IsInOperationalRegion { get; set; }
        public double LocalDistance { get; set; }
        public bool HasLocalRange { get; set; }

        public string Key
        {
            get { return VesselId + "/" + AntennaId; }
        }
    }
}

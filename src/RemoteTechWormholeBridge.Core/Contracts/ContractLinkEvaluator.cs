using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTechWormholeBridge.Core.Contracts
{
    public sealed class ContractEndpointState
    {
        public ContractEndpointState(string mouthId, string vesselId)
        {
            MouthId = mouthId;
            VesselId = vesselId;
        }

        public string MouthId { get; private set; }
        public string VesselId { get; private set; }
    }

    public sealed class ContractActiveLinkState
    {
        public ContractActiveLinkState(
            string mouthAId,
            string vesselAId,
            string mouthBId,
            string vesselBId)
        {
            MouthAId = mouthAId;
            VesselAId = vesselAId;
            MouthBId = mouthBId;
            VesselBId = vesselBId;
        }

        public string MouthAId { get; private set; }
        public string VesselAId { get; private set; }
        public string MouthBId { get; private set; }
        public string VesselBId { get; private set; }
    }

    public static class ContractLinkEvaluator
    {
        public static bool HasGateway(
            IEnumerable<ContractEndpointState> endpoints,
            string mouthId)
        {
            return !String.IsNullOrEmpty(mouthId) &&
                   (endpoints ?? Enumerable.Empty<ContractEndpointState>()).Any(endpoint =>
                       endpoint != null && String.Equals(
                           endpoint.MouthId, mouthId, StringComparison.Ordinal));
        }

        public static bool IsLinked(IEnumerable<ContractActiveLinkState> links)
        {
            return (links ?? Enumerable.Empty<ContractActiveLinkState>()).Any(link => link != null);
        }

        public static bool HasRemoteKscService(
            IEnumerable<ContractActiveLinkState> links,
            string remoteMouthId,
            ISet<string> vesselsConnectedToKsc)
        {
            if (String.IsNullOrEmpty(remoteMouthId) || vesselsConnectedToKsc == null)
                return false;

            foreach (ContractActiveLinkState link in links ?? Enumerable.Empty<ContractActiveLinkState>())
            {
                if (link == null)
                    continue;
                if (String.Equals(link.MouthAId, remoteMouthId, StringComparison.Ordinal) &&
                    vesselsConnectedToKsc.Contains(link.VesselAId))
                    return true;
                if (String.Equals(link.MouthBId, remoteMouthId, StringComparison.Ordinal) &&
                    vesselsConnectedToKsc.Contains(link.VesselBId))
                    return true;
            }
            return false;
        }
    }
}

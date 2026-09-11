using System;

namespace RemoteTechWormholeBridge.Core.Contracts
{
    public static class WormholePairId
    {
        public static string Create(string mouthA, string mouthB)
        {
            if (String.IsNullOrWhiteSpace(mouthA))
                throw new ArgumentException("A wormhole mouth identifier is required.", nameof(mouthA));
            if (String.IsNullOrWhiteSpace(mouthB))
                throw new ArgumentException("A wormhole mouth identifier is required.", nameof(mouthB));

            return String.CompareOrdinal(mouthA, mouthB) <= 0
                ? mouthA + "|" + mouthB
                : mouthB + "|" + mouthA;
        }
    }
}

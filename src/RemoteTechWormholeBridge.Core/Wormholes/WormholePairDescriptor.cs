namespace RemoteTechWormholeBridge.Core.Wormholes
{
    public sealed class WormholePairDescriptor
    {
        public WormholePairDescriptor(WormholeBodyDescriptor bodyA, WormholeBodyDescriptor bodyB)
        {
            BodyA = bodyA;
            BodyB = bodyB;
        }

        public WormholeBodyDescriptor BodyA { get; private set; }
        public WormholeBodyDescriptor BodyB { get; private set; }
    }
}

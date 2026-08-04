namespace RemoteTechWormholeBridge.Core.Endpoints
{
    public enum EndpointFailureReason
    {
        None,
        InvalidIdentity,
        NotRemoteTechVessel,
        NotDirectional,
        Inactive,
        Unpowered,
        WrongTarget,
        BridgeCapabilityMissing,
        UnsafeRegion,
        TooCloseToWormhole,
        TooFarFromWormhole,
        InsufficientLocalRange,
        InvalidChannel
    }
}

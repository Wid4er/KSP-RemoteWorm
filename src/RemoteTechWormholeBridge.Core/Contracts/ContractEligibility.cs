namespace RemoteTechWormholeBridge.Core.Contracts
{
    public enum ContractIneligibilityReason
    {
        None,
        InvalidPair,
        UnresolvedSystem,
        RuntimeStateUnavailable,
        NeitherParentReached,
        PairLinked,
        HistoricallyCompleted,
        AlreadyOfferedOrActive
    }

    public sealed class ContractEligibilityState
    {
        public bool PairValid { get; set; }
        public bool SystemsResolved { get; set; }
        public bool RuntimeStateAvailable { get; set; }
        public bool ParentAReached { get; set; }
        public bool ParentBReached { get; set; }
        public bool PairLinked { get; set; }
        public bool HistoricallyCompleted { get; set; }
        public bool AlreadyOfferedOrActive { get; set; }

        public ContractIneligibilityReason Evaluate()
        {
            if (!PairValid)
                return ContractIneligibilityReason.InvalidPair;
            if (!SystemsResolved)
                return ContractIneligibilityReason.UnresolvedSystem;
            if (!RuntimeStateAvailable)
                return ContractIneligibilityReason.RuntimeStateUnavailable;
            if (!ParentAReached && !ParentBReached)
                return ContractIneligibilityReason.NeitherParentReached;
            if (PairLinked)
                return ContractIneligibilityReason.PairLinked;
            if (HistoricallyCompleted)
                return ContractIneligibilityReason.HistoricallyCompleted;
            if (AlreadyOfferedOrActive)
                return ContractIneligibilityReason.AlreadyOfferedOrActive;
            return ContractIneligibilityReason.None;
        }
    }
}

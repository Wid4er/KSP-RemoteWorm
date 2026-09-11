using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ContractConfigurator;
using Contracts;
using KSP.Localization;
using RemoteTechWormholeBridge.API;
using RemoteTechWormholeBridge.Core.Contracts;

namespace RemoteTechWormholeBridge.Contracts
{
    internal static class RTWBContractServices
    {
        internal const string ContractTypeName = "RTWB.InterstellarLink";
        internal const string PairDataKey = "pairId";
        private static readonly Dictionary<string, ContractIneligibilityReason> LastEligibility =
            new Dictionary<string, ContractIneligibilityReason>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedPairs =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedSystems =
            new HashSet<string>(StringComparer.Ordinal);

        internal static List<string> EligiblePairIds()
        {
            var eligible = new List<string>();
            foreach (RTWBWormholePairSnapshot pair in RTWBAPI.GetWormholePairs())
            {
                LogPairOnce(pair);
                ContractIneligibilityReason reason = Eligibility(pair, null);
                if (reason == ContractIneligibilityReason.None)
                    eligible.Add(pair.PairId);
            }

            return eligible;
        }

        internal static bool IsEligible(string pairId)
        {
            return IsEligible(pairId, null);
        }

        internal static bool IsEligible(string pairId, ConfiguredContract excludedContract)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair != null && Eligibility(pair, excludedContract) == ContractIneligibilityReason.None;
        }

        internal static RTWBWormholePairSnapshot Pair(string pairId)
        {
            return RTWBAPI.GetPair(pairId);
        }

        internal static string ContractTitle(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format(
                "#LOC_RTWB_contract_title", pair.SystemAName, pair.SystemBName);
        }

        internal static string ContractDescription(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format(
                "#LOC_RTWB_contract_description", pair.SystemAName, pair.SystemBName);
        }

        internal static string ContractSynopsis(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format(
                "#LOC_RTWB_contract_synopsis", pair.SystemBName);
        }

        internal static string GatewayATitle(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format("#LOC_RTWB_contract_gateway", pair.MouthAName);
        }

        internal static string GatewayBTitle(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format("#LOC_RTWB_contract_gateway", pair.MouthBName);
        }

        internal static string LinkTitle(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format(
                "#LOC_RTWB_contract_link", pair.MouthAName, pair.MouthBName);
        }

        internal static string StableTitle(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : Localizer.Format(
                "#LOC_RTWB_contract_stable", pair.MouthBName, ContractRewardPolicy.StableDurationDays);
        }

        internal static string MouthA(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : pair.MouthAId;
        }

        internal static string MouthB(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null ? "" : pair.MouthBId;
        }

        internal static float Funds(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null || pair.Tier < 1 ? 0 : (float)ContractRewardPolicy.Funds(pair.Tier);
        }

        internal static float Science(string pairId)
        {
            RTWBWormholePairSnapshot pair = Pair(pairId);
            return pair == null || pair.Tier < 1 ? 0 : (float)ContractRewardPolicy.Science(pair.Tier);
        }

        internal static float Reputation(string ignoredPairId)
        {
            return (float)ContractRewardPolicy.ReputationReward;
        }

        internal static string GetPairId(Contract contract)
        {
            ConfiguredContract configured = contract as ConfiguredContract;
            if (configured == null || configured.contractType == null ||
                !String.Equals(configured.contractType.name, ContractTypeName, StringComparison.Ordinal))
                return null;

            object value;
            return configured.uniqueData.TryGetValue(PairDataKey, out value) ? value as string : null;
        }

        private static IEnumerable<ConfiguredContract> CurrentContracts()
        {
            return ConfiguredContract.CurrentContracts.Where(IsOurContract);
        }

        private static ContractIneligibilityReason Eligibility(
            RTWBWormholePairSnapshot pair,
            ConfiguredContract excludedContract)
        {
            HashSet<string> completed = PairIds(CompletedContracts());
            HashSet<string> current = PairIds(CurrentContracts()
                .Where(contract => !ReferenceEquals(contract, excludedContract)));
            var state = new ContractEligibilityState
            {
                PairValid = true,
                SystemsResolved = pair.SystemsResolved && pair.Tier > 0,
                RuntimeStateAvailable = RTWBAPI.RuntimeStateAvailable,
                ParentAReached = pair.ParentAReached,
                ParentBReached = pair.ParentBReached,
                PairLinked = RTWBAPI.RuntimeStateAvailable && RTWBAPI.IsPairLinked(pair.PairId),
                HistoricallyCompleted = completed.Contains(pair.PairId),
                AlreadyOfferedOrActive = current.Contains(pair.PairId)
            };
            ContractIneligibilityReason reason = state.Evaluate();
            LogEligibilityChange(pair.PairId, reason);
            return reason;
        }

        private static IEnumerable<ConfiguredContract> CompletedContracts()
        {
            return ContractSystem.Instance == null
                ? Enumerable.Empty<ConfiguredContract>()
                : ContractSystem.Instance.ContractsFinished.OfType<ConfiguredContract>()
                    .Where(IsOurContract)
                    .Where(contract => contract.ContractState == Contract.State.Completed);
        }

        private static bool IsOurContract(ConfiguredContract contract)
        {
            return contract != null && contract.contractType != null &&
                   String.Equals(contract.contractType.name, ContractTypeName, StringComparison.Ordinal);
        }

        private static HashSet<string> PairIds(IEnumerable<ConfiguredContract> contracts)
        {
            return new HashSet<string>((contracts ?? Enumerable.Empty<ConfiguredContract>())
                .Select(contract => GetPairId(contract))
                .Where(pairId => !String.IsNullOrEmpty(pairId)), StringComparer.Ordinal);
        }

        private static void LogEligibilityChange(string pairId, ContractIneligibilityReason reason)
        {
            ContractIneligibilityReason previous;
            if (LastEligibility.TryGetValue(pairId, out previous) && previous == reason)
                return;

            LastEligibility[pairId] = reason;
            ContractsLog.Info("eligibility pairId=" + pairId +
                              " eligible=" + (reason == ContractIneligibilityReason.None) +
                              " reason=" + reason);
        }

        private static void LogPairOnce(RTWBWormholePairSnapshot pair)
        {
            if (!LoggedPairs.Add(pair.PairId))
                return;

            LogSystemOnce(pair.SystemAId, pair.SystemAName, pair.DepthA);
            LogSystemOnce(pair.SystemBId, pair.SystemBName, pair.DepthB);

            string funds = pair.Tier < 1 ? "0" : ContractRewardPolicy.Funds(pair.Tier)
                .ToString("0", CultureInfo.InvariantCulture);
            string science = pair.Tier < 1 ? "0" : ContractRewardPolicy.Science(pair.Tier)
                .ToString("0", CultureInfo.InvariantCulture);
            ContractsLog.Info("pair-discovered pairId=" + pair.PairId +
                              " systemA=" + pair.SystemAId +
                              " systemB=" + pair.SystemBId +
                              " depthA=" + pair.DepthA +
                              " depthB=" + pair.DepthB +
                              " tier=" + pair.Tier +
                              " funds=" + funds +
                              " science=" + science);
        }

        private static void LogSystemOnce(string systemId, string systemName, int depth)
        {
            if (String.IsNullOrEmpty(systemId) || !LoggedSystems.Add(systemId))
                return;
            ContractsLog.Info("system-discovered id=" + systemId +
                              " name=" + systemName +
                              " depth=" + depth);
        }
    }
}

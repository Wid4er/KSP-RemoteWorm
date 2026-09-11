using ContractConfigurator;

namespace RemoteTechWormholeBridge.Contracts
{
    public sealed class RTWBPairAvailableRequirement : ContractRequirement
    {
        private string pairId;

        public RTWBPairAvailableRequirement()
        {
            needsTitle = true;
        }

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            bool valid = base.LoadFromConfig(configNode);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pairId", value => pairId = value, this);
            return valid;
        }

        public override bool RequirementMet(ConfiguredContract contract)
        {
            return RTWBContractServices.IsEligible(pairId, contract);
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("pairId", pairId);
        }

        public override void OnLoad(ConfigNode configNode)
        {
            pairId = configNode.GetValue("pairId");
        }

        protected override string RequirementText()
        {
            return KSP.Localization.Localizer.GetStringByTag("#LOC_RTWB_contract_requirement");
        }
    }
}

using ContractConfigurator;
using ContractConfigurator.Parameters;
using Contracts;
using RemoteTechWormholeBridge.API;
using RemoteTechWormholeBridge.Core.Contracts;
using System.Linq;
using UnityEngine;

namespace RemoteTechWormholeBridge.Contracts.Parameters
{
    public sealed class RTWBLinkFactory : ParameterFactory
    {
        private string pairId;

        public override bool Load(ConfigNode configNode)
        {
            bool valid = base.Load(configNode);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pairId", value => pairId = value, this);
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new RTWBLinkParameter(pairId, title);
        }
    }

    public sealed class RTWBLinkParameter : ContractConfiguratorParameter
    {
        private string pairId;
        private float nextCheck;

        public RTWBLinkParameter()
        {
        }

        internal RTWBLinkParameter(string pairId, string title)
            : base(title)
        {
            this.pairId = pairId;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (state == ParameterState.Complete || Time.realtimeSinceStartup < nextCheck)
                return;
            nextCheck = Time.realtimeSinceStartup + 1f;
            if (RTWBAPI.RuntimeStateAvailable && ContractLinkEvaluator.IsLinked(
                    RTWBAPI.GetActiveLinks(pairId).Select(link => new ContractActiveLinkState(
                        link.MouthAId,
                        link.VesselAId.ToString("D"),
                        link.MouthBId,
                        link.VesselBId.ToString("D")))))
                SetState(ParameterState.Complete);
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("pairId", pairId);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            pairId = node.GetValue("pairId");
        }
    }
}

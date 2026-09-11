using System;
using System.Linq;
using ContractConfigurator;
using ContractConfigurator.Parameters;
using Contracts;
using RemoteTechWormholeBridge.API;
using RemoteTechWormholeBridge.Core.Contracts;
using UnityEngine;

namespace RemoteTechWormholeBridge.Contracts.Parameters
{
    public sealed class RTWBGatewayFactory : ParameterFactory
    {
        private string pairId;
        private string mouthId;

        public override bool Load(ConfigNode configNode)
        {
            bool valid = base.Load(configNode);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "pairId", value => pairId = value, this);
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "mouthId", value => mouthId = value, this);
            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new RTWBGatewayParameter(pairId, mouthId, title);
        }
    }

    public sealed class RTWBGatewayParameter : ContractConfiguratorParameter
    {
        private string pairId;
        private string mouthId;
        private float nextCheck;

        public RTWBGatewayParameter()
        {
        }

        internal RTWBGatewayParameter(string pairId, string mouthId, string title)
            : base(title)
        {
            this.pairId = pairId;
            this.mouthId = mouthId;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (state == ParameterState.Complete || Time.realtimeSinceStartup < nextCheck)
                return;
            nextCheck = Time.realtimeSinceStartup + 1f;
            if (!RTWBAPI.RuntimeStateAvailable)
                return;

            bool present = ContractLinkEvaluator.HasGateway(
                RTWBAPI.GetEndpointVessels(pairId).Select(endpoint =>
                    new ContractEndpointState(endpoint.MouthId, endpoint.VesselId.ToString("D"))),
                mouthId);
            SetState(present ? ParameterState.Complete : ParameterState.Incomplete);
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            node.AddValue("pairId", pairId);
            node.AddValue("mouthId", mouthId);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            pairId = node.GetValue("pairId");
            mouthId = node.GetValue("mouthId");
        }
    }
}

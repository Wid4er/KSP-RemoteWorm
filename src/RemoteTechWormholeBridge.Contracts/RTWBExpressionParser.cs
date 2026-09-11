using System;
using System.Collections.Generic;
using ContractConfigurator.ExpressionParser;

namespace RemoteTechWormholeBridge.Contracts
{
    public sealed class RTWBExpressionValue
    {
    }

    public sealed class RTWBExpressionParser : ClassExpressionParser<RTWBExpressionValue>,
        IExpressionParserRegistrer
    {
        static RTWBExpressionParser()
        {
            RegisterFunctions();
        }

        public void RegisterExpressionParsers()
        {
            RegisterParserType(typeof(RTWBExpressionValue), typeof(RTWBExpressionParser));
        }

        public override RTWBExpressionValue ParseIdentifier(Token token)
        {
            if (token.sval.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            throw new ArgumentException("RTWBExpressionValue has no literal representation.");
        }

        private static void RegisterFunctions()
        {
            RegisterGlobalFunction(new Function<List<string>>(
                "RTWBEligiblePairIds", RTWBContractServices.EligiblePairIds, false));
            RegisterGlobalFunction(new Function<string, bool>(
                "RTWBPairEligible", RTWBContractServices.IsEligible, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBContractTitle", RTWBContractServices.ContractTitle, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBContractDescription", RTWBContractServices.ContractDescription, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBContractSynopsis", RTWBContractServices.ContractSynopsis, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBGatewayATitle", RTWBContractServices.GatewayATitle, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBGatewayBTitle", RTWBContractServices.GatewayBTitle, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBLinkTitle", RTWBContractServices.LinkTitle, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBStableTitle", RTWBContractServices.StableTitle, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBMouthA", RTWBContractServices.MouthA, false));
            RegisterGlobalFunction(new Function<string, string>(
                "RTWBMouthB", RTWBContractServices.MouthB, false));
            RegisterGlobalFunction(new Function<string, float>(
                "RTWBFunds", RTWBContractServices.Funds, false));
            RegisterGlobalFunction(new Function<string, float>(
                "RTWBScience", RTWBContractServices.Science, false));
            RegisterGlobalFunction(new Function<string, float>(
                "RTWBReputation", RTWBContractServices.Reputation, false));
        }
    }
}

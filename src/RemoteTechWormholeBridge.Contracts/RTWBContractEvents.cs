using System;
using ContractConfigurator;
using Contracts;
using UnityEngine;

namespace RemoteTechWormholeBridge.Contracts
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public sealed class RTWBContractEvents : MonoBehaviour
    {
        private void Start()
        {
            ContractsLog.Info("Contract Configurator detected; integration active");
            GameEvents.Contract.onOffered.Add(OnOffered);
            GameEvents.Contract.onAccepted.Add(OnAccepted);
            GameEvents.Contract.onCompleted.Add(OnCompleted);
        }

        private void OnDestroy()
        {
            GameEvents.Contract.onOffered.Remove(OnOffered);
            GameEvents.Contract.onAccepted.Remove(OnAccepted);
            GameEvents.Contract.onCompleted.Remove(OnCompleted);
        }

        private static void OnOffered(Contract contract)
        {
            LogContractEvent("contract-offered", contract);
        }

        private static void OnAccepted(Contract contract)
        {
            LogContractEvent("contract-accepted", contract);
        }

        private static void OnCompleted(Contract contract)
        {
            LogContractEvent("contract-completed", contract);
        }

        private static void LogContractEvent(string eventName, Contract contract)
        {
            string pairId = RTWBContractServices.GetPairId(contract);
            if (!String.IsNullOrEmpty(pairId))
                ContractsLog.Info(eventName + " pairId=" + pairId);
        }
    }
}

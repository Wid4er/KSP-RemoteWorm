using UnityEngine;

namespace RemoteTechWormholeBridge.Contracts
{
    internal static class ContractsLog
    {
        internal static void Info(string message)
        {
            Debug.Log("[RTWB-Contracts] " + message);
        }

        internal static void Warning(string message)
        {
            Debug.LogWarning("[RTWB-Contracts] " + message);
        }
    }
}

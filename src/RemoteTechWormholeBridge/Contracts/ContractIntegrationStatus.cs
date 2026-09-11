using System;
using System.Linq;

namespace RemoteTechWormholeBridge
{
    internal static class ContractIntegrationStatus
    {
        private static bool logged;

        internal static bool IsAvailable
        {
            get
            {
                return AssemblyLoader.loadedAssemblies.Any(assembly =>
                    assembly != null &&
                    String.Equals(assembly.name, "ContractConfigurator", StringComparison.Ordinal));
            }
        }

        internal static void LogAvailabilityOnce()
        {
            if (logged)
                return;

            logged = true;
            UnityEngine.Debug.Log("[RTWB-Contracts] Contract Configurator " +
                                  (IsAvailable ? "detected" : "not detected"));
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using KopernicusExpansion.Wormholes;
using RemoteTech;
using RemoteTech.RangeModel;
using RemoteTech.SimpleTypes;
using UnityEngine;

namespace RemoteTechWormholeBridge
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    internal sealed class HarmonyBootstrap : MonoBehaviour
    {
        private const string DiagnosticHarmonyId =
            "net.remote-tech-wormhole-bridge.diagnostics";
        private const string NetworkHarmonyId =
            "net.remote-tech-wormhole-bridge.network";

        internal static bool IsPatched { get; private set; }
        internal static bool IsNetworkPatched { get; private set; }

        private void Awake()
        {
            PatchJumpDiagnostics();
        }

        internal static void EnsureNetworkPatched()
        {
            if (!IsNetworkPatched)
                PatchRemoteTechNetwork();
        }

        private static void PatchJumpDiagnostics()
        {
            try
            {
                MethodInfo original = AccessTools.Method(
                    typeof(WormholeComponent),
                    "MakeOrbit",
                    new[] { typeof(OrbitDriver), typeof(CelestialBody) });

                if (original == null)
                {
                    Log.Error("Harmony target missing: WormholeComponent.MakeOrbit(OrbitDriver,CelestialBody).");
                    return;
                }

                var harmony = new Harmony(DiagnosticHarmonyId);
                harmony.Patch(
                    original,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(WormholeJumpDiagnostics), "Prefix")),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(WormholeJumpDiagnostics), "Postfix")),
                    finalizer: new HarmonyMethod(AccessTools.Method(typeof(WormholeJumpDiagnostics), "Finalizer")));

                IsPatched = true;
                Log.Info("Harmony diagnostic patch applied target=" + original.DeclaringType.FullName +
                         "." + original.Name);
            }
            catch (Exception exception)
            {
                IsPatched = false;
                Log.Error("Harmony diagnostic patch failed: " + exception);
            }
        }

        private static void PatchRemoteTechNetwork()
        {
            var harmony = new Harmony(NetworkHarmonyId);
            try
            {
                MethodInfo updateGraph = AccessTools.Method(
                    typeof(NetworkManager),
                    "UpdateGraph",
                    new[] { typeof(ISatellite) });
                MethodInfo findPath = AccessTools.Method(
                    typeof(NetworkManager),
                    "FindPath",
                    new[] { typeof(ISatellite), typeof(System.Collections.Generic.IEnumerable<ISatellite>) });
                MethodInfo linkDistance = AccessTools.Method(
                    typeof(RangeModelExtensions),
                    "DistanceTo",
                    new[] { typeof(ISatellite), typeof(NetworkLink<ISatellite>) });
                MethodInfo heuristicDistance = AccessTools.Method(
                    typeof(RangeModelExtensions),
                    "DistanceTo",
                    new[] { typeof(ISatellite), typeof(ISatellite) });

                if (updateGraph == null || findPath == null ||
                    linkDistance == null || heuristicDistance == null)
                    throw new MissingMethodException(
                        "RemoteTech network signatures do not match the supported API.");

                harmony.Patch(
                    updateGraph,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(NetworkGraphPatch), "Prefix")),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(NetworkGraphPatch), "Postfix")));
                harmony.Patch(
                    findPath,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(NetworkPathPatch), "Prefix")),
                    postfix: new HarmonyMethod(AccessTools.Method(typeof(NetworkPathPatch), "Postfix")),
                    finalizer: new HarmonyMethod(AccessTools.Method(typeof(NetworkPathPatch), "Finalizer")));
                harmony.Patch(
                    linkDistance,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(NetworkCostPatch), "Prefix")));
                harmony.Patch(
                    heuristicDistance,
                    prefix: new HarmonyMethod(AccessTools.Method(typeof(NetworkHeuristicPatch), "Prefix")));

                IsNetworkPatched = true;
                Log.Info("Harmony network patches applied updateGraph=" + updateGraph.Name +
                         " findPath=" + findPath.Name +
                         " cost=" + linkDistance.Name +
                         " heuristic=" + heuristicDistance.Name);
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(NetworkHarmonyId);
                IsNetworkPatched = false;
                Log.Error("Harmony network patches failed; graph integration disabled: " + exception);
            }
        }
    }
}

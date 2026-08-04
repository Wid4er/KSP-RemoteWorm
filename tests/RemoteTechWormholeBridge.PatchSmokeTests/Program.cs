using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KopernicusExpansion.Wormholes;
using RemoteTech;
using RemoteTech.RangeModel;
using RemoteTech.SimpleTypes;

namespace RemoteTechWormholeBridge.PatchSmokeTests
{
    internal static class Program
    {
        private const string HarmonyId = "net.remote-tech-wormhole-bridge.smoke-test";

        private static int Main()
        {
            try
            {
                Assembly plugin = typeof(ModuleRTWormholeBridge).Assembly;
                Type diagnostics = plugin.GetType(
                    "RemoteTechWormholeBridge.WormholeJumpDiagnostics", true);
                Type controller = plugin.GetType(
                    "RemoteTechWormholeBridge.DiagnosticController", true);
                Type graphPatch = plugin.GetType(
                    "RemoteTechWormholeBridge.NetworkGraphPatch", true);
                Type pathPatch = plugin.GetType(
                    "RemoteTechWormholeBridge.NetworkPathPatch", true);
                Type costPatch = plugin.GetType(
                    "RemoteTechWormholeBridge.NetworkCostPatch", true);
                Type heuristicPatch = plugin.GetType(
                    "RemoteTechWormholeBridge.NetworkHeuristicPatch", true);
                Type bridgeLink = plugin.GetType(
                    "RemoteTechWormholeBridge.RuntimeBridgeLink", true);
                MethodInfo kexTarget = AccessTools.Method(
                    typeof(WormholeComponent),
                    "MakeOrbit",
                    new[] { typeof(OrbitDriver), typeof(CelestialBody) });
                MethodInfo signatureTarget = AccessTools.Method(
                    typeof(Program),
                    "SignatureTarget",
                    new[] { typeof(OrbitDriver), typeof(CelestialBody) });

                Assert(kexTarget != null, "KEX MakeOrbit target must exist");
                Assert(signatureTarget != null, "local signature target must exist");
                MethodInfo prefix = AccessTools.Method(diagnostics, "Prefix");
                MethodInfo postfix = AccessTools.Method(diagnostics, "Postfix");
                MethodInfo finalizer = AccessTools.Method(diagnostics, "Finalizer");
                Assert(prefix != null && postfix != null && finalizer != null,
                    "all diagnostic patch methods must exist");

                MethodInfo soiHandler = controller.GetMethod(
                    "OnVesselSoiChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(soiHandler != null && !soiHandler.IsStatic,
                    "KSP EventData requires an instance SOI handler");

                CustomAttributeData controllerAddon = controller.GetCustomAttributesData()
                    .Single(attribute => attribute.AttributeType == typeof(KSPAddon));
                Assert(Convert.ToInt32(controllerAddon.ConstructorArguments[0].Value) ==
                       Convert.ToInt32(KSPAddon.Startup.EveryScene),
                    "RTWB controller must start in Flight and Tracking Station scenes");

                Assembly remoteTech = typeof(NetworkManager).Assembly;
                Type protoAntenna = remoteTech.GetType("RemoteTech.Modules.ProtoAntenna", true);
                FieldInfo protoPart = protoAntenna.GetField(
                    "mProtoPart", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(protoPart != null && protoPart.FieldType.FullName == "ProtoPartSnapshot",
                    "unloaded endpoint binding requires ProtoAntenna.mProtoPart");

                MethodInfo updateGraph = AccessTools.Method(
                    typeof(NetworkManager), "UpdateGraph", new[] { typeof(ISatellite) });
                MethodInfo findPath = AccessTools.Method(
                    typeof(NetworkManager), "FindPath",
                    new[]
                    {
                        typeof(ISatellite),
                        typeof(System.Collections.Generic.IEnumerable<ISatellite>)
                    });
                MethodInfo linkDistance = AccessTools.Method(
                    typeof(RangeModelExtensions), "DistanceTo",
                    new[] { typeof(ISatellite), typeof(NetworkLink<ISatellite>) });
                MethodInfo heuristicDistance = AccessTools.Method(
                    typeof(RangeModelExtensions), "DistanceTo",
                    new[] { typeof(ISatellite), typeof(ISatellite) });
                ConstructorInfo linkConstructor = typeof(NetworkLink<ISatellite>).GetConstructor(
                    new[]
                    {
                        typeof(ISatellite),
                        typeof(System.Collections.Generic.List<IAntenna>),
                        typeof(LinkType)
                    });
                Assert(updateGraph != null && !updateGraph.IsPublic,
                    "RemoteTech private UpdateGraph(ISatellite) target must exist");
                Assert(findPath != null && linkDistance != null && heuristicDistance != null,
                    "RemoteTech path and distance targets must exist");
                Assert(linkConstructor != null,
                    "RemoteTech NetworkLink constructor signature must exist");

                FieldInfo sourceGuid = bridgeLink.GetField(
                    "SourceGuid", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo targetGuid = bridgeLink.GetField(
                    "TargetGuid", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert(sourceGuid != null && sourceGuid.FieldType == typeof(Guid) &&
                       targetGuid != null && targetGuid.FieldType == typeof(Guid),
                    "bridge links must store stable vessel GUID fields");

                MethodInfo graphSignatureTarget = AccessTools.Method(
                    typeof(NetworkSignatureTarget), "GraphTarget", new[] { typeof(ISatellite) });
                MethodInfo pathSignatureTarget = AccessTools.Method(
                    typeof(NetworkSignatureTarget), "PathTarget",
                    new[]
                    {
                        typeof(ISatellite),
                        typeof(System.Collections.Generic.IEnumerable<ISatellite>)
                    });
                MethodInfo costSignatureTarget = AccessTools.Method(
                    typeof(Program), "CostTarget",
                    new[] { typeof(ISatellite), typeof(NetworkLink<ISatellite>) });
                MethodInfo heuristicSignatureTarget = AccessTools.Method(
                    typeof(Program), "HeuristicTarget",
                    new[] { typeof(ISatellite), typeof(ISatellite) });

                var harmony = new Harmony(HarmonyId);
                harmony.Patch(
                    signatureTarget,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix),
                    finalizer: new HarmonyMethod(finalizer));

                Patches patchInfo = Harmony.GetPatchInfo(signatureTarget);
                Assert(patchInfo != null && patchInfo.Owners.Contains(HarmonyId),
                    "Harmony must accept the exact Prefix/Postfix/Finalizer signatures");

                harmony.Patch(
                    graphSignatureTarget,
                    prefix: new HarmonyMethod(AccessTools.Method(graphPatch, "Prefix")),
                    postfix: new HarmonyMethod(AccessTools.Method(graphPatch, "Postfix")));
                harmony.Patch(
                    pathSignatureTarget,
                    prefix: new HarmonyMethod(AccessTools.Method(pathPatch, "Prefix")),
                    postfix: new HarmonyMethod(AccessTools.Method(pathPatch, "Postfix")),
                    finalizer: new HarmonyMethod(AccessTools.Method(pathPatch, "Finalizer")));
                harmony.Patch(
                    costSignatureTarget,
                    prefix: new HarmonyMethod(AccessTools.Method(costPatch, "Prefix")));
                harmony.Patch(
                    heuristicSignatureTarget,
                    prefix: new HarmonyMethod(AccessTools.Method(heuristicPatch, "Prefix")));

                AssertPatched(graphSignatureTarget, "UpdateGraph");
                AssertPatched(pathSignatureTarget, "FindPath");
                AssertPatched(costSignatureTarget, "path cost DistanceTo");
                AssertPatched(heuristicSignatureTarget, "heuristic DistanceTo");

                harmony.UnpatchAll(HarmonyId);
                Console.WriteLine("RTWB Harmony patch smoke test passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        // Uses the exact original argument types without executing KSP or Unity code.
        private static void SignatureTarget(OrbitDriver driver, CelestialBody reference)
        {
        }

        private static double CostTarget(
            ISatellite source,
            NetworkLink<ISatellite> link)
        {
            return 0;
        }

        private static double HeuristicTarget(ISatellite source, ISatellite target)
        {
            return 0;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertPatched(MethodInfo method, string name)
        {
            Patches patchInfo = Harmony.GetPatchInfo(method);
            Assert(patchInfo != null && patchInfo.Owners.Contains(HarmonyId),
                "Harmony must accept the " + name + " patch signature");
        }

        private sealed class NetworkSignatureTarget : NetworkManager
        {
            private void GraphTarget(ISatellite source)
            {
            }

            private void PathTarget(
                ISatellite source,
                System.Collections.Generic.IEnumerable<ISatellite> commandStations)
            {
            }
        }
    }
}

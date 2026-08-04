using System.Collections;
using UnityEngine;

namespace RemoteTechWormholeBridge
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal sealed class DiagnosticController : MonoBehaviour
    {
        private static bool refreshRequested;
        private bool supportedScene;
        private bool subscribed;
        private float nextPeriodicRefresh;

        private const float RefreshIntervalSeconds = 1f;

        internal static void RequestRefresh()
        {
            refreshRequested = true;
        }

        private IEnumerator Start()
        {
            supportedScene = HighLogic.LoadedSceneIsFlight ||
                             HighLogic.LoadedScene == GameScenes.TRACKSTATION;
            if (!supportedScene)
            {
                enabled = false;
                yield break;
            }

            AssemblyVersions.LogDetectedVersions();

            GameEvents.onVesselSOIChanged.Add(OnVesselSoiChanged);
            subscribed = true;

            yield return new WaitForSeconds(1f);
            HarmonyBootstrap.EnsureNetworkPatched();
            WormholeRenderManager.Attach();
            Log.Info("mode=logical-link graphMutation=" + HarmonyBootstrap.IsNetworkPatched +
                     " renderer=" + WormholeRenderManager.IsAttached +
                     " scene=" + HighLogic.LoadedScene +
                     " jumpDiagnosticsPatched=" + HarmonyBootstrap.IsPatched);
            Refresh(StartReason(), true, true);
            nextPeriodicRefresh = Time.realtimeSinceStartup + RefreshIntervalSeconds;
        }

        private void Update()
        {
            if (refreshRequested)
            {
                refreshRequested = false;
                Refresh("requested", true, true);
                nextPeriodicRefresh = Time.realtimeSinceStartup + RefreshIntervalSeconds;
                return;
            }

            if (Time.realtimeSinceStartup < nextPeriodicRefresh)
                return;

            nextPeriodicRefresh = Time.realtimeSinceStartup + RefreshIntervalSeconds;
            Refresh("periodic", false, false);
        }

        private void OnDestroy()
        {
            if (!supportedScene)
                return;

            WormholeRenderManager.Detach();
            WormholeNetworkIntegration.Replace(null);
            if (!subscribed)
                return;

            GameEvents.onVesselSOIChanged.Remove(OnVesselSoiChanged);
            subscribed = false;
        }

        private void OnVesselSoiChanged(
            GameEvents.HostedFromToAction<Vessel, CelestialBody> transition)
        {
            string vessel = transition.host == null ? "<null>" : transition.host.id.ToString("D");
            string from = transition.from == null ? "<null>" : transition.from.name;
            string to = transition.to == null ? "<null>" : transition.to.name;
            bool knownPair = KexWormholeCatalog.ArePartners(transition.from, transition.to);

            Log.Info("soi-change vessel=" + vessel + " from=" + from + " to=" + to +
                     " wormholePair=" + knownPair);
            Refresh("soi-change", true, true);
        }

        private static void Refresh(string reason, bool refreshCatalog, bool logUnchanged)
        {
            try
            {
                if (refreshCatalog)
                    KexWormholeCatalog.Refresh(reason);
                RemoteTechEndpointScanner.Refresh(reason, logUnchanged);
                WormholeCoverageScanner.Refresh(reason, logUnchanged);
            }
            catch (System.Exception exception)
            {
                WormholeNetworkIntegration.Replace(null);
                Log.Error("diagnostic refresh failed reason=" + reason + " exception=" + exception);
            }
        }

        private static string StartReason()
        {
            return HighLogic.LoadedScene == GameScenes.TRACKSTATION
                ? "tracking-start"
                : "flight-start";
        }
    }
}

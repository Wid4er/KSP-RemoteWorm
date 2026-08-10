using System;
using System.Collections.Generic;
using RemoteTech;
using RemoteTechWormholeBridge.Core.Endpoints;
using RemoteTechWormholeBridge.Core.Geometry;
using UnityEngine;

namespace RemoteTechWormholeBridge
{
    internal sealed class WormholeRenderManager : MonoBehaviour
    {
        private const float LineWidth = 3f;
        private const float GuideLineWidth = 2f;
        private const int GuideRingSegments = 64;
        private static readonly Color BridgeColor = new Color(1f, 79f / 255f, 216f / 255f, 1f);
        private static readonly Color ConeColor = new Color(1f, 79f / 255f, 216f / 255f, 0.7f);
        private static readonly Color GuideColor = new Color(1f, 48f / 255f, 48f / 255f, 0.8f);
        private static readonly double[] GuideRingCosines = BuildRingCoordinates(false);
        private static readonly double[] GuideRingSines = BuildRingCoordinates(true);

        private static WormholeRenderManager instance;
        private readonly List<MapLineMesh> linePool = new List<MapLineMesh>();
        private Vessel selectedRouteVessel;
        private bool loggedVisible;
        private bool renderFailureLogged;

        internal static bool IsAttached
        {
            get { return instance != null; }
        }

        internal static void Attach()
        {
            if (instance != null || MapView.MapCamera == null)
                return;

            instance = MapView.MapCamera.gameObject.AddComponent<WormholeRenderManager>();
            Log.Info("renderer-attached bridgeColor=#FF4FD8 guideColor=#FF3030" +
                     " operationalBand=per-wormhole" +
                     " coneLength=per-wormhole guideRings=true" +
                     " visibility=selected-wormhole-pair-route");
        }

        internal static void Detach()
        {
            if (instance == null)
                return;

            Destroy(instance);
            instance = null;
        }

        private void OnPreCull()
        {
            try
            {
                RenderFrame();
            }
            catch (Exception exception)
            {
                HideUnused(0);
                if (renderFailureLogged)
                    return;

                renderFailureLogged = true;
                Log.Error("renderer failed open: " + exception);
            }
        }

        private void RenderFrame()
        {
            if (!MapView.MapIsEnabled || RTCore.Instance == null || RTCore.Instance.Renderer == null)
            {
                HideUnused(0);
                return;
            }

            Vessel focused = MapView.MapCamera == null || MapView.MapCamera.target == null
                ? null
                : MapView.MapCamera.target.vessel;
            Vessel active = FlightGlobals.ActiveVessel;
            if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                selectedRouteVessel = focused;
            else if (active != null)
                selectedRouteVessel = active;
            else if (focused != null)
                selectedRouteVessel = focused;

            if (focused == null && selectedRouteVessel == null)
            {
                HideUnused(0);
                return;
            }

            bool showSegments = RTCore.Instance.Renderer.ShowDish;
            bool showCones = RTCore.Instance.Renderer.ShowCone;
            int used = 0;
            int visibleLinks = 0;
            int visibleConeEndpoints = 0;
            int visibleGuideRings = 0;
            bool visibleFromRoute = false;
            bool renderedSegments = false;
            bool renderedCones = false;
            bool renderedGuideRings = false;
            string guideBody = "<none>";
            foreach (RuntimeBridgeLink link in WormholeNetworkIntegration.SnapshotVisualLinks())
            {
                bool endpointSelected = IsEndpointSelected(link, focused);
                bool routeSelected = !endpointSelected &&
                                     WormholeNetworkIntegration.IsUsedByRouteFrom(
                                         link,
                                         selectedRouteVessel);
                if (!endpointSelected && !routeSelected)
                    continue;

                Vector3d radialA;
                Vector3d radialB;
                Vector3d relayA;
                Vector3d relayB;
                Vector3d entryA;
                Vector3d entryB;
                if (!TryGeometry(link.Source, out radialA, out relayA, out entryA) ||
                    !TryGeometry(link.Target, out radialB, out relayB, out entryB))
                    continue;

                ++visibleLinks;
                visibleFromRoute |= routeSelected;
                if (showSegments)
                {
                    DrawLine(ref used, relayA, entryA, BridgeColor);
                    DrawLine(ref used, relayB, entryB, BridgeColor);
                    renderedSegments = true;
                }

            }

            Vessel coneOwner = SelectConeOwner(focused, selectedRouteVessel);
            if (showCones && coneOwner != null)
            {
                List<RuntimeEndpoint> endpoints = RemoteTechEndpointScanner.SnapshotAccepted();
                RuntimeEndpoint ownerEndpoint = endpoints.Find(endpoint =>
                    endpoint != null && endpoint.Vessel == coneOwner);
                KexBodyInfo ownerPartner;
                if (ownerEndpoint == null ||
                    !KexWormholeCatalog.TryGetPartner(ownerEndpoint.Wormhole, out ownerPartner))
                    ownerPartner = null;

                foreach (RuntimeEndpoint endpoint in endpoints)
                {
                    if (!IsInWormholePair(endpoint, ownerEndpoint, ownerPartner))
                        continue;

                    KexBodyInfo partner;
                    Vector3d outputRadial;
                    Vector3d relay;
                    Vector3d transitionPoint;
                    if (!KexWormholeCatalog.TryGetPartner(endpoint.Wormhole, out partner) ||
                        !TryGeometry(endpoint, out outputRadial, out relay, out transitionPoint))
                        continue;

                    if (DrawOutgoingCone(ref used, endpoint, partner, outputRadial))
                    {
                        ++visibleConeEndpoints;
                        renderedCones = true;
                    }
                }
            }

            RuntimeEndpoint guideEndpoint = showCones
                ? SelectGuideEndpoint(focused, selectedRouteVessel)
                : null;
            if (guideEndpoint != null && DrawGuideRings(ref used, guideEndpoint))
            {
                visibleGuideRings = 2;
                renderedGuideRings = true;
                guideBody = guideEndpoint.Wormhole.Body.name;
            }

            HideUnused(used);
            if (!loggedVisible &&
                (visibleLinks > 0 || visibleConeEndpoints > 0 || visibleGuideRings > 0) && used > 0)
            {
                loggedVisible = true;
                Log.Info("renderer-visible links=" + visibleLinks +
                         " coneEndpoints=" + visibleConeEndpoints +
                         " meshes=" + used +
                         " segments=" + renderedSegments +
                         " cones=" + renderedCones +
                         " guideRings=" + renderedGuideRings +
                         " guideBody=" + guideBody +
                         " selection=" + (visibleFromRoute ? "route" : "endpoint") +
                         " selected=" + SelectedName(focused, selectedRouteVessel) +
                         " coneLength=per-wormhole");
            }
        }

        private bool DrawOutgoingCone(
            ref int used,
            RuntimeEndpoint source,
            KexBodyInfo destination,
            Vector3d outputRadial)
        {
            if (source == null || destination == null || destination.Body == null ||
                destination.OperationalBand == null || source.Antenna == null)
                return false;

            Vector3d direction = outputRadial.normalized;
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0)
                return false;

            Vector3d origin = destination.Body.position +
                              direction * destination.TransitionRadius;
            double cosine = Math.Max(-1.0, Math.Min(1.0, source.Antenna.CosAngle));
            double halfAngle = Math.Acos(cosine);
            Vector3d perpendicular = ConePerpendicular(destination.Body, direction);
            if (perpendicular.sqrMagnitude <= 0)
                return false;

            double length = destination.OperationalBand.MaximumLocalDistance;
            Vector3d axial = direction * length;
            Vector3d lateral = perpendicular * (Math.Tan(halfAngle) * length);
            DrawLine(ref used, origin, origin + axial + lateral, ConeColor);
            DrawLine(ref used, origin, origin + axial - lateral, ConeColor);
            return true;
        }

        private bool DrawGuideRings(ref int used, RuntimeEndpoint endpoint)
        {
            if (endpoint == null || endpoint.Vessel == null || endpoint.Wormhole == null ||
                endpoint.Wormhole.Body == null || endpoint.Wormhole.OperationalBand == null)
                return false;

            Vector3d basisX;
            Vector3d basisY;
            if (!TryRingBasis(endpoint, out basisX, out basisY))
                return false;

            BridgeOperationalBand band = endpoint.Wormhole.OperationalBand;
            DrawGuideRing(ref used, endpoint.Wormhole.Body.position, basisX, basisY, band.InnerRadius);
            DrawGuideRing(ref used, endpoint.Wormhole.Body.position, basisX, basisY, band.OuterRadius);
            return true;
        }

        private void DrawGuideRing(
            ref int used,
            Vector3d center,
            Vector3d basisX,
            Vector3d basisY,
            double radius)
        {
            if (!IsFinite(radius) || radius <= 0)
                return;

            Vector3d previous = center + basisX * radius;
            for (int index = 1; index <= GuideRingSegments; ++index)
            {
                Vector3d current = center +
                                   (basisX * GuideRingCosines[index] +
                                    basisY * GuideRingSines[index]) * radius;
                DrawLine(ref used, previous, current, GuideColor, GuideLineWidth);
                previous = current;
            }
        }

        private static bool TryRingBasis(
            RuntimeEndpoint endpoint,
            out Vector3d basisX,
            out Vector3d basisY)
        {
            basisX = Vector3d.zero;
            basisY = Vector3d.zero;
            Vessel vessel = endpoint.Vessel;
            CelestialBody body = endpoint.Wormhole.Body;
            if (vessel.orbit != null && vessel.orbit.referenceBody == body)
            {
                double universalTime = Planetarium.GetUniversalTime();
                Vector3d orbitalPosition = vessel.orbit.getRelativePositionAtUT(universalTime);
                Vector3d orbitalVelocity = vessel.orbit.getOrbitalVelocityAtUT(universalTime);
                Vector3d orbitalNormal = Vector3d.Cross(orbitalPosition, orbitalVelocity);
                Vector3d worldPosition = OrbitalToWorld(orbitalPosition).normalized;
                Vector3d worldNormal = OrbitalToWorld(orbitalNormal).normalized;
                Vector3d worldTangent = Vector3d.Cross(worldNormal, worldPosition).normalized;
                if (IsFinite(worldPosition) && IsFinite(worldNormal) && IsFinite(worldTangent) &&
                    worldPosition.sqrMagnitude > 0 && worldNormal.sqrMagnitude > 0 &&
                    worldTangent.sqrMagnitude > 0)
                {
                    basisX = worldPosition;
                    basisY = worldTangent;
                    return true;
                }
            }

            Vector3d normal = body.transform == null
                ? Vector3d.up
                : ((Vector3d)body.transform.up).normalized;
            if (!IsFinite(normal) || normal.sqrMagnitude <= 0)
                normal = Vector3d.up;

            Vector3d reference = Math.Abs(Vector3d.Dot(normal, Vector3d.up)) < 0.9
                ? Vector3d.up
                : Vector3d.forward;
            basisX = Vector3d.Cross(normal, reference).normalized;
            basisY = Vector3d.Cross(normal, basisX).normalized;
            return IsFinite(basisX) && IsFinite(basisY) &&
                   basisX.sqrMagnitude > 0 && basisY.sqrMagnitude > 0;
        }

        private static Vector3d OrbitalToWorld(Vector3d orbital)
        {
            Vector3Value world = KspCoordinateFrames.OrbitalToWorld(
                new Vector3Value(orbital.x, orbital.y, orbital.z));
            return new Vector3d(world.X, world.Y, world.Z);
        }

        private static Vector3d ConePerpendicular(CelestialBody body, Vector3d direction)
        {
            Vector3d polarAxis = body == null
                ? Vector3d.up
                : (Vector3d)body.transform.up;
            Vector3d perpendicular = Vector3d.Cross(polarAxis, direction);
            if (perpendicular.sqrMagnitude < 1e-12)
                perpendicular = Vector3d.Cross(Vector3d.up, direction);
            if (perpendicular.sqrMagnitude < 1e-12)
                perpendicular = Vector3d.Cross(Vector3d.forward, direction);
            return perpendicular.normalized;
        }

        private static double[] BuildRingCoordinates(bool sine)
        {
            var values = new double[GuideRingSegments + 1];
            for (int index = 0; index <= GuideRingSegments; ++index)
            {
                double angle = index * Math.PI * 2.0 / GuideRingSegments;
                values[index] = sine ? Math.Sin(angle) : Math.Cos(angle);
            }

            return values;
        }

        private void DrawLine(ref int used, Vector3d start, Vector3d end, Color color)
        {
            DrawLine(ref used, start, end, color, LineWidth);
        }

        private void DrawLine(
            ref int used,
            Vector3d start,
            Vector3d end,
            Color color,
            float width)
        {
            if (used == linePool.Count)
                linePool.Add(new MapLineMesh("RTWBMapLine"));

            linePool[used].Update(start, end, color, width);
            ++used;
        }

        private void HideUnused(int used)
        {
            for (int index = used; index < linePool.Count; ++index)
                linePool[index].SetActive(false);
        }

        private static bool TryGeometry(
            RuntimeEndpoint endpoint,
            out Vector3d radial,
            out Vector3d relay,
            out Vector3d transitionPoint)
        {
            radial = Vector3d.zero;
            relay = Vector3d.zero;
            transitionPoint = Vector3d.zero;
            if (endpoint == null || endpoint.Vessel == null || endpoint.Vessel.orbit == null ||
                endpoint.Satellite == null || endpoint.Wormhole == null || endpoint.Wormhole.Body == null)
                return false;

            Vector3d orbitalRadial = endpoint.Vessel.orbit
                .getRelativePositionAtUT(Planetarium.GetUniversalTime())
                .normalized;
            Vector3Value worldRadial = KspCoordinateFrames.OrbitalToWorld(
                new Vector3Value(orbitalRadial.x, orbitalRadial.y, orbitalRadial.z));
            radial = new Vector3d(worldRadial.X, worldRadial.Y, worldRadial.Z);
            if (!IsFinite(radial) || radial.sqrMagnitude <= 0)
                return false;

            relay = endpoint.Satellite.Position;
            transitionPoint = endpoint.Wormhole.Body.position +
                              radial * endpoint.Wormhole.TransitionRadius;
            return IsFinite(relay) && IsFinite(transitionPoint);
        }

        private static bool IsEndpointSelected(RuntimeBridgeLink link, Vessel selected)
        {
            if (link == null || selected == null)
                return false;

            return (link.Source != null && link.Source.Vessel == selected) ||
                   (link.Target != null && link.Target.Vessel == selected);
        }

        private static Vessel SelectConeOwner(Vessel focused, Vessel routeVessel)
        {
            List<RuntimeEndpoint> endpoints = RemoteTechEndpointScanner.SnapshotAccepted();
            if (focused != null && endpoints.Exists(endpoint =>
                    endpoint != null && endpoint.Vessel == focused))
                return focused;

            if (routeVessel != null && endpoints.Exists(endpoint =>
                    endpoint != null && endpoint.Vessel == routeVessel))
                return routeVessel;

            return null;
        }

        private static RuntimeEndpoint SelectGuideEndpoint(Vessel focused, Vessel routeVessel)
        {
            IReadOnlyList<RuntimeEndpoint> guides = RemoteTechEndpointScanner.Guides;
            RuntimeEndpoint focusedGuide = FindGuideForVessel(guides, focused);
            if (focusedGuide != null)
                return focusedGuide;

            return FindGuideForVessel(guides, routeVessel);
        }

        private static RuntimeEndpoint FindGuideForVessel(
            IReadOnlyList<RuntimeEndpoint> guides,
            Vessel vessel)
        {
            if (guides == null || vessel == null)
                return null;

            for (int index = 0; index < guides.Count; ++index)
            {
                RuntimeEndpoint endpoint = guides[index];
                if (endpoint != null && endpoint.Vessel == vessel)
                    return endpoint;
            }

            return null;
        }

        private static bool IsInWormholePair(
            RuntimeEndpoint endpoint,
            RuntimeEndpoint ownerEndpoint,
            KexBodyInfo ownerPartner)
        {
            if (endpoint == null || endpoint.Wormhole == null || endpoint.Wormhole.Body == null ||
                ownerEndpoint == null || ownerEndpoint.Wormhole == null ||
                ownerEndpoint.Wormhole.Body == null || ownerPartner == null ||
                ownerPartner.Body == null)
                return false;

            return endpoint.Wormhole.Body == ownerEndpoint.Wormhole.Body ||
                   endpoint.Wormhole.Body == ownerPartner.Body;
        }

        private static string SelectedName(Vessel focused, Vessel routeVessel)
        {
            Vessel selected = focused ?? routeVessel;
            return selected == null ? "<none>" : selected.vesselName;
        }

        private static bool IsFinite(Vector3d value)
        {
            return !Double.IsNaN(value.x) && !Double.IsInfinity(value.x) &&
                   !Double.IsNaN(value.y) && !Double.IsInfinity(value.y) &&
                   !Double.IsNaN(value.z) && !Double.IsInfinity(value.z);
        }

        private static bool IsFinite(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        private void OnDestroy()
        {
            foreach (MapLineMesh line in linePool)
                line.Dispose();
            linePool.Clear();
            if (instance == this)
                instance = null;
        }

        private sealed class MapLineMesh
        {
            private readonly GameObject gameObject;
            private readonly Mesh mesh;
            private readonly MeshRenderer renderer;
            private readonly Vector3[] points = new Vector3[4];
            private readonly Color[] colors = new Color[4];

            internal MapLineMesh(string name)
            {
                gameObject = new GameObject(name);
                gameObject.layer = 31;
                MeshFilter filter = gameObject.AddComponent<MeshFilter>();
                renderer = gameObject.AddComponent<MeshRenderer>();
                mesh = new Mesh { name = name };
                filter.mesh = mesh;
                mesh.vertices = points;
                mesh.uv = new[]
                {
                    new Vector2(0, 1), new Vector2(0, 0),
                    new Vector2(1, 1), new Vector2(1, 0)
                };
                mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
                Material material = Resources.Load<Material>("Telemetry/TelemetryMaterial");
                if (material != null)
                    renderer.sharedMaterial = material;
                SetActive(false);
            }

            internal void Update(Vector3d localStart, Vector3d localEnd, Color color, float width)
            {
                Camera camera = PlanetariumCamera.Camera;
                if (camera == null)
                {
                    SetActive(false);
                    return;
                }

                Vector3 start = camera.WorldToScreenPoint(
                    (Vector3)ScaledSpace.LocalToScaledSpace(localStart));
                Vector3 end = camera.WorldToScreenPoint(
                    (Vector3)ScaledSpace.LocalToScaledSpace(localEnd));
                Vector3 offset = new Vector3(end.y - start.y, start.x - end.x, 0).normalized *
                                 (width / 2f);

                if (!MapView.Draw3DLines)
                {
                    if (start.z < 0)
                        start = FlipDirection(start, end);
                    else if (end.z < 0)
                        end = FlipDirection(end, start);

                    float depth = Screen.height / 2f + 0.01f;
                    start.z = start.z < 0.15f ? -depth : depth;
                    end.z = end.z < 0.15f ? -depth : depth;
                    points[0] = start - offset;
                    points[1] = start + offset;
                    points[2] = end - offset;
                    points[3] = end + offset;
                }
                else
                {
                    points[0] = camera.ScreenToWorldPoint(start - offset);
                    points[1] = camera.ScreenToWorldPoint(start + offset);
                    points[2] = camera.ScreenToWorldPoint(end - offset);
                    points[3] = camera.ScreenToWorldPoint(end + offset);
                }

                for (int index = 0; index < colors.Length; ++index)
                    colors[index] = color;
                mesh.vertices = points;
                mesh.colors = colors;
                mesh.RecalculateBounds();
                mesh.MarkDynamic();
                SetActive(true);
            }

            internal void SetActive(bool active)
            {
                renderer.enabled = active;
                gameObject.SetActive(active);
            }

            internal void Dispose()
            {
                UnityEngine.Object.Destroy(mesh);
                UnityEngine.Object.Destroy(gameObject);
            }

            private static Vector3 FlipDirection(Vector3 point, Vector3 pivot)
            {
                point -= pivot;
                point *= -1f;
                return point + pivot;
            }
        }
    }
}

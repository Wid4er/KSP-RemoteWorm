using System;

namespace RemoteTechWormholeBridge
{
    internal static class WormholeJumpDiagnostics
    {
        private const double IdentityToleranceDegrees = 0.01;

        internal static void Prefix(OrbitDriver __0, CelestialBody __1, out OrbitSnapshot __state)
        {
            __state = null;
            try
            {
                __state = OrbitSnapshot.Capture(__0);
                if (__state != null)
                    Log.Info(__state.Format("before") + " requestedTarget=" + BodyName(__1));
            }
            catch (Exception exception)
            {
                Log.Error("jump prefix telemetry failed: " + exception);
            }
        }

        internal static void Postfix(OrbitDriver __0, CelestialBody __1, OrbitSnapshot __state)
        {
            try
            {
                OrbitSnapshot after = OrbitSnapshot.Capture(__0);
                if (after == null)
                {
                    Log.Warning("jump postfix telemetry unavailable target=" + BodyName(__1));
                    return;
                }

                Log.Info(after.Format("after") + " requestedTarget=" + BodyName(__1));
                if (__state == null)
                    return;

                double orbitAngle = OrbitSnapshot.AngleDegrees(
                    __state.EvaluatedPosition, after.EvaluatedPosition);
                double worldAngle = OrbitSnapshot.AngleDegrees(
                    __state.WorldRelativePosition, after.WorldRelativePosition);
                bool elementsPreserved = ElementsPreserved(__state, after);

                Log.Info("jump-transform from=" + __state.BodyName + " to=" + after.BodyName +
                         " orbitRadialAngleDeg=" + OrbitSnapshot.Number(orbitAngle) +
                         " worldRadialAngleDeg=" + OrbitSnapshot.Number(worldAngle) +
                         " identityCandidate=" + (!Double.IsNaN(orbitAngle) &&
                                                   orbitAngle <= IdentityToleranceDegrees) +
                         " elementsPreserved=" + elementsPreserved +
                         " graphMutation=" + HarmonyBootstrap.IsNetworkPatched);
                DiagnosticController.RequestRefresh();
            }
            catch (Exception exception)
            {
                Log.Error("jump postfix telemetry failed: " + exception);
            }
        }

        internal static Exception Finalizer(Exception __exception, CelestialBody __1)
        {
            if (__exception != null)
                Log.Error("KEX MakeOrbit threw target=" + BodyName(__1) + " exception=" + __exception);
            return __exception;
        }

        private static bool ElementsPreserved(OrbitSnapshot before, OrbitSnapshot after)
        {
            return NearlyEqual(before.Inclination, after.Inclination) &&
                   NearlyEqual(before.Eccentricity, after.Eccentricity) &&
                   NearlyEqual(before.SemiMajorAxis, after.SemiMajorAxis) &&
                   NearlyEqual(before.LongitudeAscendingNode, after.LongitudeAscendingNode) &&
                   NearlyEqual(before.ArgumentOfPeriapsis, after.ArgumentOfPeriapsis) &&
                   NearlyEqual(before.MeanAnomalyAtEpoch, after.MeanAnomalyAtEpoch) &&
                   NearlyEqual(before.Epoch, after.Epoch);
        }

        private static bool NearlyEqual(double first, double second)
        {
            double scale = Math.Max(1.0, Math.Max(Math.Abs(first), Math.Abs(second)));
            return Math.Abs(first - second) <= 1e-9 * scale;
        }

        private static string BodyName(CelestialBody body)
        {
            return body == null ? "<null>" : body.name;
        }
    }
}

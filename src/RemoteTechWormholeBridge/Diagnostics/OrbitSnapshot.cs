using System;
using System.Globalization;

namespace RemoteTechWormholeBridge
{
    internal sealed class OrbitSnapshot
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        internal string VesselId;
        internal string BodyName;
        internal double UniversalTime;
        internal Vector3d OrbitPosition;
        internal Vector3d OrbitVelocity;
        internal Vector3d EvaluatedPosition;
        internal Vector3d EvaluatedVelocity;
        internal Vector3d WorldRelativePosition;
        internal double BodyRadius;
        internal double GravParameter;
        internal double Inclination;
        internal double Eccentricity;
        internal double SemiMajorAxis;
        internal double LongitudeAscendingNode;
        internal double ArgumentOfPeriapsis;
        internal double MeanAnomalyAtEpoch;
        internal double Epoch;
        internal double TrueAnomaly;

        internal static OrbitSnapshot Capture(OrbitDriver driver)
        {
            if (driver == null || driver.orbit == null)
                return null;

            Orbit orbit = driver.orbit;
            CelestialBody body = orbit.referenceBody;
            Vessel vessel = driver.vessel;
            double universalTime = Planetarium.GetUniversalTime();

            return new OrbitSnapshot
            {
                VesselId = vessel == null ? "<null>" : vessel.id.ToString("D"),
                BodyName = body == null ? "<null>" : body.name,
                UniversalTime = universalTime,
                OrbitPosition = orbit.pos,
                OrbitVelocity = orbit.vel,
                EvaluatedPosition = orbit.getRelativePositionAtUT(universalTime),
                EvaluatedVelocity = orbit.getOrbitalVelocityAtUT(universalTime),
                WorldRelativePosition = vessel == null || body == null
                    ? Vector3d.zero
                    : vessel.GetWorldPos3D() - body.position,
                BodyRadius = body == null ? Double.NaN : body.Radius,
                GravParameter = body == null ? Double.NaN : body.gravParameter,
                Inclination = orbit.inclination,
                Eccentricity = orbit.eccentricity,
                SemiMajorAxis = orbit.semiMajorAxis,
                LongitudeAscendingNode = orbit.LAN,
                ArgumentOfPeriapsis = orbit.argumentOfPeriapsis,
                MeanAnomalyAtEpoch = orbit.meanAnomalyAtEpoch,
                Epoch = orbit.epoch,
                TrueAnomaly = orbit.trueAnomaly
            };
        }

        internal string Format(string phase)
        {
            return "jump-snapshot phase=" + phase +
                   " vessel=" + VesselId +
                   " body=" + BodyName +
                   " ut=" + Number(UniversalTime) +
                   " bodyRadius=" + Number(BodyRadius) +
                   " mu=" + Number(GravParameter) +
                   " orbitPos=" + Vector(OrbitPosition) +
                   " evaluatedPos=" + Vector(EvaluatedPosition) +
                   " worldRelativePos=" + Vector(WorldRelativePosition) +
                   " orbitVel=" + Vector(OrbitVelocity) +
                   " evaluatedVel=" + Vector(EvaluatedVelocity) +
                   " inc=" + Number(Inclination) +
                   " ecc=" + Number(Eccentricity) +
                   " sma=" + Number(SemiMajorAxis) +
                   " lan=" + Number(LongitudeAscendingNode) +
                   " argPe=" + Number(ArgumentOfPeriapsis) +
                   " meanAtEpoch=" + Number(MeanAnomalyAtEpoch) +
                   " epoch=" + Number(Epoch) +
                   " trueAnomaly=" + Number(TrueAnomaly);
        }

        internal static double AngleDegrees(Vector3d first, Vector3d second)
        {
            double firstMagnitude = first.magnitude;
            double secondMagnitude = second.magnitude;
            if (firstMagnitude <= 0 || secondMagnitude <= 0)
                return Double.NaN;

            double cosine = Vector3d.Dot(first, second) / (firstMagnitude * secondMagnitude);
            cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
            return Math.Acos(cosine) * 180.0 / Math.PI;
        }

        internal static string Number(double value)
        {
            return value.ToString("R", Invariant);
        }

        private static string Vector(Vector3d value)
        {
            return "(" + Number(value.x) + "," + Number(value.y) + "," + Number(value.z) + ")";
        }
    }
}

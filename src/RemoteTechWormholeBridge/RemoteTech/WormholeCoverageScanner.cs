using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteTechWormholeBridge.Core.Geometry;

namespace RemoteTechWormholeBridge
{
    internal static class WormholeCoverageScanner
    {
        private const double TunnelEffectiveDistance = 1000.0;
        private static string lastState;

        internal static void Refresh(string reason, bool logUnchanged)
        {
            List<RuntimeEndpoint> endpoints = RemoteTechEndpointScanner.Accepted
                .OrderBy(endpoint => endpoint.Descriptor.Key)
                .ToList();
            var results = new List<PairResult>();
            var activeLinks = new List<RuntimeBridgeLink>();

            for (int first = 0; first < endpoints.Count; ++first)
            {
                for (int second = first + 1; second < endpoints.Count; ++second)
                {
                    RuntimeEndpoint a = endpoints[first];
                    RuntimeEndpoint b = endpoints[second];
                    if (a.Descriptor.Channel != b.Descriptor.Channel ||
                        !KexWormholeCatalog.ArePartners(a.Wormhole.Body, b.Wormhole.Body))
                        continue;

                    BidirectionalCoverage coverage = WormholeConeCoverage.EvaluateIdentityPair(
                        a.Radial,
                        a.Antenna.CosAngle,
                        b.Radial,
                        b.Antenna.CosAngle);
                    double effectiveDistance =
                        a.LocalDistance + TunnelEffectiveDistance + b.LocalDistance;
                    results.Add(new PairResult
                    {
                        A = a,
                        B = b,
                        Coverage = coverage,
                        EffectiveDistance = effectiveDistance
                    });
                    if (coverage.Active)
                    {
                        activeLinks.Add(new RuntimeBridgeLink
                        {
                            Source = a,
                            Target = b,
                            SourceGuid = a.Vessel.id,
                            TargetGuid = b.Vessel.id,
                            EffectiveDistance = effectiveDistance
                        });
                        activeLinks.Add(new RuntimeBridgeLink
                        {
                            Source = b,
                            Target = a,
                            SourceGuid = b.Vessel.id,
                            TargetGuid = a.Vessel.id,
                            EffectiveDistance = effectiveDistance
                        });
                    }
                }
            }

            WormholeNetworkIntegration.Replace(activeLinks);

            string currentState = BuildState(results);
            bool changed = !String.Equals(currentState, lastState, StringComparison.Ordinal);
            lastState = currentState;
            if (!logUnchanged && !changed)
                return;

            int active = results.Count(result => result.Coverage.Active);
            Log.Info("bridge-scan reason=" + reason + " eligiblePairs=" + results.Count +
                     " active=" + active + " transform=orbital-radial-identity graphMutation=" +
                     HarmonyBootstrap.IsNetworkPatched);

            foreach (PairResult result in results)
                LogResult(reason, result);
        }

        private static void LogResult(string reason, PairResult result)
        {
            RuntimeEndpoint a = result.A;
            RuntimeEndpoint b = result.B;
            DirectionalCoverage ab = result.Coverage.AToB;
            DirectionalCoverage ba = result.Coverage.BToA;
            Vector3Value entryA = WormholeConeCoverage.PointOnTransitionSurface(
                a.Radial, a.Wormhole.TransitionRadius);
            Vector3Value exitB = WormholeConeCoverage.PointOnTransitionSurface(
                a.Radial, b.Wormhole.TransitionRadius);
            double effectiveDistance = result.EffectiveDistance;

            Log.Info("bridge-coverage reason=" + reason +
                     " a=" + a.Descriptor.Key +
                     " b=" + b.Descriptor.Key +
                     " channel=" + a.Descriptor.Channel +
                     " errorABDeg=" + RemoteTechEndpointScanner.Format(ab.AngularErrorDegrees) +
                     " halfAngleADeg=" + RemoteTechEndpointScanner.Format(ab.HalfAngleDegrees) +
                     " validAB=" + ab.Covers +
                     " errorBADeg=" + RemoteTechEndpointScanner.Format(ba.AngularErrorDegrees) +
                     " halfAngleBDeg=" + RemoteTechEndpointScanner.Format(ba.HalfAngleDegrees) +
                     " validBA=" + ba.Covers +
                     " active=" + result.Coverage.Active +
                     " localDistanceA=" + RemoteTechEndpointScanner.Format(a.LocalDistance) +
                     " localDistanceB=" + RemoteTechEndpointScanner.Format(b.LocalDistance) +
                     " tunnelDistance=" + RemoteTechEndpointScanner.Format(TunnelEffectiveDistance) +
                     " effectiveDistance=" + RemoteTechEndpointScanner.Format(effectiveDistance) +
                     " radialA=" + FormatVector(a.Radial.Normalized()) +
                     " radialB=" + FormatVector(b.Radial.Normalized()) +
                     " entryPointA=" + FormatVector(entryA) +
                     " exitPointB=" + FormatVector(exitB));
        }

        private static string BuildState(IEnumerable<PairResult> results)
        {
            var builder = new StringBuilder();
            foreach (PairResult result in results)
            {
                builder.Append(result.A.Descriptor.Key).Append('|')
                    .Append(result.B.Descriptor.Key).Append('|')
                    .Append(result.A.Descriptor.Channel).Append('|')
                    .Append(result.Coverage.AToB.Covers).Append('|')
                    .Append(result.Coverage.BToA.Covers).Append(';');
            }

            return builder.ToString();
        }

        private static string FormatVector(Vector3Value vector)
        {
            return "(" + RemoteTechEndpointScanner.Format(vector.X) + "," +
                   RemoteTechEndpointScanner.Format(vector.Y) + "," +
                   RemoteTechEndpointScanner.Format(vector.Z) + ")";
        }

        private sealed class PairResult
        {
            internal RuntimeEndpoint A;
            internal RuntimeEndpoint B;
            internal BidirectionalCoverage Coverage;
            internal double EffectiveDistance;
        }
    }
}

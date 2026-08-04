using System;
using System.Linq;
using RemoteTechWormholeBridge.Core.Endpoints;
using RemoteTechWormholeBridge.Core.Geometry;
using RemoteTechWormholeBridge.Core.Routing;
using RemoteTechWormholeBridge.Core.Wormholes;

namespace RemoteTechWormholeBridge.Core.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                RegistersOneReciprocalPair();
                RejectsNonReciprocalPair();
                FiltersEndpointsAndChannels();
                PrioritizesOperationalStateWhenDishRangeIsZero();
                KeepsAntennasOnOneVesselDistinct();
                AppliesInclusiveOperationalDistanceBand();
                ConvertsOrbitalCoordinatesToKspWorldCoordinates();
                FindsAConcreteBridgeInsideASelectedVesselRoute();
                ComputesIdentityCoverageInBothDirections();
                RejectsCoverageOutsideEitherCone();
                Console.WriteLine("RTWB core tests passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void RegistersOneReciprocalPair()
        {
            var registry = new WormholeRegistry();
            registry.Refresh(new[]
            {
                Body("A", "B"),
                Body("B", "A")
            });

            Assert(registry.Pairs.Count == 1, "reciprocal pair must be deduplicated");
            Assert(registry.Issues.Count == 0, "valid pair must not report issues");
            Assert(Math.Abs(registry.Pairs[0].BodyA.SafetySurfaceRadius - 45000) < 0.001,
                "safety radius must include body radius");
        }

        private static void RejectsNonReciprocalPair()
        {
            var registry = new WormholeRegistry();
            registry.Refresh(new[]
            {
                Body("A", "B"),
                Body("B", "C"),
                Body("C", "B")
            });

            Assert(registry.Pairs.Count == 1, "only reciprocal B/C must be accepted");
            Assert(registry.Issues.Any(issue => issue.Subject == "A"), "A must report non-reciprocity");
        }

        private static void FiltersEndpointsAndChannels()
        {
            var accepted = Endpoint("v1", "a1", 2);
            var rejected = Endpoint("v2", "a2", 2);
            rejected.Powered = false;

            var registry = new EndpointRegistry();
            registry.Refresh(new[] { accepted, rejected });

            Assert(registry.Endpoints.Count == 1, "only valid endpoint must be registered");
            Assert(registry.Rejected[rejected.Key] == EndpointFailureReason.Unpowered,
                "power failure reason must be preserved");
            Assert(registry.ForBodyAndChannel("A", 2).Single() == accepted,
                "body/channel lookup must return the accepted endpoint");
        }

        private static void PrioritizesOperationalStateWhenDishRangeIsZero()
        {
            var unpowered = Endpoint("unpowered-vessel", "part", 0);
            unpowered.Powered = false;
            unpowered.IsDirectional = false;

            var inactive = Endpoint("inactive-vessel", "part", 0);
            inactive.Activated = false;
            inactive.Powered = false;
            inactive.IsDirectional = false;

            Assert(EndpointRegistry.Validate(unpowered) == EndpointFailureReason.Unpowered,
                "an unpowered dish with zero range must report its power state");
            Assert(EndpointRegistry.Validate(inactive) == EndpointFailureReason.Inactive,
                "an inactive dish with zero range must report its activation state first");
        }

        private static void KeepsAntennasOnOneVesselDistinct()
        {
            var first = Endpoint("shared-vessel", "part-101", 0);
            var second = Endpoint("shared-vessel", "part-202", 0);
            second.IsDirectional = false;

            var registry = new EndpointRegistry();
            registry.Refresh(new[] { first, second });

            Assert(first.Key != second.Key, "part identities must distinguish antennas on one vessel");
            Assert(registry.Endpoints.ContainsKey(first.Key), "valid antenna must remain accepted");
            Assert(registry.Rejected.ContainsKey(second.Key), "invalid sibling antenna must remain rejected");
        }

        private static void AppliesInclusiveOperationalDistanceBand()
        {
            Assert(!BridgeOperationalBand.Contains(99999.999),
                "a relay below 100 km must be too close");
            Assert(BridgeOperationalBand.Contains(100000),
                "the 100 km boundary must be eligible");
            Assert(BridgeOperationalBand.Contains(300000),
                "the 300 km boundary must be eligible");
            Assert(!BridgeOperationalBand.Contains(300000.001),
                "a relay above 300 km must be too far");
            Assert(!BridgeOperationalBand.Contains(Double.NaN),
                "an invalid distance must not be eligible");

            EndpointDescriptor tooClose = Endpoint("near", "part", 0);
            tooClose.LocalDistance = 99999;
            EndpointDescriptor tooFar = Endpoint("far", "part", 0);
            tooFar.LocalDistance = 300001;
            Assert(EndpointRegistry.Validate(tooClose) == EndpointFailureReason.TooCloseToWormhole,
                "the registry must report the lower-band failure");
            Assert(EndpointRegistry.Validate(tooFar) == EndpointFailureReason.TooFarFromWormhole,
                "the registry must report the upper-band failure");
        }

        private static void ConvertsOrbitalCoordinatesToKspWorldCoordinates()
        {
            Vector3Value world = KspCoordinateFrames.OrbitalToWorld(
                new Vector3Value(1, 2, 3));
            Assert(world.X == 1 && world.Y == 3 && world.Z == 2,
                "KSP world rendering must swap orbital Y and Z");
        }

        private static void FindsAConcreteBridgeInsideASelectedVesselRoute()
        {
            Guid selected = Guid.NewGuid();
            Guid relayA = Guid.NewGuid();
            Guid relayB = Guid.NewGuid();
            Guid commandStation = Guid.NewGuid();

            Assert(BridgeRouteVisibility.ContainsUndirectedEdge(
                    relayA,
                    relayB,
                    selected,
                    new[] { relayB, relayA, commandStation }),
                "a selected vessel route must reveal the bridge it traverses");
            Assert(BridgeRouteVisibility.ContainsUndirectedEdge(
                    relayA,
                    relayB,
                    selected,
                    new[] { relayA, relayB, commandStation }),
                "bridge visibility must work in both route directions");
            Assert(!BridgeRouteVisibility.ContainsUndirectedEdge(
                    relayA,
                    relayB,
                    selected,
                    new[] { relayA, commandStation }),
                "an unrelated selected vessel route must not reveal the bridge");
        }

        private static void ComputesIdentityCoverageInBothDirections()
        {
            double cosTenDegrees = Math.Cos(10 * Math.PI / 180.0);
            var result = WormholeConeCoverage.EvaluateIdentityPair(
                new Vector3Value(1, 0, 0),
                cosTenDegrees,
                new Vector3Value(Math.Cos(5 * Math.PI / 180.0), Math.Sin(5 * Math.PI / 180.0), 0),
                cosTenDegrees);

            Assert(result.Active, "a five-degree radial separation must fit both ten-degree cones");
            Assert(Math.Abs(result.AToB.AngularErrorDegrees - 5) < 0.000001,
                "coverage must report the radial angular error");

            Vector3Value exit = WormholeConeCoverage.PointOnTransitionSurface(
                new Vector3Value(2, 0, 0), 45000);
            Assert(Math.Abs(exit.X - 45000) < 0.001,
                "the exit point must use the normalized transformed radial");
        }

        private static void RejectsCoverageOutsideEitherCone()
        {
            var result = WormholeConeCoverage.EvaluateIdentityPair(
                new Vector3Value(1, 0, 0),
                Math.Cos(4 * Math.PI / 180.0),
                new Vector3Value(Math.Cos(5 * Math.PI / 180.0), Math.Sin(5 * Math.PI / 180.0), 0),
                Math.Cos(6 * Math.PI / 180.0));

            Assert(!result.AToB.Covers, "A must reject B outside A's half-angle");
            Assert(result.BToA.Covers, "B must cover A inside B's half-angle");
            Assert(!result.Active, "a bridge must require bidirectional coverage");
        }

        private static WormholeBodyDescriptor Body(string id, string partner)
        {
            return new WormholeBodyDescriptor(id, partner, 10000, 35000, 10, 30000);
        }

        private static EndpointDescriptor Endpoint(string vessel, string antenna, int channel)
        {
            return new EndpointDescriptor(vessel, antenna, "A", channel)
            {
                IsRemoteTechVessel = true,
                IsDirectional = true,
                Activated = true,
                Powered = true,
                TargetsLocalWormhole = true,
                BridgeCapabilityEnabled = true,
                IsInOperationalRegion = true,
                LocalDistance = 200000,
                HasLocalRange = true
            };
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}

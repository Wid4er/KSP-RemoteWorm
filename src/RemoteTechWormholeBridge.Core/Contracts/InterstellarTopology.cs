using System;
using System.Collections.Generic;
using System.Linq;

namespace RemoteTechWormholeBridge.Core.Contracts
{
    public sealed class WormholeTopologyEdge
    {
        public WormholeTopologyEdge(string pairId, string systemA, string systemB)
        {
            if (String.IsNullOrWhiteSpace(pairId))
                throw new ArgumentException("A pair identifier is required.", nameof(pairId));
            if (String.IsNullOrWhiteSpace(systemA))
                throw new ArgumentException("A system identifier is required.", nameof(systemA));
            if (String.IsNullOrWhiteSpace(systemB))
                throw new ArgumentException("A system identifier is required.", nameof(systemB));

            PairId = pairId;
            SystemA = systemA;
            SystemB = systemB;
        }

        public string PairId { get; private set; }
        public string SystemA { get; private set; }
        public string SystemB { get; private set; }
    }

    public sealed class InterstellarTopology
    {
        private readonly Dictionary<string, int> depths;
        private readonly Dictionary<string, int> tiers;

        private InterstellarTopology(Dictionary<string, int> depths, Dictionary<string, int> tiers)
        {
            this.depths = depths;
            this.tiers = tiers;
        }

        public IReadOnlyDictionary<string, int> Depths { get { return depths; } }
        public IReadOnlyDictionary<string, int> Tiers { get { return tiers; } }

        public static InterstellarTopology Build(
            string homeSystem,
            IEnumerable<WormholeTopologyEdge> sourceEdges)
        {
            if (String.IsNullOrWhiteSpace(homeSystem))
                throw new ArgumentException("A home system identifier is required.", nameof(homeSystem));

            List<WormholeTopologyEdge> edges = (sourceEdges ?? Enumerable.Empty<WormholeTopologyEdge>())
                .Where(edge => edge != null)
                .ToList();
            var adjacency = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (WormholeTopologyEdge edge in edges)
            {
                AddNeighbor(adjacency, edge.SystemA, edge.SystemB);
                AddNeighbor(adjacency, edge.SystemB, edge.SystemA);
            }

            var depths = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [homeSystem] = 0
            };
            var pending = new Queue<string>();
            pending.Enqueue(homeSystem);
            while (pending.Count != 0)
            {
                string current = pending.Dequeue();
                HashSet<string> neighbors;
                if (!adjacency.TryGetValue(current, out neighbors))
                    continue;

                foreach (string neighbor in neighbors.OrderBy(value => value, StringComparer.Ordinal))
                {
                    if (depths.ContainsKey(neighbor))
                        continue;
                    depths.Add(neighbor, depths[current] + 1);
                    pending.Enqueue(neighbor);
                }
            }

            var tiers = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (WormholeTopologyEdge edge in edges)
            {
                int depthA;
                int depthB;
                if (depths.TryGetValue(edge.SystemA, out depthA) &&
                    depths.TryGetValue(edge.SystemB, out depthB))
                    tiers[edge.PairId] = Math.Min(depthA, depthB) + 1;
            }

            return new InterstellarTopology(depths, tiers);
        }

        private static void AddNeighbor(
            IDictionary<string, HashSet<string>> adjacency,
            string source,
            string target)
        {
            HashSet<string> neighbors;
            if (!adjacency.TryGetValue(source, out neighbors))
            {
                neighbors = new HashSet<string>(StringComparer.Ordinal);
                adjacency.Add(source, neighbors);
            }
            neighbors.Add(target);
        }
    }
}

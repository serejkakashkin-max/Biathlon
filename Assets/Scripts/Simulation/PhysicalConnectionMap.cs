using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalSeamTransition
    {
        internal PhysicalConnectionMap Owner { get; }
        public string ConnectionId { get; }
        public int JunctionIndex { get; }
        public PhysicalTrackPoint Incoming { get; }
        public PhysicalTrackPoint Outgoing { get; }
        public long TravelNanometers { get; }
        internal PhysicalSeamTransition(PhysicalConnectionMap owner, string id, int junction, PhysicalTrackPoint incoming, PhysicalTrackPoint outgoing, long travel)
        { Owner = owner; ConnectionId = id; JunctionIndex = junction; Incoming = incoming; Outgoing = outgoing; TravelNanometers = travel; }
    }

    /// <summary>Validates an authored zero-distance seam. Does not grant admission or discard the incoming body footprint.</summary>
    public sealed class PhysicalConnectionMap
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalConnectionDefinition definition;
        private readonly PhysicalTrackPoint[] from, to;
        private readonly int[] junctionIndices;
        public PhysicalConnectionMap(PhysicalTrackMap map, PhysicalJunctionDefinition junctions, PhysicalConnectionDefinition definition)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (junctions == null || definition.TrackSnapshotJson != map.TrackSnapshotJson ||
                junctions.TrackSnapshotJson != map.TrackSnapshotJson || definition.JunctionSnapshotJson != junctions.ToJson())
                throw new ArgumentException("Connection mapping requires exact track and junction snapshots.");
            from = new PhysicalTrackPoint[definition.Count]; to = new PhysicalTrackPoint[definition.Count];
            junctionIndices = new int[definition.Count];
            for (int i = 0; i < definition.Count; i++)
            {
                var connection = definition.Get(i);
                for (int j = 0; j < junctions.Count; j++) if (junctions.Get(j).Id == connection.JunctionId)
                {
                    junctionIndices[i] = j; var junction = junctions.Get(j);
                    for (int p = 0; p < junction.PortCount; p++)
                    {
                        var port = junction.GetPort(p);
                        var point = map.MapLoop(port.Anchor.LoopId, checked(port.Anchor.Distance.Millimeters * SkiState.NanometersPerMillimeter));
                        if (port.Id == connection.FromPortId) from[i] = point;
                        if (port.Id == connection.ToPortId) to[i] = point;
                    }
                }
            }
        }
        public PhysicalSeamTransition Transition(string connectionId, PhysicalTrackPoint incoming, long totalTravelNanometers)
        {
            map.Validate(incoming);
            if (totalTravelNanometers < 0) throw new ArgumentOutOfRangeException(nameof(totalTravelNanometers));
            for (int i = 0; i < definition.Count; i++) if (definition.Get(i).Id == connectionId)
            {
                if (!map.SameLoop(incoming, from[i]) || incoming.LocalNanometers != from[i].LocalNanometers)
                    throw new ArgumentException("Transition requires the exact directed incoming anchor.");
                return new PhysicalSeamTransition(this, connectionId, junctionIndices[i], incoming, to[i], totalTravelNanometers);
            }
            throw new ArgumentException("Unknown directed connection.");
        }
        internal void Validate(PhysicalSeamTransition transition)
        { if (!ReferenceEquals(transition.Owner, this)) throw new ArgumentException("Foreign/default seam transition."); }

        internal int FindTransition(PhysicalTrackPoint incoming, PhysicalTrackPoint outgoing)
        {
            map.Validate(incoming); map.Validate(outgoing);
            for (int i = 0; i < from.Length; i++)
                if (map.SameLoop(incoming, from[i]) && incoming.LocalNanometers == from[i].LocalNanometers &&
                    map.SameLoop(outgoing, to[i]) && outgoing.LocalNanometers == to[i].LocalNanometers)
                    return i;
            return -1;
        }
    }
}

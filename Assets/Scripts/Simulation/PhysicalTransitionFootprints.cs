using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalTransitionFootprint
    {
        internal PhysicalTransitionFootprints Owner { get; }
        internal PhysicalSeamTransition Transition { get; }
        public PhysicalTrackPoint Current { get; }
        public long TravelSinceTransitionNanometers { get; }
        public long TotalTravelNanometers => checked(Transition.TravelNanometers + TravelSinceTransitionNanometers);
        public bool HasIncomingTail { get; }
        public long IncomingTailNanometers { get; }
        internal PhysicalTransitionFootprint(PhysicalTransitionFootprints owner, PhysicalSeamTransition transition,
            PhysicalTrackPoint current, long travel, long halfBody)
        {
            Owner = owner; Transition = transition; Current = current; TravelSinceTransitionNanometers = travel;
            HasIncomingTail = travel <= halfBody;
            IncomingTailNanometers = Math.Max(0, halfBody - travel);
        }
    }

    /// <summary>Pure, immutable whole-body junction queries during an already authorized seam
    /// transition. Does not grant admission, switch a session's position or release ownership.</summary>
    public sealed class PhysicalTransitionFootprints
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalConnectionMap connections;
        private readonly PhysicalJunctionMap junctions;
        private readonly long halfBody;
        public PhysicalTransitionFootprints(PhysicalTrackMap map, PhysicalJunctionDefinition junctions,
            PhysicalConnectionDefinition connections, OccupancyDefinition body)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            if (body == null) throw new ArgumentNullException(nameof(body));
            this.connections = new PhysicalConnectionMap(map, junctions, connections);
            this.junctions = new PhysicalJunctionMap(map, junctions);
            halfBody = checked((long)Math.Ceiling(body.LongitudinalSeparation * SkiState.NanometersPerMeter / 2));
        }
        public PhysicalTransitionFootprint Begin(string connectionId, PhysicalTrackPoint incoming, long totalTravelNanometers)
        {
            var transition = connections.Transition(connectionId, incoming, totalTravelNanometers);
            if (halfBody >= incoming.LoopLengthNanometers || halfBody >= transition.Outgoing.LoopLengthNanometers ||
                (!incoming.Closed && incoming.LocalNanometers < halfBody))
                throw new ArgumentException("Incoming tail must fit its authored path; shorter paths require multiple retained branches.");
            return new PhysicalTransitionFootprint(this, transition, transition.Outgoing, 0, halfBody);
        }
        public PhysicalTransitionFootprint Advance(PhysicalTransitionFootprint before, long travelNanometers)
        {
            Validate(before);
            if (travelNanometers < 0 || travelNanometers > before.Current.LoopLengthNanometers ||
                (!before.Current.Closed && travelNanometers > before.Current.LoopLengthNanometers - before.Current.LocalNanometers))
                throw new ArgumentOutOfRangeException(nameof(travelNanometers));
            long travelled = checked(before.TravelSinceTransitionNanometers + travelNanometers);
            _ = checked(before.Transition.TravelNanometers + travelled);
            long local = checked(before.Current.LocalNanometers + travelNanometers);
            if (before.Current.Closed && local >= before.Current.LoopLengthNanometers) local -= before.Current.LoopLengthNanometers;
            return new PhysicalTransitionFootprint(this, before.Transition,
                map.MapLoop(before.Current.LoopId, local), travelled, halfBody);
        }
        public bool TouchesJunction(int junctionIndex, PhysicalTransitionFootprint state)
        {
            Validate(state);
            return junctions.TouchesForwardSweep(junctionIndex, state.Current, 0, halfBody) || IncomingTouches(junctionIndex, state);
        }
        public bool TouchesJunctionDuringAdvance(int junctionIndex, PhysicalTransitionFootprint before, long travelNanometers)
        {
            // Validate the complete proposed advance even if the incoming footprint already touches.
            Advance(before, travelNanometers);
            return junctions.TouchesForwardSweep(junctionIndex, before.Current, travelNanometers, halfBody) || IncomingTouches(junctionIndex, before);
        }
        private bool IncomingTouches(int junctionIndex, PhysicalTransitionFootprint state)
        {
            if (!state.HasIncomingTail) return false;
            var anchor = state.Transition.Incoming;
            long local = anchor.LocalNanometers - state.IncomingTailNanometers;
            if (local < 0) local += anchor.LoopLengthNanometers;
            // Tail is [anchor - remaining length, anchor], not a full body centred on the old anchor.
            return junctions.TouchesForwardSweep(junctionIndex, map.MapLoop(anchor.LoopId, local), state.IncomingTailNanometers, 0);
        }
        private void Validate(PhysicalTransitionFootprint state)
        {
            if (!ReferenceEquals(state.Owner, this)) throw new ArgumentException("Foreign/default transition footprint.");
            connections.Validate(state.Transition);
        }
    }
}

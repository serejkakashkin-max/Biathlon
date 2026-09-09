using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ContactReason { None, Athlete, TrackEdge }
    public readonly struct ContactObservation
    {
        public AthleteId AthleteId { get; }
        public AthleteId BlockerId { get; }
        public ContactReason Reason { get; }
        public long RequestedDistanceNanometers { get; }
        public long ResolvedDistanceNanometers { get; }
        public bool Constrained => Reason != ContactReason.None;
        internal ContactObservation(AthleteId athlete, AthleteId blocker, ContactReason reason, long requested, long resolved)
        { AthleteId = athlete; BlockerId = blocker; Reason = reason; RequestedDistanceNanometers = requested; ResolvedDistanceNanometers = resolved; }
    }
    public sealed class SeparationResolver
    {
        private readonly OccupancySystem occupancy;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private readonly SkiPlacement[] starts, wishes, resolved;
        private readonly int[] order;
        private readonly bool[] processed, priorContact;
        private readonly AthleteId[] rosterIds;
        private readonly ContactObservation[] contacts;
        private int rosterCount = -1;
        public SimulationTick Tick { get; private set; }
        public int Count => Math.Max(0, rosterCount);
        public ContactObservation GetContact(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return contacts[index];
        }
        public SeparationResolver(OccupancySystem occupancy, int capacity, int tickFrequency, IRaceEventSink events)
        {
            this.occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            if (capacity <= 0 || tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            frequency = tickFrequency;
            starts = new SkiPlacement[capacity]; wishes = new SkiPlacement[capacity]; resolved = new SkiPlacement[capacity];
            order = new int[capacity]; processed = new bool[capacity]; priorContact = new bool[capacity];
            rosterIds = new AthleteId[capacity]; contacts = new ContactObservation[capacity];
        }
        public void Resolve(SimulationTick tick, ReadOnlySpan<SkiPlacement> before, ReadOnlySpan<SkiPlacement> proposals,
            ReadOnlySpan<double> minimumWidths, Span<SkiPlacement> output)
        {
            int count = before.Length;
            if (!tick.Equals(Tick.Next()) || count == 0 || count > starts.Length || proposals.Length != count || minimumWidths.Length != count || output.Length < count ||
                (rosterCount >= 0 && count != rosterCount)) throw new ArgumentException("Invalid separation frame shape/tick.");
            occupancy.ValidateRoster(before);
            for (int i = 0; i < count; i++)
            {
                if (!proposals[i].IsValid || !before[i].AthleteId.Equals(proposals[i].AthleteId) || before[i].CorridorId != proposals[i].CorridorId ||
                    proposals[i].Motion.DistanceNanometers < before[i].Motion.DistanceNanometers ||
                    !occupancy.FitsWidth(before[i].LateralMeters, minimumWidths[i]) ||
                    (rosterCount >= 0 && !rosterIds[i].Equals(before[i].AthleteId)))
                    throw new ArgumentException("Invalid separation identity, reverse motion, width or roster order.");
            }
            // Preserve all inputs before writing output, including when output aliases the caller's input array.
            before.CopyTo(starts); proposals.CopyTo(wishes); before.CopyTo(resolved);
            for (int i = 0; i < count; i++)
            {
                processed[i] = false; order[i] = i;
                int j = i;
                while (j > 0 && HasPriority(starts[order[j]], starts[order[j - 1]]))
                { int swap = order[j]; order[j] = order[j - 1]; order[j - 1] = swap; j--; }
            }
            for (int rank = 0; rank < count; rank++)
            {
                int index = order[rank]; var desired = wishes[index];
                bool edge = !occupancy.FitsWidth(desired.LateralMeters, minimumWidths[index]);
                if (edge)
                {
                    double available = minimumWidths[index] * 0.5 - occupancy.Definition.EdgeMargin;
                    double bound = Math.Floor(available * SkiState.NanometersPerMeter) / SkiState.NanometersPerMeter;
                    desired = new SkiPlacement(desired.AthleteId, desired.CorridorId, desired.Motion,
                        Math.Max(-bound, Math.Min(bound, desired.LateralMeters)));
                }
                var collision = FindCollision(index, desired, count);
                var accepted = desired;
                if (collision.Blocked)
                {
                    // Earlier trajectories have already been checked against this stationary start,
                    // hence zero displacement is always a safe lower bound. Keep only verified candidates.
                    double low = 0, high = 1; accepted = Interpolate(starts[index], desired, 0);
                    for (int iteration = 0; iteration < 64; iteration++)
                    {
                        double fraction = (low + high) * 0.5;
                        if (fraction == low || fraction == high) break;
                        var candidate = Interpolate(starts[index], desired, fraction);
                        if (FindCollision(index, candidate, count).Blocked) high = fraction;
                        else { low = fraction; accepted = candidate; }
                    }
                }
                resolved[index] = accepted; processed[index] = true;
                contacts[index] = new ContactObservation(starts[index].AthleteId, collision.BlockerId,
                    collision.Blocked ? ContactReason.Athlete : edge ? ContactReason.TrackEdge : ContactReason.None,
                    wishes[index].Motion.DistanceNanometers, accepted.Motion.DistanceNanometers);
            }
            // All validation/constraint work completed before committing caller output or replay episodes.
            for (int i = 0; i < count; i++) output[i] = resolved[i];
            rosterCount = count; Tick = tick;
            for (int i = 0; i < count; i++)
            {
                rosterIds[i] = starts[i].AthleteId;
                if (contacts[i].Constrained != priorContact[i])
                    events.Record(new RaceEvent(tick, frequency, rosterIds[i], contacts[i].Constrained ? RaceEventType.ContactStarted : RaceEventType.ContactEnded, (int)contacts[i].Reason));
                priorContact[i] = contacts[i].Constrained;
            }
        }
        private OccupancyQuery FindCollision(int index, SkiPlacement candidate, int count)
        {
            var best = new OccupancyQuery(false, false, default, 1);
            for (int other = 0; other < count; other++)
            {
                if (other == index) continue;
                var hit = occupancy.QueryRelativeMove(starts[index], candidate, starts[other], processed[other] ? resolved[other] : starts[other]);
                if (hit.Blocked && (!best.Blocked || hit.FirstContactFraction < best.FirstContactFraction ||
                    (hit.FirstContactFraction == best.FirstContactFraction && string.CompareOrdinal(hit.BlockerId.Value, best.BlockerId.Value) < 0))) best = hit;
            }
            return best;
        }
        private SkiPlacement Interpolate(SkiPlacement start, SkiPlacement desired, double fraction)
        {
            long delta = desired.Motion.DistanceNanometers - start.Motion.DistanceNanometers;
            // Decimal multiplication keeps checked Int64 nanometres valid even near the encoding maximum.
            long moved = (long)(delta * (decimal)fraction);
            double lateral = PhysiologyNumbers.Quantize(start.LateralMeters + (desired.LateralMeters - start.LateralMeters) * fraction);
            double speed = Math.Min(desired.Motion.SpeedMetersPerSecond, moved / (double)SkiState.NanometersPerMeter * frequency);
            return new SkiPlacement(start.AthleteId, start.CorridorId, new SkiState(checked(start.Motion.DistanceNanometers + moved), speed), lateral);
        }
        private static bool HasPriority(SkiPlacement a, SkiPlacement b) =>
            a.Motion.DistanceNanometers > b.Motion.DistanceNanometers ||
            (a.Motion.DistanceNanometers == b.Motion.DistanceNanometers && string.CompareOrdinal(a.AthleteId.Value, b.AthleteId.Value) < 0);
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct JunctionAdmissionPreview
    {
        internal JunctionAdmissionSession Owner { get; }
        internal long Revision { get; }
        public SimulationTick Tick { get; }
        public long StartFraction { get; }
        public long EndFraction { get; }
        public bool CompletesTick { get; }
        internal JunctionAdmissionPreview(JunctionAdmissionSession owner, long revision, SimulationTick tick,
            long startFraction, long endFraction, bool completesTick)
        { Owner = owner; Revision = revision; Tick = tick; StartFraction = startFraction; EndFraction = endFraction; CompletesTick = completesTick; }
    }

    /// <summary>Conservative exclusive admission over one common linear interval. Owns accepted
    /// physical positions; configured seams require explicit stationary boundary intervals. This is not a force integrator or an
    /// automatic collision resolver: a newly unsafe stationary suffix rejects the whole batch.</summary>
    public sealed class JunctionAdmissionSession
    {
        public const long FractionParts = 1000000000;
        private readonly PhysicalTrackMap map;
        private readonly PhysicalJunctionMap junctions;
        private readonly PhysicalOccupancySystem occupancy;
        private readonly PhysicalOccupancySweep[] states, wishes, accepted, nextStates;
        private readonly int[] owners, intervalOwners, nextOwners;
        private readonly bool[] seen, granted, lastGranted;
        private readonly long halfBody;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private long revision;
        private bool previewReady;
        private readonly object contextIdentity = new object();
        internal long AuthorityRevision { get; private set; }
        internal bool TickInProgress { get; private set; }
        private readonly PhysicalTransitionFootprints footprints;
        private readonly PhysicalConnectionDefinition connections;
        private readonly PhysicalTransitionFootprint[] tails, preparedTails, candidateTails;
        private readonly PhysicalOccupancySweep[] ghosts, preparedGhosts;
        private readonly int[] transitionIndices;
        public SimulationTick Tick { get; private set; }
        /// <summary>Covered fraction of Tick.Next(), without advancing the completed simulation clock.</summary>
        public long PendingFraction { get; private set; }
        public int Count => states.Length;
        public int JunctionCount => owners.Length;
        public PhysicalOccupancySweep GetState(int index) => states[index];
        public bool WasAdmitted(int index) => lastGranted[index];
        public AthleteId GetOwner(int junctionIndex) => owners[junctionIndex] < 0 ? default : states[owners[junctionIndex]].AthleteId;

        public JunctionAdmissionSession(PhysicalTrackMap map, PhysicalJunctionDefinition definition,
            OccupancyDefinition body, ReadOnlySpan<PhysicalOccupancySweep> initialStates, int tickFrequency, IRaceEventSink events,
            PhysicalConnectionDefinition connections = null)
        {
            this.map = map ?? throw new ArgumentNullException(nameof(map));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            if (tickFrequency <= 0 || initialStates.Length == 0) throw new ArgumentOutOfRangeException();
            frequency = tickFrequency;
            junctions = new PhysicalJunctionMap(map, definition);
            occupancy = new PhysicalOccupancySystem(map, body);
            this.connections = connections;
            if (connections != null) footprints = new PhysicalTransitionFootprints(map, definition, connections, body);
            halfBody = checked((long)Math.Ceiling(body.LongitudinalSeparation * SkiState.NanometersPerMeter / 2));
            if (occupancy.QueryBatch(initialStates).Blocked) throw new ArgumentException("Initial physical bodies overlap.");
            states = initialStates.ToArray();
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].TravelNanometers != 0 || states[i].LateralBeforeMeters != states[i].LateralAfterMeters)
                    throw new ArgumentException("Initial states must be stationary.");
                for (int j = i; j > 0 && string.CompareOrdinal(states[j].AthleteId.Value, states[j - 1].AthleteId.Value) < 0; j--)
                { var swap = states[j]; states[j] = states[j - 1]; states[j - 1] = swap; }
            }
            wishes = new PhysicalOccupancySweep[Count]; accepted = new PhysicalOccupancySweep[Count]; nextStates = new PhysicalOccupancySweep[Count];
            seen = new bool[Count]; granted = new bool[Count]; lastGranted = new bool[Count];
            tails = new PhysicalTransitionFootprint[Count]; preparedTails = new PhysicalTransitionFootprint[Count]; candidateTails = new PhysicalTransitionFootprint[Count];
            ghosts = new PhysicalOccupancySweep[Count]; preparedGhosts = new PhysicalOccupancySweep[Count]; transitionIndices = new int[Count];
            owners = new int[definition.Count]; intervalOwners = new int[definition.Count]; nextOwners = new int[definition.Count];
            for (int j = 0; j < owners.Length; j++)
            {
                owners[j] = -1;
                for (int i = 0; i < Count; i++) if (Touches(j, states[i]))
                {
                    if (owners[j] >= 0) throw new ArgumentException("Multiple initial occupants in an exclusive junction.");
                    owners[j] = i;
                }
            }
        }

        public void Step(SimulationTick tick, ReadOnlySpan<PhysicalOccupancySweep> proposals)
            => Commit(Prepare(tick, proposals));

        public JunctionAdmissionPreview Prepare(SimulationTick tick, ReadOnlySpan<PhysicalOccupancySweep> proposals)
            => Prepare(tick, proposals, ReadOnlySpan<string>.Empty);

        /// <summary>Optional connection IDs align with proposal rows. A seam change occupies the
        /// entire interval with zero travel/lateral change. Split moving intervals at seams.
        /// A second transition before the old tail clears is rejected, never overwritten.</summary>
        public JunctionAdmissionPreview Prepare(SimulationTick tick, ReadOnlySpan<PhysicalOccupancySweep> proposals,
            ReadOnlySpan<string> connectionIds)
            => PrepareCore(tick, 0, FractionParts, true, proposals, connectionIds, false);

        /// <summary>Contiguous exact fractional coverage of one simulation tick. Seam changes take
        /// zero time; all athletes must be stationary in a zero-duration boundary batch.
        /// CompletesTick seals only at FractionParts. Caller supplies force-derived trajectories.</summary>
        public JunctionAdmissionPreview PrepareSubstep(SimulationTick tick, long startFraction, long endFraction,
            bool completesTick, ReadOnlySpan<PhysicalOccupancySweep> proposals, ReadOnlySpan<string> connectionIds)
            => PrepareCore(tick, startFraction, endFraction, completesTick, proposals, connectionIds, true);

        private JunctionAdmissionPreview PrepareCore(SimulationTick tick, long startFraction, long endFraction,
            bool completesTick, ReadOnlySpan<PhysicalOccupancySweep> proposals, ReadOnlySpan<string> connectionIds, bool exactTiming)
        {
            // Even an unsuccessful replacement invalidates the previous prepared scratch buffers.
            previewReady = false;
            if (!tick.Equals(Tick.Next()) || proposals.Length != Count) throw new ArgumentException("Invalid admission tick/roster.");
            if (startFraction != PendingFraction || endFraction < startFraction || endFraction > FractionParts ||
                (completesTick && endFraction != FractionParts)) throw new ArgumentException("Substeps must cover the tick contiguously and seal only at its end.");
            if (connectionIds.Length != 0 && connectionIds.Length != Count) throw new ArgumentException("Connection rows must align with proposals.");
            Array.Clear(seen, 0, Count);
            for (int p = 0; p < Count; p++)
            {
                var wish = proposals[p]; map.Validate(wish.Start);
                if (startFraction == endFraction && (wish.TravelNanometers != 0 || wish.LateralBeforeMeters != wish.LateralAfterMeters))
                    throw new ArgumentException("A zero-time boundary cannot move an athlete.");
                int i = Index(wish.AthleteId);
                if (i < 0 || seen[i]) throw new ArgumentException("Foreign/duplicate admission athlete.");
                var prior = states[i];
                if (!map.SameLoop(prior.Start, wish.Start) || prior.Start.LocalNanometers != wish.Start.LocalNanometers ||
                    prior.LateralAfterMeters != wish.LateralBeforeMeters)
                    throw new ArgumentException("Admission must continue the owned position; branch transitions require a separate protocol.");
                seen[i] = true; wishes[i] = wish;
                transitionIndices[i] = -1; candidateTails[i] = tails[i]; preparedGhosts[i] = ghosts[i];
                string connection = connectionIds.Length == 0 ? null : connectionIds[p];
                if (connection != null)
                {
                    if (exactTiming && startFraction != endFraction) throw new ArgumentException("Seam changes require zero-duration boundaries.");
                    if (footprints == null || tails[i].HasIncomingTail || wish.TravelNanometers != 0 || wish.LateralAfterMeters != wish.LateralBeforeMeters)
                        throw new ArgumentException("Seam changes require configured connectivity, a cleared previous tail and a stationary boundary interval.");
                    candidateTails[i] = footprints.Begin(connection, prior.Start, 0);
                    for (int c = 0; c < connections.Count; c++) if (connections.Get(c).Id == connection) transitionIndices[i] = c;
                    preparedGhosts[i] = prior;
                    wishes[i] = new PhysicalOccupancySweep(wish.AthleteId, candidateTails[i].Current, 0, wish.LateralBeforeMeters, wish.LateralAfterMeters);
                }
            }
            // Existing occupants reserve the whole interval, even if their proposed endpoint is clear.
            Array.Copy(owners, intervalOwners, owners.Length);
            for (int i = 0; i < Count; i++)
            {
                bool allow = true;
                for (int j = 0; j < owners.Length; j++)
                    if (intervalOwners[j] >= 0 && intervalOwners[j] != i && CandidateTouches(j, i)) allow = false;
                granted[i] = allow;
                accepted[i] = allow ? wishes[i] : states[i];
                if (allow)
                    for (int j = 0; j < owners.Length; j++) if (CandidateTouches(j, i)) intervalOwners[j] = i;
                nextStates[i] = Endpoint(accepted[i]);
                preparedTails[i] = allow ? candidateTails[i] : tails[i];
                if (!allow) preparedGhosts[i] = ghosts[i];
                if (preparedTails[i].HasIncomingTail)
                    preparedTails[i] = footprints.Advance(preparedTails[i], accepted[i].TravelNanometers);
            }
            // A rejected entrant stops. That suffix may endanger a following athlete; never commit it unchecked.
            if (occupancy.QueryBatch(accepted).Blocked)
                throw new InvalidOperationException("Admission creates a body conflict. Replan the complete common interval and retry.");
            // Keep a conservative stationary incoming body during any interval with an old tail.
            // This also protects portions outside authored junction guards. Never compare it to its own athlete.
            for (int i = 0; i < Count; i++)
            {
                bool retained = granted[i] ? candidateTails[i].HasIncomingTail : tails[i].HasIncomingTail;
                if (!retained) continue;
                for (int k = 0; k < Count; k++) if (k != i && occupancy.QueryRelativeMove(preparedGhosts[i], accepted[k]).Blocked)
                    throw new InvalidOperationException("Incoming transition body conflicts with the accepted field.");
                for (int k = 0; k < i; k++)
                    if ((granted[k] ? candidateTails[k].HasIncomingTail : tails[k].HasIncomingTail) &&
                        occupancy.QueryRelativeMove(preparedGhosts[i], preparedGhosts[k]).Blocked)
                        throw new InvalidOperationException("Retained incoming transition bodies conflict.");
            }
            for (int j = 0; j < owners.Length; j++)
            {
                int owner = intervalOwners[j];
                nextOwners[j] = owner >= 0 && (Touches(j, nextStates[owner]) ||
                    (preparedTails[owner].HasIncomingTail && footprints.TouchesJunction(j, preparedTails[owner]))) ? owner : -1;
            }
            revision = checked(revision + 1);
            previewReady = true;
            return new JunctionAdmissionPreview(this, revision, tick, startFraction, endFraction, completesTick);
        }

        public void ValidatePreview(JunctionAdmissionPreview preview)
        {
            if (!ReferenceEquals(preview.Owner, this) || !previewReady || preview.Revision != revision || !preview.Tick.Equals(Tick.Next()) || preview.StartFraction != PendingFraction)
                throw new ArgumentException("Foreign, stale or consumed admission preview.");
        }

        public PhysicalOccupancySweep GetPreparedSweep(JunctionAdmissionPreview preview, int index)
        { ValidatePreview(preview); return accepted[index]; }
        public PhysicalOccupancySweep GetPreparedState(JunctionAdmissionPreview preview, int index)
        { ValidatePreview(preview); return nextStates[index]; }
        public bool IsPreparedAdmissionGranted(JunctionAdmissionPreview preview, int index)
        { ValidatePreview(preview); return granted[index]; }
        public AthleteId GetPreparedOwner(JunctionAdmissionPreview preview, int junctionIndex)
        { ValidatePreview(preview); int index = nextOwners[junctionIndex]; return index < 0 ? default : states[index].AthleteId; }

        public void Commit(JunctionAdmissionPreview preview)
        {
            ValidatePreview(preview);
            long nextAuthorityRevision = checked(AuthorityRevision + 1);
            previewReady = false;
            AuthorityRevision = nextAuthorityRevision;
            var tick = preview.Tick;
            // All validation precedes authoritative state, clock and event changes.
            Array.Copy(nextStates, states, Count); Array.Copy(granted, lastGranted, Count);
            PendingFraction = preview.CompletesTick ? 0 : preview.EndFraction;
            TickInProgress = !preview.CompletesTick;
            if (preview.CompletesTick) Tick = tick;
            Array.Copy(preparedTails, tails, Count); Array.Copy(preparedGhosts, ghosts, Count);
            for (int j = 0; j < owners.Length; j++)
            {
                if (owners[j] < 0 && intervalOwners[j] >= 0)
                    events.Record(new RaceEvent(tick, frequency, states[intervalOwners[j]].AthleteId, RaceEventType.JunctionReserved, j));
                if (intervalOwners[j] >= 0 && nextOwners[j] < 0)
                    events.Record(new RaceEvent(tick, frequency, states[intervalOwners[j]].AthleteId, RaceEventType.JunctionReleased, j));
                owners[j] = nextOwners[j];
            }
            for (int i = 0; i < Count; i++) if (granted[i] && transitionIndices[i] >= 0)
                events.Record(new RaceEvent(tick, frequency, states[i].AthleteId, RaceEventType.PhysicalSeamCrossed, transitionIndices[i]));
        }

        private bool CandidateTouches(int junction, int index) => Touches(junction, wishes[index]) ||
            (transitionIndices[index] >= 0 && Touches(junction, states[index])) ||
            (candidateTails[index].HasIncomingTail && footprints.TouchesJunctionDuringAdvance(junction, candidateTails[index], wishes[index].TravelNanometers));

        // Cold planning copy shares immutable geometry only; every mutable buffer is independent.
        private JunctionAdmissionSession(JunctionAdmissionSession source, IRaceEventSink sink)
        {
            contextIdentity = source.contextIdentity; map = source.map; junctions = source.junctions;
            occupancy = source.occupancy; footprints = source.footprints; connections = source.connections;
            halfBody = source.halfBody; frequency = source.frequency; events = sink;
            int count = source.Count, junctionCount = source.JunctionCount;
            states = new PhysicalOccupancySweep[count]; wishes = new PhysicalOccupancySweep[count];
            accepted = new PhysicalOccupancySweep[count]; nextStates = new PhysicalOccupancySweep[count];
            owners = new int[junctionCount]; intervalOwners = new int[junctionCount]; nextOwners = new int[junctionCount];
            seen = new bool[count]; granted = new bool[count]; lastGranted = new bool[count];
            tails = new PhysicalTransitionFootprint[count]; preparedTails = new PhysicalTransitionFootprint[count]; candidateTails = new PhysicalTransitionFootprint[count];
            ghosts = new PhysicalOccupancySweep[count]; preparedGhosts = new PhysicalOccupancySweep[count]; transitionIndices = new int[count];
            CopyAuthorityFrom(source);
        }
        internal JunctionAdmissionSession CreatePlanningCopy(IRaceEventSink sink) => new JunctionAdmissionSession(this, sink);
        internal void CopyAuthorityFrom(JunctionAdmissionSession source)
        {
            if (!ReferenceEquals(contextIdentity, source.contextIdentity)) throw new ArgumentException("Foreign admission context.");
            long nextRevision = checked(AuthorityRevision + 1);
            Array.Copy(source.states, states, Count); Array.Copy(source.lastGranted, lastGranted, Count);
            Array.Copy(source.tails, tails, Count); Array.Copy(source.ghosts, ghosts, Count);
            Array.Copy(source.owners, owners, JunctionCount);
            Tick = source.Tick; PendingFraction = source.PendingFraction;
            TickInProgress = source.TickInProgress;
            previewReady = false; AuthorityRevision = nextRevision;
        }
        internal void PublishEvents(ReadOnlySpan<RaceEvent> buffered)
        { for (int i = 0; i < buffered.Length; i++) events.Record(buffered[i]); }

        private bool Touches(int junction, PhysicalOccupancySweep sweep) =>
            junctions.TouchesForwardSweep(junction, sweep.Start, sweep.TravelNanometers, halfBody);
        private int Index(AthleteId id)
        { for (int i = 0; i < Count; i++) if (states[i].AthleteId.Equals(id)) return i; return -1; }
        private PhysicalOccupancySweep Endpoint(PhysicalOccupancySweep sweep)
        {
            long local = checked(sweep.Start.LocalNanometers + sweep.TravelNanometers);
            if (sweep.Start.Closed && local >= sweep.Start.LoopLengthNanometers) local -= sweep.Start.LoopLengthNanometers;
            return new PhysicalOccupancySweep(sweep.AthleteId, map.MapLoop(sweep.Start.LoopId, local), 0, sweep.LateralAfterMeters, sweep.LateralAfterMeters);
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Shared physical Sprint/Pursuit field. Physics/physiology are prepared before continuous
    /// pack validation; accepted forward prefixes, stops and lateral motion commit together.
    /// Prestart staging and classified athletes are outside the racing corridor.</summary>
    public sealed class PackSprintFieldSession : ISprintFieldView
    {
        private struct Tail { public bool Active; public PhysicalTrackPoint Point; public double Lateral; public long BirthTravel; }
        private sealed class Trace
        {
            internal readonly PhysicalMotionReceipt[] Segments;
            internal int Count;
            internal PhysicalTrackPoint Start, End;
            internal double LateralStart, LateralEnd;
            internal Tail OldTail, NewTail, EndTail;
            internal int SeamIndex;
            internal Trace(int capacity) { Segments = new PhysicalMotionReceipt[capacity]; }
        }
        private readonly SprintFieldSession field;
        private readonly PhysicalTrackMap map;
        private readonly PhysicalMotionProjection projection;
        private readonly PhysicalRangeLaneMap[] rangeMaps;
        private readonly PhysicalMotionProjection[] rangeProjections;
        private readonly PhysicalStartApproachMap startOverlay;
        public MassStartGrid StartGrid { get; }
        public StartApproachMap StartApproach => startOverlay?.Approach;
        private readonly PhysicalJunctionMap junctions;
        private readonly PhysicalConnectionMap connections;
        private readonly PhysicalConnectionDefinition connectionDefinition;
        private readonly OccupancySystem widths;
        private readonly PhysicalOccupancySystem occupancy;
        private readonly DraftingSystem drafting;
        private readonly TrackSampler penaltySampler;
        private readonly OvertakeDefinition movement;
        private readonly IRaceEventSink events;
        private readonly Trace[] traces, stationary;
        private readonly SprintStepPreview[] previews;
        private readonly Tail[] tails;
        private readonly bool[] onCourse, active, processed, priorContact, constrained;
        private readonly double[] lateral, desiredLateral, drag, propulsion;
        private readonly int[] extraEffort, order;
        private readonly DraftOutput[] drafts, committedDrafts;
        private readonly SkiPlacement[] draftRoster;
        private readonly long[] grid;
        private readonly long[] junctionWaitSince, preparedJunctionWaitSince;
        private readonly RaceMotionSegment[] allSegments;
        private readonly double[] timeScratch;
        private readonly long halfBody;
        private readonly double lateralStep, minimumWidth;
        private readonly int junctionCount, frequency;
        public SimulationTick Tick => field.Tick;
        public int TickFrequency => frequency;
        public int Count => field.Count;
        public bool IsComplete => field.IsComplete;
        public SprintResultSnapshot Result => field.Result;
        public PursuitResultSnapshot PursuitResult => field.PursuitResult;
        public MassStartResultSnapshot MassStartResult => field.MassStartResult;
        public TrackDefinition Track => field.Track;
        public RaceRoute Route => field.Route;
        public CompetitionProfile Profile => field.Profile;
        public CheckpointTimingBoard Timing => field.Timing;
        public long PrecisionFallbackTicks { get; private set; }
        public string ContextJson { get; }
        public SprintAthleteSession GetAthlete(int index) => field.GetAthlete(index);
        public bool IsOnCourse(int index) => onCourse[index] && !GetAthlete(index).HasFinished;
        public double GetLateral(int index) => lateral[index];
        /// <summary>First unserved junction arrival tick; zero means no pending ticket.</summary>
        public long GetJunctionWaitSince(int index) => junctionWaitSince[index];
        public DraftOutput GetDraft(int index) => committedDrafts[index];
        public PhysicalTrackPoint GetPhysicalPoint(int index) => CurrentPoint(index);

        public PackSprintFieldSession(SimulationConfig config, CompetitionProfile profile, SkiModelDefinitions skiModel,
            ShootingModelDefinitions shootingModel, TrackDefinition track, RaceRoute route, RaceSnowConditions conditions,
            ResultTimingDefinition timing, ReadOnlySpan<SprintEntrant> entrants, SimulationTick firstStart, string resultId,
            OccupancyDefinition body, DraftDefinition draft, OvertakeDefinition movement,
            PhysicalJunctionDefinition junctionDefinition, PhysicalConnectionDefinition connectionDefinition, IRaceEventSink events,
            PhysicalRangeLaneDefinition rangeLanes = null, PursuitQualificationSnapshot qualification = null,
            MassStartGrid massGrid = null, StartApproachMap startApproach = null)
        {
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.movement = movement ?? throw new ArgumentNullException(nameof(movement));
            field = new SprintFieldSession(config, profile, skiModel, shootingModel, track, route, conditions, timing, entrants, firstStart, resultId, events, qualification,massGrid,startApproach);
            frequency = config.TickFrequency; map = new PhysicalTrackMap(track, route);
            StartGrid=massGrid;
            if(massGrid!=null)
            {
                if(massGrid.BodyJson!=body.ToJson())throw new ArgumentException("Grid and field bodies must match.");
                startOverlay=new PhysicalStartApproachMap(map,track,route,startApproach);
            }
            for (int i = 0; i < route.SectionCount; i++)
            {
                var section = route.GetSection(i);
                if (!track.GetLoop(section.LoopId).Closed && (i > 0 || i < route.SectionCount - 1))
                    throw new ArgumentException("Pack multi-section routes require closed physical loops; open endpoints need explicit routed connectors.");
            }
            projection = new PhysicalMotionProjection(map, 1.0 / frequency);
            junctions = new PhysicalJunctionMap(map, junctionDefinition); junctionCount = junctionDefinition.Count;
            this.connectionDefinition = connectionDefinition; connections = new PhysicalConnectionMap(map, junctionDefinition, connectionDefinition);
            if (rangeLanes != null)
            {
                if (Count > rangeLanes.Count) throw new ArgumentException("Unique physical range lanes required for the entire roster.");
                rangeMaps = new PhysicalRangeLaneMap[Count]; rangeProjections = new PhysicalMotionProjection[Count];
                for (int i = 0; i < Count; i++)
                {
                    rangeMaps[i] = new PhysicalRangeLaneMap(map, route, rangeLanes, i);
                    rangeProjections[i] = new PhysicalMotionProjection(map, 1.0 / frequency, rangeMaps[i],startApproach,
                        massGrid==null?0:-massGrid.Get(i).LongitudinalNanometers,startOverlay);
                    var start = map.MapLoop(rangeLanes.GetLane(i), 0);
                    var end = map.MapLoop(rangeLanes.GetLane(i), (rangeLanes.Exit.Millimeters - rangeLanes.Entry.Millimeters) * SkiState.NanometersPerMillimeter);
                    if (ConnectionIndex(map.MapLoop(rangeLanes.MainLoopId, rangeLanes.Entry.Millimeters * SkiState.NanometersPerMillimeter), start) < 0 ||
                        ConnectionIndex(end, map.MapLoop(rangeLanes.MainLoopId, rangeLanes.Exit.Millimeters * SkiState.NanometersPerMillimeter)) < 0 ||
                        ConnectionIndex(end, map.MapPenalty(0)) < 0)
                        throw new ArgumentException("Range lane requires explicit entry, main exit and penalty exit connections.");
                }
            }
            else if(massGrid!=null)
            {
                rangeProjections=new PhysicalMotionProjection[Count];
                for(int i=0;i<Count;i++)rangeProjections[i]=new PhysicalMotionProjection(map,1.0/frequency,null,startApproach,-massGrid.Get(i).LongitudinalNanometers,startOverlay);
            }
            widths = new OccupancySystem(body); occupancy = new PhysicalOccupancySystem(map, body); drafting = new DraftingSystem(draft);
            penaltySampler = new TrackSampler(track.GetLoop(track.Penalty.LoopId));
            halfBody = (long)Math.Ceiling(body.LongitudinalSeparation * SkiState.NanometersPerMeter / 2);
            lateralStep = movement.LateralSpeed / frequency; minimumWidth = double.MaxValue;
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i);
                for (int j = 0; j < loop.SegmentCount; j++) minimumWidth = Math.Min(minimumWidth, loop.GetSegment(j).WidthMeters);
            }
            if (!widths.FitsWidth(0, minimumWidth)) throw new ArgumentException("Race paths must contain the body.");
            traces = new Trace[Count]; stationary = new Trace[Count]; previews = new SprintStepPreview[Count]; tails = new Tail[Count];
            onCourse = new bool[Count]; active = new bool[Count]; processed = new bool[Count]; priorContact = new bool[Count]; constrained = new bool[Count];
            lateral = new double[Count]; desiredLateral = new double[Count]; drag = new double[Count]; propulsion = new double[Count];
            extraEffort = new int[Count]; order = new int[Count]; drafts = new DraftOutput[Count]; committedDrafts = new DraftOutput[Count]; draftRoster = new SkiPlacement[Count];
            junctionWaitSince = new long[Count]; preparedJunctionWaitSince = new long[Count];
            int capacity = 0;
            for (int i = 0; i < Count; i++) { traces[i] = new Trace(GetAthlete(i).MotionSegmentCapacity); stationary[i] = new Trace(0); capacity += traces[i].Segments.Length; }
            allSegments = new RaceMotionSegment[capacity]; grid = new long[2 + capacity * 2]; timeScratch = new double[grid.Length];
            if(massGrid!=null)
            {
                for(int i=0;i<Count;i++)
                {
                    lateral[i]=massGrid.Get(i).LateralNanometers/1e9;onCourse[i]=true;
                    double startWidth=track.GetLoop(startApproach.LoopId).GetSegment(0).WidthMeters;
                    if(!widths.FitsWidth(lateral[i],startWidth))throw new ArgumentException("Grid does not fit actual start corridor.");
                    var point=CurrentPoint(i);var idle=stationary[i];idle.Start=idle.End=point;idle.LateralStart=idle.LateralEnd=lateral[i];
                    for(int j=0;j<i;j++)if(Conflict(idle,i,stationary[j],j))throw new ArgumentException("Grid conflicts with physical corridor/junctions.");
                }
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] { "sprint_shared_pack_prefix_v1", field.ContextJson,
                body.ToJson(), draft.ToJson(), movement.ToJson(), junctionDefinition.ToJson(), connectionDefinition.ToJson(),
                "tick_start_periodic_draft;fixed_tick_lateral;prefix_stop_mm_quantum;full_interval_junction;seam_tail_lateral_lock;staging_before_admission;finish_exit;fraction_nearest_even_1e9;precision_keep_first_mover;contact_from_replan;junction_fifo_after_incumbent_v1" });
            if (rangeLanes != null) ContextJson = ShootingModelDefinitions.EncodeContext(new[] { ContextJson, rangeLanes.ToJson(), "physical_range_lanes_v1;unique_roster_slot_lane;last_lap_main_bypass" });
            if(startOverlay!=null)ContextJson=ShootingModelDefinitions.EncodeContext(new[]{ContextJson,startOverlay.ContextJson,"grid_bodies_present_before_gun_v1"});
        }

        public void Step(ReadOnlySpan<SprintAthleteInput> inputs, ReadOnlySpan<double> requestedLateral)
        {
            field.ValidateCommands(inputs);
            if (requestedLateral.Length != Count) throw new ArgumentException("Complete lateral command batch required.");
            for (int i = 0; i < Count; i++) PhysiologyNumbers.RequireRange(requestedLateral[i], -1000000, 1000000, nameof(requestedLateral));
            var tick = Tick.Next();
            // Snapshot all physical starts before preparing any athlete.
            for (int i = 0; i < Count; i++)
            {
                var athlete = GetAthlete(i); var start = CurrentPoint(i); var idle = stationary[i];
                idle.Start = idle.End = start; idle.LateralStart = idle.LateralEnd = lateral[i]; idle.Count = 0;
                idle.OldTail = tails[i]; idle.NewTail = default; idle.EndTail = tails[i];
                active[i] = onCourse[i] && !athlete.HasFinished; processed[i] = false; constrained[i] = false; order[i] = i;
            }
            // Official start clock is unchanged; if the line is occupied the athlete waits in staging.
            for (int i = 0; i < Count; i++) if (!active[i] && !GetAthlete(i).HasFinished && tick.Value > GetAthlete(i).ScheduledStart.Value)
            {
                bool clear = true;
                for (int j = 0; j < Count; j++) if (j != i && active[j] && Conflict(stationary[i], i, stationary[j], j)) clear = false;
                active[i] = clear;
            }
            for (int i = 0; i < Count; i++)
            {
                var athlete = GetAthlete(i); if (athlete.HasFinished) continue;
                bool movable = active[i] && !(athlete.CurrentRangeMotion.HasValue && athlete.CurrentRangeMotion.Value.Phase == RangeTraversalPhase.Shooting);
                if(StartGrid!=null && tick.Value<=athlete.ScheduledStart.Value)movable=false;
                double bound = Math.Floor((minimumWidth / 2 - widths.Definition.EdgeMargin) * 1e9) / 1e9;
                double target = Math.Max(-bound, Math.Min(bound, requestedLateral[i]));
                desiredLateral[i] = movable ? PhysiologyNumbers.Quantize(lateral[i] + Math.Max(-lateralStep, Math.Min(lateralStep, target - lateral[i]))) : lateral[i];
                double fraction = Math.Min(1, Math.Abs(desiredLateral[i] - lateral[i]) / lateralStep);
                extraEffort[i] = (int)Math.Ceiling(movement.LateralEffortSurcharge * fraction);
                propulsion[i] = 1 - movement.LateralPropulsionLoss * fraction;
                drafts[i] = active[i] ? Draft(i, inputs[i]) : default; drag[i] = drafts[i].DragAreaMultiplier;
                previews[i] = athlete.PreparePackStep(inputs[i], drag[i], propulsion[i], extraEffort[i]);
                if (!active[i] && previews[i].MovementActive) previews[i] = athlete.ConstrainPreparedTravel(previews[i], 0);
                BuildTrace(i, previews[i], desiredLateral[i]);
            }
            // Arrival tickets are scratch until commit. Keep a waiting ticket across tiny
            // acceleration prefixes at a stop line; clear it once its body is admitted.
            for (int i = 0; i < Count; i++)
            {
                preparedJunctionWaitSince[i] = junctionWaitSince[i];
                if (!active[i] || GetAthlete(i).HasFinished || OwnsJunction(i)) preparedJunctionWaitSince[i] = 0;
                else if (preparedJunctionWaitSince[i] == 0)
                    for (int j = 0; j < junctionCount; j++) if (Touches(traces[i], j))
                    { preparedJunctionWaitSince[i] = tick.Value; break; }
            }
            for (int i = 1; i < Count; i++)
                for (int j = i; j > 0 && Priority(order[j], order[j - 1]); j--)
                { int swap = order[j]; order[j] = order[j - 1]; order[j - 1] = swap; }
            for (int rank = 0; rank < Count; rank++)
            {
                int i = order[rank]; var athlete = GetAthlete(i);
                if (!active[i] || athlete.HasFinished) { processed[i] = true; continue; }
                if (!Safe(i))
                {
                    constrained[i] = true;
                    long available = previews[i].MovementActive ? previews[i].Movement.Motion.DistanceNanometers - athlete.Motion.DistanceNanometers : 0;
                    // Zero movement is a proven fallback: earlier accepted paths were checked against this start.
                    PrepareCandidate(i, inputs[i], 0, lateral[i]);
                    if (!Safe(i)) throw new InvalidOperationException("Initial shared pack geometry is inconsistent.");
                    // Resolve a blocked prefix at authored track millimetre resolution.
                    // Nanometre searches otherwise keep proposing microscopic stop/restart
                    // fragments with nearly identical times throughout a dense queue.
                    // Unblocked physics and exact receipt/finish endpoints remain unchanged.
                    long low = 0, high = available / SkiState.NanometersPerMillimeter + 1;
                    for (int n = 0; n < 64 && high - low > 1; n++)
                    {
                        long mid = low + (high - low) / 2;
                        bool valid;
                        try { PrepareCandidate(i, inputs[i], mid * SkiState.NanometersPerMillimeter, lateral[i]); valid = Safe(i); }
                        catch (InvalidOperationException) { valid = false; }
                        if (valid) low = mid; else high = mid;
                    }
                    PrepareCandidate(i, inputs[i], low * SkiState.NanometersPerMillimeter, lateral[i]);
                }
                processed[i] = true;
            }
            // All physical plans and athlete handles are validated before any authoritative commit/event.
            bool precisionFallback = false;
            try { ValidateCommonBoundaries(); }
            catch (InvalidOperationException)
            {
                // Keep the first nonstationary accepted trajectory in priority order. It was
                // checked against all still-stationary later athletes, so freezing them is safe.
                // This retains progress instead of repeatedly freezing the entire field when
                // distinct short stopping times round to the same fractional boundary.
                int keep = -1;
                for (int rank = 0; rank < Count; rank++)
                {
                    int i = order[rank];
                    if (active[i] && (traces[i].LateralEnd != lateral[i] || (previews[i].MovementActive &&
                        previews[i].Movement.Motion.DistanceNanometers > GetAthlete(i).Motion.DistanceNanometers))) { keep = i; break; }
                }
                if (keep < 0) throw;
                for (int i = 0; i < Count; i++) if (i != keep && active[i])
                {
                    constrained[i] |= traces[i].LateralEnd != lateral[i] || (previews[i].MovementActive &&
                        previews[i].Movement.Motion.DistanceNanometers > GetAthlete(i).Motion.DistanceNanometers);
                    PrepareCandidate(i, inputs[i], 0, lateral[i]);
                }
                ValidateCommonBoundaries(); precisionFallback = true;
            }
            for (int i = 0; i < Count; i++) if (!GetAthlete(i).HasFinished) GetAthlete(i).ValidatePreparedCommit(previews[i]);
            for (int i = 0; i < Count; i++) if (active[i])
                for (int j = 0; j < i; j++) if (active[j] && Conflict(traces[i], i, traces[j], j))
                    throw new InvalidOperationException("Final pack trajectory verification failed.");
            field.CommitPrepared(inputs, previews);
            Array.Copy(preparedJunctionWaitSince, junctionWaitSince, Count);
            if (precisionFallback) PrecisionFallbackTicks++;
            Array.Copy(drafts, committedDrafts, Count);
            for (int i = 0; i < Count; i++)
            {
                if (!active[i]) continue;
                bool contact = constrained[i];
                lateral[i] = traces[i].LateralEnd; tails[i] = traces[i].EndTail; onCourse[i] = true;
                if (contact != priorContact[i]) events.Record(new RaceEvent(tick, frequency, GetAthlete(i).AthleteId, contact ? RaceEventType.ContactStarted : RaceEventType.ContactEnded, (int)ContactReason.Athlete));
                priorContact[i] = contact;
                if (traces[i].NewTail.Active) events.Record(new RaceEvent(tick, frequency, GetAthlete(i).AthleteId, RaceEventType.PhysicalSeamCrossed, traces[i].SeamIndex));
            }
        }

        private void ValidateCommonBoundaries()
        {
            int total = 0;
            for (int i = 0; i < Count; i++) if (!GetAthlete(i).HasFinished)
                for (int j = 0; j < previews[i].Movement.SegmentCount; j++) allSegments[total++] = GetAthlete(i).GetPreparedSegment(previews[i], j);
            FractionalMotionTimeline.MergeBoundaries(allSegments.AsSpan(0, total), 1.0 / frequency, timeScratch, grid);
        }

        private void PrepareCandidate(int i, SprintAthleteInput input, long cap, double line)
        {
            var athlete = GetAthlete(i); previews[i] = athlete.PreparePackStep(input, drag[i], propulsion[i], extraEffort[i]);
            if (previews[i].MovementActive) previews[i] = athlete.ConstrainPreparedTravel(previews[i], cap);
            BuildTrace(i, previews[i], line);
        }
        private PhysicalTrackPoint RoutePoint(int index, long distance, PhysicalBoundarySide side = PhysicalBoundarySide.Outgoing)
            => rangeMaps == null ? map.MapRoute(distance, side) : rangeMaps[index].MapRoute(distance, side);
        private PhysicalMotionProjection Projection(int index) => rangeProjections == null ? projection : rangeProjections[index];
        private PhysicalTrackPoint CurrentPoint(int index)
        {
            var athlete = GetAthlete(index);
            if(athlete.RemainingStartApproachNanometers>0)return startOverlay.MapRemaining(athlete.RemainingStartApproachNanometers);
            var range = athlete.CurrentRangeMotion;
            return range.HasValue && range.Value.Phase == RangeTraversalPhase.PenaltyLoop ? map.MapPenalty(range.Value.PenaltyNanometers) : RoutePoint(index, athlete.RouteNanometers);
        }
        private DraftOutput Draft(int index, SprintAthleteInput input)
        {
            var self = stationary[index].Start; var athlete = GetAthlete(index); long origin = self.LoopLengthNanometers;
            var follower = new SkiPlacement(athlete.AthleteId, self.LoopId, new SkiState(origin, athlete.Motion.SpeedMetersPerSecond), lateral[index]);
            int count = 0;
            for (int i = 0; i < Count; i++) if (i != index && active[i] && map.SameLoop(self, stationary[i].Start))
            {
                long delta = stationary[i].Start.LocalNanometers - self.LocalNanometers;
                if (delta < 0 && self.Closed) delta += self.LoopLengthNanometers;
                if (delta < 0) continue;
                draftRoster[count++] = new SkiPlacement(GetAthlete(i).AthleteId, self.LoopId, new SkiState(origin + delta, GetAthlete(i).Motion.SpeedMetersPerSecond), lateral[i]);
            }
            var range = athlete.CurrentRangeMotion;
            var sample = athlete.RemainingStartApproachNanometers>0 ? startOverlay.Approach.Sample(athlete.RemainingStartApproachNanometers) : range.HasValue && range.Value.Phase == RangeTraversalPhase.PenaltyLoop ? penaltySampler.Sample(new TrackDistance(self.LocalNanometers / SkiState.NanometersPerMillimeter)) :
                field.Route.Sample(new TrackDistance(athlete.RouteNanometers / SkiState.NanometersPerMillimeter)).Track;
            return drafting.Evaluate(follower, draftRoster.AsSpan(0, count), sample, input.Ski.HeadwindMetersPerSecond);
        }
        private void BuildTrace(int i, SprintStepPreview preview, double line)
        {
            var athlete = GetAthlete(i); var trace = traces[i]; var idle = stationary[i];
            trace.Start = idle.Start; trace.End = idle.End; trace.LateralStart = lateral[i]; trace.LateralEnd = line;
            trace.Count = 0; trace.OldTail = tails[i]; trace.NewTail = default; trace.EndTail = tails[i]; trace.SeamIndex = -1;
            if (trace.OldTail.Active) trace.LateralEnd = trace.LateralStart;
            if (!preview.MovementActive) return;
            long penalty = athlete.CurrentRangeMotion.HasValue && athlete.CurrentRangeMotion.Value.Phase == RangeTraversalPhase.PenaltyLoop ? athlete.CurrentRangeMotion.Value.PenaltyNanometers : 0;
            var prior = trace.Start;
            for (int s = 0; s < preview.Movement.SegmentCount; s++)
            {
                var segment = athlete.GetPreparedSegment(preview, s);
                var start = segment.OnStartApproach ? startOverlay.MapRemaining(athlete.InitialStartApproachNanometers-segment.TravelBefore) : segment.OnPenaltyLoop ? map.MapPenalty(penalty) : RoutePoint(i, segment.RouteBefore);
                Seam(trace, prior, start, segment.TravelBefore);
                var receipt = Projection(i).Project(segment, start); trace.Segments[trace.Count++] = receipt;
                if (segment.OnPenaltyLoop) penalty = receipt.End.LocalNanometers;
                prior = receipt.End;
            }
            var range = athlete.PreviewRangeMotion(preview);
            long remainingApproach=athlete.InitialStartApproachNanometers-preview.Movement.Motion.DistanceNanometers;
            trace.End = remainingApproach>0 ? startOverlay.MapRemaining(remainingApproach) : range.HasValue && range.Value.Phase == RangeTraversalPhase.PenaltyLoop ? map.MapPenalty(range.Value.PenaltyNanometers) : RoutePoint(i, preview.Movement.RouteNanometers);
            Seam(trace, prior, trace.End, preview.Movement.Motion.DistanceNanometers);
            trace.EndTail = trace.NewTail.Active ? trace.NewTail : trace.OldTail;
            if (trace.EndTail.Active && preview.Movement.Motion.DistanceNanometers - trace.EndTail.BirthTravel > halfBody) trace.EndTail = default;
        }
        private void Seam(Trace trace, PhysicalTrackPoint before, PhysicalTrackPoint after, long travel)
        {
            if (map.SameLoop(before, after))
            { if (before.LocalNanometers != after.LocalNanometers) throw new InvalidOperationException("Unaccounted physical displacement at tick " + Tick.Value + ": " + before.LoopId + " " + before.LocalNanometers + " -> " + after.LocalNanometers + "; travel=" + travel); return; }
            int connection = ConnectionIndex(before, after);
            if (connection < 0 || trace.NewTail.Active || (trace.OldTail.Active && travel - trace.OldTail.BirthTravel <= halfBody))
                throw new InvalidOperationException("Unsupported or uncleared pack seam.");
            trace.NewTail = new Tail { Active = true, Point = before, Lateral = trace.LateralStart, BirthTravel = travel };
            // Keep the incoming proxy exact laterally throughout a bend/transition. Requested
            // steering work is still paid, but lateral transfer waits until the tail is clear.
            trace.LateralEnd = trace.LateralStart;
            trace.SeamIndex = connection;
        }
        private int ConnectionIndex(PhysicalTrackPoint before, PhysicalTrackPoint after)
        {
            for (int i = 0; i < connectionDefinition.Count; i++)
            {
                PhysicalSeamTransition transition;
                try { transition = connections.Transition(connectionDefinition.Get(i).Id, before, 0); }
                catch (ArgumentException) { continue; }
                if (map.SameLoop(transition.Outgoing, after) && transition.Outgoing.LocalNanometers == after.LocalNanometers) return i;
            }
            return -1;
        }
        private bool Priority(int a, int b)
        {
            bool ao = OwnsJunction(a), bo = OwnsJunction(b);
            if (ao != bo) return ao;
            long aw = preparedJunctionWaitSince[a], bw = preparedJunctionWaitSince[b];
            if (!ao && aw != bw && (aw != 0 || bw != 0)) return aw != 0 && (bw == 0 || aw < bw);
            long ar = GetAthlete(a).RouteNanometers, br = GetAthlete(b).RouteNanometers;
            if(startOverlay!=null){ar-=GetAthlete(a).RemainingStartApproachNanometers;br-=GetAthlete(b).RemainingStartApproachNanometers;}
            return ar != br ? ar > br : string.CompareOrdinal(GetAthlete(a).AthleteId.Value, GetAthlete(b).AthleteId.Value) < 0;
        }
        private bool OwnsJunction(int i)
        { if (!active[i]) return false; for (int j = 0; j < junctionCount; j++) if (Touches(stationary[i], j)) return true; return false; }
        private bool Safe(int i)
        { for (int j = 0; j < Count; j++) if (j != i && active[j] && Conflict(traces[i], i, processed[j] ? traces[j] : stationary[j], j)) return false; return true; }
        private bool Touches(Trace trace, int junction)
        {
            if (junctions.TouchesForwardSweep(junction, trace.Start, 0, halfBody) || junctions.TouchesForwardSweep(junction, trace.End, 0, halfBody)) return true;
            if (trace.OldTail.Active && junctions.TouchesForwardSweep(junction, trace.OldTail.Point, 0, halfBody)) return true;
            if (trace.NewTail.Active && junctions.TouchesForwardSweep(junction, trace.NewTail.Point, 0, halfBody)) return true;
            for (int i = 0; i < trace.Count; i++) if (junctions.TouchesForwardSweep(junction, trace.Segments[i].Start, trace.Segments[i].TravelNanometers, halfBody)) return true;
            return false;
        }
        private bool Conflict(Trace a, int ai, Trace b, int bi)
        {
            for (int j = 0; j < junctionCount; j++) if (Touches(a, j) && Touches(b, j)) return true;
            int count = 2; grid[0] = 0; grid[1] = JunctionAdmissionSession.FractionParts;
            for (int i = 0; i < a.Count; i++) { Insert(a.Segments[i].StartFraction, ref count); Insert(a.Segments[i].EndFraction, ref count); }
            for (int i = 0; i < b.Count; i++) { Insert(b.Segments[i].StartFraction, ref count); Insert(b.Segments[i].EndFraction, ref count); }
            for (int i = 1; i < count; i++)
            {
                var aa = Sweep(a, ai, grid[i - 1], grid[i]); var bb = Sweep(b, bi, grid[i - 1], grid[i]);
                if (occupancy.QueryRelativeMove(aa, bb).Blocked || GhostHit(a.OldTail, ai, bb) || GhostHit(a.NewTail, ai, bb) ||
                    GhostHit(b.OldTail, bi, aa) || GhostHit(b.NewTail, bi, aa)) return true;
            }
            return GhostPair(a.OldTail, ai, b.OldTail, bi) || GhostPair(a.NewTail, ai, b.OldTail, bi) ||
                GhostPair(a.OldTail, ai, b.NewTail, bi) || GhostPair(a.NewTail, ai, b.NewTail, bi);
        }
        private bool GhostPair(Tail a, int ai, Tail b, int bi) => a.Active && b.Active && GhostHit(a, ai,
            new PhysicalOccupancySweep(GetAthlete(bi).AthleteId, b.Point, 0, b.Lateral, b.Lateral));
        private bool GhostHit(Tail tail, int index, PhysicalOccupancySweep other) => tail.Active && occupancy.QueryRelativeMove(
            new PhysicalOccupancySweep(GetAthlete(index).AthleteId, tail.Point, 0, tail.Lateral, tail.Lateral), other).Blocked;
        private void Insert(long value, ref int count)
        { int i = 0; while (i < count && grid[i] < value) i++; if (i < count && grid[i] == value) return; for (int j = count; j > i; j--) grid[j] = grid[j - 1]; grid[i] = value; count++; }
        private PhysicalOccupancySweep Sweep(Trace trace, int index, long start, long end)
        {
            double fromLine = trace.LateralStart + (trace.LateralEnd - trace.LateralStart) * start / JunctionAdmissionSession.FractionParts;
            double toLine = trace.LateralStart + (trace.LateralEnd - trace.LateralStart) * end / JunctionAdmissionSession.FractionParts;
            var point = trace.Start;
            for (int i = 0; i < trace.Count; i++)
            {
                var segment = trace.Segments[i];
                if (start >= segment.StartFraction && end <= segment.EndFraction) return Projection(index).Slice(segment, GetAthlete(index).AthleteId, start, end, fromLine, toLine);
                if (segment.EndFraction <= start) point = segment.End;
            }
            if (trace.Count > 0 && start >= trace.Segments[trace.Count - 1].EndFraction) point = trace.End;
            return new PhysicalOccupancySweep(GetAthlete(index).AthleteId, point, 0, fromLine, toLine);
        }
    }
}

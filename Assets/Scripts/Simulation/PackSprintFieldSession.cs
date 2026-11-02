using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum PackMovementBlockKind { None, Width, Athlete }
    public readonly struct PackMovementBlock
    {
        public PackMovementBlockKind Kind { get; }
        public AthleteId Blocker { get; }
        /// <summary>The complete authored world-body sweep itself rejects the
        /// first requested move, independently of any coarse junction guard.</summary>
        public bool WorldBodyConflict { get; }
        internal PackMovementBlock(PackMovementBlockKind kind,AthleteId blocker=default,bool worldBodyConflict=false)
        {Kind=kind;Blocker=blocker;WorldBodyConflict=worldBodyConflict;}
    }
    /// <summary>Shared physical Sprint/Pursuit field. Physics/physiology are prepared before continuous
    /// pack validation; accepted forward prefixes, stops and lateral motion commit together.
    /// Prestart staging and classified athletes are outside the racing corridor.</summary>
    public sealed partial class PackSprintFieldSession : ISprintFieldView
    {
        private struct Tail
        {
            public bool Active; public PhysicalTrackPoint Point; public double Lateral; public long BirthTravel;
            // Provenance belongs to the accepted physical seam, including later ticks when
            // no new seam is present. A loop name alone cannot authorize a directed passage.
            public int ConnectionIndex, SourceSection; public long SeamRouteNanometers;
            public int ApronPath, ApronOccurrence;
        }
        private sealed class Trace
        {
            internal readonly PhysicalMotionReceipt[] Segments;
            internal readonly long[] RouteBefore, RouteAfter;
            internal RaceMotionSegment[] ApronSegments;
            internal PhysicalApronMotionV2[] ApronMotions;
            internal PhysicalApronTraceV2 ApronTrace;
            internal int ApronPath;
            // 0 unknown, 1 absent, 2 present. Junction definitions/body are immutable;
            // start/end/receipts and both incoming tails belong to one trace revision.
            internal readonly byte[] JunctionTouches;
            internal void ResetJunctionTouches() => Array.Clear(JunctionTouches,0,JunctionTouches.Length);
            internal int Count;
            internal PhysicalTrackPoint Start, End;
            internal double LateralStart, LateralEnd;
            internal Tail OldTail, NewTail, EndTail;
            internal int SeamIndex;
            internal long StartRoute, EndRoute;
            internal Trace(int capacity,int junctionCount,bool passageRoutes=false)
            {
                Segments = new PhysicalMotionReceipt[capacity]; JunctionTouches = new byte[junctionCount];
                if(passageRoutes){RouteBefore=new long[capacity];RouteAfter=new long[capacity];}
            }
        }
        private readonly SprintFieldSession field;
        private readonly PhysicalTrackMap map;
        private readonly PhysicalMotionProjection projection;
        private readonly PhysicalRangeLaneMap[] rangeMaps;
        private readonly PhysicalMotionProjection[] rangeProjections;
        private readonly PhysicalRangeFieldLayout rangeLayout;
        private readonly PhysicalMotionProjection[][] pathProjections;
        private readonly PhysicalRangeArrivalOrder pathArrivalOrder;
        private readonly PhysicalRangeReservations reservations;
        private readonly PhysicalRangeRelease[] reservationReleases;
        private readonly long[] reservationCaps;
        private readonly bool[] reservationWaiting, preparedReservationWaiting;
        private readonly PhysicalRangeArrival[] pathArrivals;
        private readonly long[] pathAdmissionLines;
        private readonly RaceMotionSegment[] arrivalSegments;
        private readonly TrackSampler[] physicalSamplers;
        private readonly PhysicalStartApproachMap startOverlay;
        private readonly PhysicalStartContinuationMap startContinuation;
        private readonly PhysicalDirectedPassageMap directedPassages;
        private readonly bool[] passageSharing;
        private readonly PhysicalSharedApronMapV2 sharedApron;
        private readonly PhysicalRangeWorldTrafficMap rangeWorldTraffic;
        private readonly PhysicalRangeMatAdmissionMap matAdmission;
        private readonly PhysicalApronPairScratchV2 apronScratch;
        private long preparedApronPairProofs;
        public bool UsesSharedRangeApron => sharedApron!=null;
        public int SharedRangeApronRegionCount => sharedApron?.RegionCount??0;
        public long SharedRangeApronPairProofs { get; private set; }
        public bool IsSharedRangeApronSharingEnabled(int region) => sharedApron!=null&&region>=0&&region<sharedApron.RegionCount?sharedApron.IsEligible(region):throw new ArgumentOutOfRangeException(nameof(region));
        public MassStartGrid StartGrid { get; }
        public StartApproachMap StartApproach => startOverlay?.Approach;
        public bool UsesAuthoredStartApproach => startOverlay!=null&&startOverlay.UsesAuthoredBranch;
        public bool UsesSharedStartContinuation => startContinuation!=null;
        public bool UsesDirectedPassages => directedPassages!=null;
        public int DirectedPassageCount => directedPassages?.Count??0;
        public long DirectedPassageTickReserveNanometers { get; }
        public bool IsDirectedPassageSharingEnabled(int passage)
            =>passageSharing!=null&&passage>=0&&passage<passageSharing.Length?passageSharing[passage]:throw new ArgumentOutOfRangeException(nameof(passage));
        private readonly PhysicalMotionProjection[][] orderedLaneProjections;
        private readonly int[] assignedLane, workingLane, assignedBout, workingBout, arrivals, workingArrivals;
        private readonly long[] rangeForks;
        private readonly ShootingLaneAssignment[] orderedShootingLanes;
        public bool UsesReusableRangeMats => reservations!=null;
        internal bool UsesRangePathChoices=>reservations!=null||pathArrivalOrder!=null;
        internal int RangePathLaneCount=>rangeLayout?.Count??0;
        public PhysicalRangeReservations RangeReservations => reservations;
        public bool IsWaitingForRangeCapacity(int index) => reservationWaiting!=null&&reservationWaiting[index];
        public int GetAssignedRangeLane(int index) => reservations!=null?reservations.GetOwnedLane(index):rangeLayout != null ? GetAthlete(index).AssignedShootingLane.Ordinal : assignedLane==null?index+1:assignedLane[index]+1;
        public int GetRangeArrivalCount(int boutIndex) => reservations!=null?reservations.GetArrivalCount(boutIndex):pathArrivalOrder == null ? throw new InvalidOperationException("No physical common-line arrival policy.") : pathArrivalOrder.GetArrivalCount(boutIndex);
        public PhysicalRangeArrival GetAcceptedRangeArrival(int index, int boutIndex)
        {
            if(reservations==null)return pathArrivalOrder == null ? throw new InvalidOperationException("No physical common-line arrival policy.") : pathArrivalOrder.GetAcceptedArrival(index,boutIndex);
            var record=reservations.GetRecord(index,boutIndex);if(!record.IsValid)throw new InvalidOperationException("The athlete has no accepted arrival for this bout.");return record.Arrival;
        }
        private readonly PhysicalJunctionMap junctions;
        private readonly PhysicalJunctionDefinition junctionDefinition;
        private readonly PhysicalConnectionMap connections;
        private readonly PhysicalConnectionDefinition connectionDefinition;
        private readonly OccupancySystem widths;
        private readonly PhysicalOccupancySystem occupancy;
        private readonly DraftingSystem drafting;
        private readonly TrackSampler penaltySampler;
        private readonly OvertakeDefinition movement;
        private readonly IRaceEventSink events;
        private readonly Trace[] traces, stationary;
        // Private reflection-only differential reference; no gameplay/context option.
        private bool bypassJunctionTouchCache = false;
        private readonly SprintStepPreview[] previews;
        private readonly Tail[] tails;
        private readonly bool[] onCourse, active, processed, priorContact, constrained;
        private readonly PackMovementBlock[] initialBlocks;
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
        private readonly string manualBodyContext;
        private readonly ManualRouteActorFrame[] manualFrames;
        private readonly PhysicalTrackPoint[] manualPoints,preparedManualPoints;
        private ManualWorldRosterLedger manualWorld;
        public bool HasManualWorldState=>manualWorld!=null||manualScopedWorld!=null;
        public SimulationTick Tick => field.Tick;
        public int TickFrequency => frequency;
        public int Count => field.Count;
        /// <summary>Why the first full-speed candidate was shortened on the last tick.
        /// The blocker is a stable athlete ID; absence means no initial pack block.</summary>
        public PackMovementBlock GetInitialMovementBlock(int index)
            =>index>=0&&index<Count?initialBlocks[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public bool IsComplete => field.IsComplete;
        public SprintResultSnapshot Result => field.Result;
        public PursuitResultSnapshot PursuitResult => field.PursuitResult;
        public MassStartResultSnapshot MassStartResult => field.MassStartResult;
        public TrackDefinition Track => field.Track;
        public RaceRoute Route => field.Route;
        public CompetitionProfile Profile => field.Profile;
        public CheckpointTimingBoard Timing => field.Timing;
        public long PrecisionFallbackTicks { get; private set; }
        public string ContextJson { get; private set; }
        private readonly bool completeStartCorridorProof;
        private readonly int startProofVersion;
        public int StartContinuationVersion=>startContinuation!=null?startProofVersion:0;
        public RaceWeatherTimeline WeatherTimeline {get;private set;}
        public void BindWeather(RaceWeatherTimeline timeline,TrackVisualDefinition geometry=null)
        {
            if(Tick.Value!=0||WeatherTimeline!=null||timeline==null)throw new InvalidOperationException("Weather requires a fresh field.");
            for(int i=0;i<Count;i++)GetAthlete(i).BindWeather(timeline,geometry);
            field.BindWeatherEvents(timeline);
            WeatherTimeline=timeline;ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,"absolute_race_weather_v1",timeline.ToJson());
            if(geometry!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,"weather_route_heading_v2:"+ManualContextIdentity.Sha256(geometry.ToJson()));
        }
        public SprintAthleteSession GetAthlete(int index) => field.GetAthlete(index);
        public bool IsOnCourse(int index) => onCourse[index] && !GetAthlete(index).HasFinished;
        public double GetLateral(int index) => lateral[index];
        /// <summary>Choose stationary departure lines before signed-world
        /// binding. Exact Pursuit starts and all athlete capabilities remain.
        /// Width, body and passing clearance determine the available slots.</summary>
        internal void ArrangeManualPursuitStartLines(OccupancyDefinition body)
        {
            if(Profile.Format!=CompetitionFormat.Pursuit)return;
            if(Tick.Value!=0)throw new InvalidOperationException("Departure lines must be selected before any paid race tick.");
            double spacing=body.LateralSeparation+2*movement.PassingClearance;
            double edgeMargin=Math.Max(body.EdgeMargin,body.LateralSeparation*.5+body.EdgeClearance);
            int sideSlots=(int)Math.Floor((WidthAt(GetPhysicalPoint(0))*.5-edgeMargin)/spacing);
            if(sideSlots<1)throw new ArgumentException("Pursuit start snow must fit adjacent departure lines.");
            int slots=2*sideSlots+1;
            for(int i=0;i<Count;i++)
            {
                int slot=i%slots;
                lateral[i]=PhysiologyNumbers.Quantize(((slot+1)/2)*spacing*(slot%2==0?1:-1));
            }
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{ContextJson,"signed_pursuit_initial_departure_lines_v1",
                spacing.ToString("R",System.Globalization.CultureInfo.InvariantCulture),slots.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "stationary_initial_layout_only;stable_qualification_order;unchanged_official_start_ticks;no_paid_wait_or_lateral_teleport"});
        }
        public ManualRouteActorFrame GetManualWorldActor(int index)
        {
            if(manualWorld==null)throw new InvalidOperationException("No accepted base-route manual world state.");
            return index>=0&&index<Count?manualFrames[index]:throw new ArgumentOutOfRangeException(nameof(index));
        }
        /// <summary>Read-only preview of the next ordinary paid lateral step on the
        /// first start continuation, including bent bodies and retained incoming tails.</summary>
        public bool IsStartContinuationLateralRequestClear(int index,double requested)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            PhysiologyNumbers.RequireRange(requested,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(requested));
            if(!TryGetStartContinuationObservation(index,out _)||tails[index].Active)return false;
            var point=GetPhysicalPoint(index);
            double next=PhysiologyNumbers.Quantize(lateral[index]+Math.Max(-lateralStep,Math.Min(lateralStep,requested-lateral[index])));
            if(next==lateral[index]||!FitsLoopEnvelope(point,next))return false;
            long travel=Math.Min((long)Math.Round(GetAthlete(index).Motion.SpeedMetersPerSecond/frequency*SkiState.NanometersPerMeter),point.LoopLengthNanometers-point.LocalNanometers);
            var sweep=new PhysicalOccupancySweep(GetAthlete(index).AthleteId,point,travel,lateral[index],next);
            if(!startContinuation.Contains(sweep))return false;
            for(int j=0;j<Count;j++)if(j!=index&&TryGetStartContinuationObservation(j,out _))
            {
                var other=GetPhysicalPoint(j);
                var body=new PhysicalOccupancySweep(GetAthlete(j).AthleteId,other,0,lateral[j],lateral[j]);
                if(startContinuation.Conflicts(sweep,body)||WorldGhostHit(tails[j],j,sweep))return false;
            }
            return true;
        }
        /// <summary>Conservative destination-lane preview through the first start seam.
        /// This selects an AI intent; only the next paid step is ever accepted.</summary>
        public bool IsStartContinuationBypassGoalClear(int index,double requested,double lookaheadMeters)
        {
            PhysiologyNumbers.RequireRange(lookaheadMeters,.001,100,nameof(lookaheadMeters));
            if(!IsStartContinuationLateralRequestClear(index,requested))return false;
            var point=GetPhysicalPoint(index);if(!FitsLoopEnvelope(point,requested))return false;
            long remaining=(long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter);
            long travel=Math.Min(remaining,point.LoopLengthNanometers-point.LocalNanometers);
            var sweep=new PhysicalOccupancySweep(GetAthlete(index).AthleteId,point,travel,requested,requested);
            if(!Clear(sweep))return false;
            if(remaining>travel&&point.LoopId==startContinuation.SourceLoopId)
            {
                var main=rangeLayout.GetPath(index).PhysicalMap.MapRoute(0);
                var continued=new PhysicalOccupancySweep(GetAthlete(index).AthleteId,main,Math.Min(remaining-travel,startContinuation.MainPrefixNanometers),requested,requested);
                if(!FitsLoopEnvelope(main,requested)||!Clear(continued))return false;
            }
            return true;
            bool Clear(PhysicalOccupancySweep candidate)
            {
                if(!startContinuation.Contains(candidate))return false;
                for(int j=0;j<Count;j++)if(j!=index&&TryGetStartContinuationObservation(j,out _))
                {
                    var body=new PhysicalOccupancySweep(GetAthlete(j).AthleteId,GetPhysicalPoint(j),0,lateral[j],lateral[j]);
                    if(startContinuation.Conflicts(candidate,body)||WorldGhostHit(tails[j],j,candidate))return false;
                }
                return true;
            }
        }
        /// <summary>Read-only AI lookahead; a clear request still pays ordinary lateral
        /// effort and is fully revalidated by the accepting field transaction.</summary>
        public bool HasPaidRouteWorldGeometry(PhysicalTrackPoint point)=>rangeWorldTraffic!=null&&rangeWorldTraffic.IsPaidRoutePoint(point);
        /// <summary>Read-only AI destination-line lookahead against observed bodies
        /// and retained tails. It grants no admission and never changes player input.
        /// The ordinary per-tick sweep still pays and validates the lateral transition.</summary>
        public bool IsRangeWorldForwardLaneClear(int index,double line,double lookaheadMeters)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            PhysiologyNumbers.RequireRange(line,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(line));
            PhysiologyNumbers.RequireRange(lookaheadMeters,.001,100,nameof(lookaheadMeters));
            if(rangeWorldTraffic==null||GetAthlete(index).ActiveShooting!=null)return false;
            var point=GetPhysicalPoint(index);
            if(!rangeWorldTraffic.Contains(point)||!FitsBypassPathWidth(index,point,line,(long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter)))return false;
            long travel=Math.Min((long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter),point.LoopLengthNanometers-point.LocalNanometers);
            var sweep=new PhysicalOccupancySweep(GetAthlete(index).AthleteId,point,travel,line,line);
            for(int j=0;j<Count;j++)if(j!=index&&IsOnCourse(j))
            {
                var other=GetPhysicalPoint(j);if(!rangeWorldTraffic.Contains(other))continue;
                var body=new PhysicalOccupancySweep(GetAthlete(j).AthleteId,other,0,lateral[j],lateral[j]);
                bool pose=GetAthlete(j).CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                if(rangeWorldTraffic.Conflicts(sweep,body,false,pose)||tails[j].Active&&
                    rangeWorldTraffic.TailConflicts(tails[j].Point,tails[j].Lateral,sweep,tails[j].ConnectionIndex,pose))return false;
            }
            return true;
        }
        /// <summary>Observe the last proven world-body blocker in the current
        /// body's right frame, including bodies on a different branch.</summary>
        public bool TryObserveInitialWorldBlockerSide(int index,out double right)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            right=0;var block=initialBlocks[index];
            if(rangeWorldTraffic==null||!block.WorldBodyConflict||block.Kind!=PackMovementBlockKind.Athlete)return false;
            for(int j=0;j<Count;j++)if(j!=index&&IsOnCourse(j)&&GetAthlete(j).AthleteId.Equals(block.Blocker))
                return rangeWorldTraffic.TryObserveRelativeBody(GetPhysicalPoint(index),lateral[index],
                    GetPhysicalPoint(j),lateral[j],out _,out right);
            return false;
        }
        /// <summary>Width-only planning horizon, independent of bodies occupying
        /// the forward lane. A short congestion escape may relax body lookahead
        /// but still must fit the upcoming assigned corridor.</summary>
        public bool IsRangeWorldBypassLaneWidthClear(int index,double line,double lookaheadMeters)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            PhysiologyNumbers.RequireRange(line,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(line));
            PhysiologyNumbers.RequireRange(lookaheadMeters,.001,100,nameof(lookaheadMeters));
            if(rangeWorldTraffic==null||GetAthlete(index).ActiveShooting!=null)return false;
            var point=GetPhysicalPoint(index);
            return rangeWorldTraffic.Contains(point)&&FitsBypassPathWidth(index,point,line,
                (long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter));
        }
        public bool IsRangeWorldLateralRequestClear(int index,double requested,double previewMeters)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            PhysiologyNumbers.RequireRange(requested,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(requested));
            PhysiologyNumbers.RequireRange(previewMeters,.001,5,nameof(previewMeters));
            if(rangeWorldTraffic==null||tails[index].Active||GetAthlete(index).ActiveShooting!=null)return false;
            var point=GetPhysicalPoint(index);if(!rangeWorldTraffic.Contains(point))return false;
            // Match the field's committed representation. A sub-nanometre residue
            // otherwise renews an escape forever while every accepted step is zero.
            double next=PhysiologyNumbers.Quantize(lateral[index]+Math.Max(-lateralStep,Math.Min(lateralStep,requested-lateral[index])));
            if(next==lateral[index]||!FitsBypassPathWidth(index,point,next,0))return false;
            var selfId=GetAthlete(index).AthleteId;
            var self=new SkiPlacement(selfId,point.LoopId,new SkiState(point.LocalNanometers,GetAthlete(index).Motion.SpeedMetersPerSecond),lateral[index]);
            int count=0;
            for(int j=0;j<Count;j++)if(j!=index&&IsOnCourse(j))
            {
                var other=GetPhysicalPoint(j);
                if(other.LoopId==point.LoopId)draftRoster[count++]=new SkiPlacement(GetAthlete(j).AthleteId,point.LoopId,new SkiState(other.LocalNanometers,GetAthlete(j).Motion.SpeedMetersPerSecond),lateral[j]);
            }
            double width=GetAthlete(index).RangePath.SampleRoute(GetAthlete(index).RouteNanometers).WidthMeters;
            if(point.LoopId==Track.Penalty.LoopId)width=penaltySampler.Sample(new TrackDistance(point.LocalNanometers/SkiState.NanometersPerMillimeter)).WidthMeters;
            if(widths.QueryMove(self,new SkiPlacement(selfId,point.LoopId,self.Motion,next),width,draftRoster.AsSpan(0,count)).Blocked)return false;
            long travel=Math.Min((long)Math.Round(Math.Min(previewMeters,GetAthlete(index).Motion.SpeedMetersPerSecond/frequency)*SkiState.NanometersPerMeter),point.LoopLengthNanometers-point.LocalNanometers);
            var sweep=new PhysicalOccupancySweep(selfId,point,travel,lateral[index],next);
            for(int j=0;j<Count;j++)if(j!=index&&IsOnCourse(j))
            {
                var other=GetPhysicalPoint(j);if(!rangeWorldTraffic.Contains(other))continue;
                if(other.LoopId==point.LoopId&&!point.Closed&&!tails[j].Active)continue;
                var body=new PhysicalOccupancySweep(GetAthlete(j).AthleteId,other,0,lateral[j],lateral[j]);
                bool pose=GetAthlete(j).CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                if(rangeWorldTraffic.Conflicts(sweep,body,false,pose)||tails[j].Active&&rangeWorldTraffic.TailConflicts(tails[j].Point,tails[j].Lateral,sweep,tails[j].ConnectionIndex,pose))return false;
            }
            return true;
        }
        private bool FitsBypassPathWidth(int index,PhysicalTrackPoint point,double line,long forward)
        {
            if(!FitsLoopEnvelope(point,line))return false;
            var actor=GetAthlete(index);
            if(actor.CurrentRangeMotion?.Phase!=RangeTraversalPhase.PenaltyLoop&&
                !FitsRouteEnvelope(actor.RangePath,actor.RouteNanometers,line,forward))return false;
            // The body's front can already enter a narrower penalty lane while
            // its centre is still on the wide exit aisle. Match that admission
            // envelope rather than repeatedly proposing an impossible outward step.
            var front=actor.PreviewCurrentRangeMotion(checked(halfBody+forward));
            if(front?.Phase==RangeTraversalPhase.PenaltyLoop&&
                !FitsLoopEnvelope(map.MapPenalty(front.Value.PenaltyNanometers),line))return false;
            return true;
        }
        /// <summary>First unserved junction arrival tick; zero means no pending ticket.</summary>
        public long GetJunctionWaitSince(int index) => junctionWaitSince[index];
        public DraftOutput GetDraft(int index) => committedDrafts[index];
        public PhysicalTrackPoint GetPhysicalPoint(int index) => CurrentPoint(index);
        /// <summary>First-pass paid station used only for AI observation across the exact
        /// authored start seam. Admission still uses the world swept-body continuation.</summary>
        public bool TryGetStartContinuationObservation(int index,out long stationNanometers)
        {
            var athlete=GetAthlete(index);var point=GetPhysicalPoint(index);stationNanometers=0;
            if(startContinuation==null||!IsOnCourse(index)||!startContinuation.Contains(point)||
                point.LoopId==startContinuation.MainLoopId&&athlete.RouteNanometers>startContinuation.MainPrefixNanometers)return false;
            stationNanometers=point.LoopId==startContinuation.SourceLoopId?point.LocalNanometers:
                checked(StartApproach.LengthNanometers+point.LocalNanometers);
            return true;
        }

        public PackSprintFieldSession(SimulationConfig config, CompetitionProfile profile, SkiModelDefinitions skiModel,
            ShootingModelDefinitions shootingModel, TrackDefinition track, RaceRoute route, RaceSnowConditions conditions,
            ResultTimingDefinition timing, ReadOnlySpan<SprintEntrant> entrants, SimulationTick firstStart, string resultId,
            OccupancyDefinition body, DraftDefinition draft, OvertakeDefinition movement,
            PhysicalJunctionDefinition junctionDefinition, PhysicalConnectionDefinition connectionDefinition, IRaceEventSink events,
            PhysicalRangeLaneDefinition rangeLanes = null, PursuitQualificationSnapshot qualification = null,
            MassStartGrid massGrid = null, StartApproachMap startApproach = null, MassStartRangePolicyDefinition rangePolicy = null,
            PhysicalRangeFieldLayout rangeLayout = null, bool authoredStartApproach = false,
            PhysicalStartContinuationDefinition startContinuation = null, bool reusableRangeMats = false, bool routeBoundJunctions = false,
            PhysicalDirectedPassageDefinition directedPassages = null,
            PhysicalSharedApronDefinition sharedApron = null, TrackVisualDefinition sharedApronVisual = null, VenueRangeLayout sharedApronRange = null,bool completeStartCorridorProof=false,int startContinuationVersion=2)
        {
            if(startContinuationVersion<0||startContinuationVersion>2)throw new ArgumentOutOfRangeException(nameof(startContinuationVersion));
            startProofVersion=completeStartCorridorProof?startContinuationVersion:0;
            this.completeStartCorridorProof=startProofVersion>0;
            this.junctionDefinition=junctionDefinition;
            if (rangeLayout != null && (rangeLanes != null || !rangeLayout.IsBoundTo(track, route)))
                throw new ArgumentException("One exact authored range layout is required.", nameof(rangeLayout));
            if(routeBoundJunctions&&rangeLayout==null)
                throw new ArgumentException("Route-bound junctions require an exact authored physical range layout.",nameof(rangeLayout));
            if(directedPassages!=null&&(profile==null||profile.Format!=CompetitionFormat.Pursuit||rangeLayout==null||
                !routeBoundJunctions||!reusableRangeMats||massGrid!=null))
                throw new ArgumentException("Directed format passages require an exact route-bound Pursuit field with reusable mats.",nameof(directedPassages));
            if(sharedApron!=null)
            {
                if(rangeLayout==null||!routeBoundJunctions||sharedApronVisual==null||config==null)
                    throw new ArgumentException("Shared apron requires exact route-bound authored paths and visual authority.");
                // Validate every optional source before schedulers can emit roster events.
                // The final map below adds the unchanged source-receipt rounding reserve.
                _ = new PhysicalSharedApronMapV2(rangeLayout.GetPath(0).PhysicalMap,track,route,profile,rangeLayout,
                    sharedApronVisual,junctionDefinition,connectionDefinition,body,sharedApron,1.0/config.TickFrequency,0);
                if(sharedApron.PenaltyWorldMergeCount>0)
                    rangeWorldTraffic=new PhysicalRangeWorldTrafficMap(rangeLayout.GetPath(0).PhysicalMap,track,route,sharedApronVisual,rangeLayout,junctionDefinition,connectionDefinition,body,sharedApron,
                        startProofVersion>=2&&startContinuation!=null?startApproach?.LoopId:null);
                if(sharedApronRange!=null)
                {
                    if(sharedApronRange.MatDefinition==null||sharedApronRange.Forward==null||Math.Abs(sharedApronRange.Forward.Y)>1e-9)
                        throw new ArgumentException("An explicit physical mat and horizontal final shooting direction are required.");
                    matAdmission=new PhysicalRangeMatAdmissionMap(track,route,profile,rangeLayout,sharedApronVisual,body,
                        sharedApronRange.MatDefinition,sharedApronRange.Forward.X,sharedApronRange.Forward.Z,1.0/config.TickFrequency);
                }
            }
            else if(sharedApronVisual!=null||sharedApronRange!=null)throw new ArgumentException("Apron visual/mat authority requires its typed definition.");
            if(reusableRangeMats)
            {
                if(rangeLayout==null||rangePolicy!=null||massGrid!=null||startApproach!=null&&!startApproach.HasLaunchPosition||config==null)
                    throw new ArgumentException("Reusable mats require exact Sprint/Pursuit paths without a Mass Start lane/start policy.");
                var roster=new AthleteId[entrants.Length];
                for(int i=0;i<roster.Length;i++)roster[i]=entrants[i]?.AthleteId??throw new ArgumentException("Null entrant.");
                // Reject the complete reservation contract before schedulers emit roster events.
                reservations=new PhysicalRangeReservations(profile,roster,rangeLayout,body,config.TickFrequency);
            }
            if(authoredStartApproach&&(rangeLayout==null||startApproach==null||massGrid==null&&!startApproach.HasLaunchPosition))
                throw new ArgumentException("Authored start mapping requires an explicit physical range layout, grid and approach.",nameof(authoredStartApproach));
            // Prove the optional new seam before constructing a field that may emit roster events.
            var authoredStart=authoredStartApproach?new PhysicalStartApproachMap(rangeLayout.GetPath(0).PhysicalMap,track,route,
                startApproach,true,junctionDefinition,connectionDefinition):null;
            if(startContinuation!=null&&!authoredStartApproach)
                throw new ArgumentException("Shared continuation requires an explicitly authored start branch.",nameof(startContinuation));
            this.startContinuation=startContinuation==null?null:new PhysicalStartContinuationMap(rangeLayout.GetPath(0).PhysicalMap,
                track,route,startApproach,junctionDefinition,connectionDefinition,body,startContinuation);
            // Optional policy failures must precede schedulers and their roster events. Reuse
            // this exact map for receipts; the null branch retains its original construction.
            var passagePhysicalMap=directedPassages==null?null:rangeLayout.GetPath(0).PhysicalMap;
            this.directedPassages=directedPassages==null?null:new PhysicalDirectedPassageMap(passagePhysicalMap,
                track,route,junctionDefinition,connectionDefinition,body,directedPassages);
            passageSharing=this.directedPassages==null?null:new bool[this.directedPassages.Count];
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.movement = movement ?? throw new ArgumentNullException(nameof(movement));
            this.rangeLayout = rangeLayout;
            field = new SprintFieldSession(config, profile, skiModel, shootingModel, track, route, conditions, timing, entrants, firstStart, resultId, events, qualification,massGrid,startApproach,rangeLayout,reusableRangeMats);
            frequency = config.TickFrequency; map = passagePhysicalMap??(rangeLayout == null ? new PhysicalTrackMap(track, route) : rangeLayout.GetPath(0).PhysicalMap);
            StartGrid=massGrid;
            if(startApproach!=null)
            {
                if(massGrid!=null&&massGrid.BodyJson!=body.ToJson())throw new ArgumentException("Grid and field bodies must match.");
                startOverlay=authoredStart??new PhysicalStartApproachMap(map,track,route,startApproach);
            }
            for (int i = 0; i < route.SectionCount; i++)
            {
                var section = route.GetSection(i);
                if (route.SchemaVersion==1 && !track.GetLoop(section.LoopId).Closed && (i > 0 || i < route.SectionCount - 1))
                    throw new ArgumentException("Pack multi-section routes require closed physical loops; open endpoints need explicit routed connectors.");
            }
            projection = new PhysicalMotionProjection(map, 1.0 / frequency);
            junctions = routeBoundJunctions?
                new PhysicalJunctionMap(map,junctionDefinition,track,route,rangeLayout,body,authoredStartApproach?startApproach.LoopId:null):
                new PhysicalJunctionMap(map,junctionDefinition); junctionCount = junctionDefinition.Count;
            this.connectionDefinition = connectionDefinition; connections = new PhysicalConnectionMap(map, junctionDefinition, connectionDefinition);
            if (rangeLanes != null)
            {
                if (Count > rangeLanes.Count) throw new ArgumentException("Unique physical range lanes required for the entire roster.");
                rangeMaps = new PhysicalRangeLaneMap[Count]; rangeProjections = new PhysicalMotionProjection[Count];
                for (int i = 0; i < Count; i++)
                {
                    rangeMaps[i] = new PhysicalRangeLaneMap(map, route, rangeLanes, i);
                    rangeProjections[i] = new PhysicalMotionProjection(map, 1.0 / frequency, rangeMaps[i],startApproach,
                        checked((startApproach?.LaunchRemainingNanometers??0)+(massGrid==null?0:-massGrid.Get(i).LongitudinalNanometers)),startOverlay);
                    var start = map.MapLoop(rangeLanes.GetLane(i), 0);
                    var end = map.MapLoop(rangeLanes.GetLane(i), (rangeLanes.Exit.Millimeters - rangeLanes.Entry.Millimeters) * SkiState.NanometersPerMillimeter);
                    if (ConnectionIndex(map.MapLoop(rangeLanes.MainLoopId, rangeLanes.Entry.Millimeters * SkiState.NanometersPerMillimeter), start) < 0 ||
                        ConnectionIndex(end, map.MapLoop(rangeLanes.MainLoopId, rangeLanes.Exit.Millimeters * SkiState.NanometersPerMillimeter)) < 0 ||
                        ConnectionIndex(end, map.MapPenalty(0)) < 0)
                        throw new ArgumentException("Range lane requires explicit entry, main exit and penalty exit connections.");
                }
            }
            else if(massGrid!=null && rangeLayout == null)
            {
                rangeProjections=new PhysicalMotionProjection[Count];
                for(int i=0;i<Count;i++)rangeProjections[i]=new PhysicalMotionProjection(map,1.0/frequency,null,startApproach,checked(startApproach.LaunchRemainingNanometers-massGrid.Get(i).LongitudinalNanometers),startOverlay);
            }
            if(rangePolicy!=null && rangeLayout == null)
            {
                if(massGrid==null || rangeMaps==null)throw new ArgumentException("Mass Start ordered lanes require grid and physical range branches.");
                assignedLane=new int[Count];workingLane=new int[Count];assignedBout=new int[Count];workingBout=new int[Count];
                arrivals=new int[profile.BoutCount];workingArrivals=new int[profile.BoutCount];rangeForks=new long[profile.BoutCount];
                arrivals[0]=Count;orderedLaneProjections=new PhysicalMotionProjection[Count][];
                orderedShootingLanes=new ShootingLaneAssignment[Count];
                for(int i=0;i<Count;i++)
                {
                    assignedLane[i]=workingLane[i]=i;orderedLaneProjections[i]=new PhysicalMotionProjection[Count];
                    orderedShootingLanes[i]=new ShootingLaneAssignment(track.Range.Id,"synthetic_lane_"+(i+1).ToString(System.Globalization.CultureInfo.InvariantCulture),i+1);
                    for(int j=0;j<Count;j++)orderedLaneProjections[i][j]=new PhysicalMotionProjection(map,1.0/frequency,rangeMaps[j],startApproach,checked(startApproach.LaunchRemainingNanometers-massGrid.Get(i).LongitudinalNanometers),startOverlay);
                }
                for(int b=0;b<profile.BoutCount;b++)
                {
                    int matches=0;
                    for(int s=0;s<route.SectionCount;s++)
                    {
                        var section=route.GetSection(s);
                        if(section.LapIndex+1==profile.GetBout(b).AfterLap && section.LoopId==rangeLanes.MainLoopId && section.HasRange)
                        {rangeForks[b]=(section.Start.Millimeters+rangeLanes.Entry.Millimeters-section.LoopStart.Millimeters)*SkiState.NanometersPerMillimeter;matches++;}
                    }
                    if(matches!=1)throw new ArgumentException("One physical range fork per bout required.");
                }
            }
            widths = new OccupancySystem(body); occupancy = new PhysicalOccupancySystem(map, body); drafting = new DraftingSystem(draft);
            manualBodyContext=body.ToJson();manualFrames=new ManualRouteActorFrame[Count];
            manualScopedFrames=new ManualRacePathActorFrame[Count];
            manualPoints=new PhysicalTrackPoint[Count];preparedManualPoints=new PhysicalTrackPoint[Count];
            penaltySampler = new TrackSampler(track.GetLoop(track.Penalty.LoopId));
            halfBody = (long)Math.Ceiling(body.LongitudinalSeparation * SkiState.NanometersPerMeter / 2);
            long passageTickReserve=0;
            if (rangeLayout != null)
            {
                int largest = 0;
                pathProjections = new PhysicalMotionProjection[Count][];
                for (int i = 0; i < Count; i++)
                {
                    largest = Math.Max(largest, GetAthlete(i).MotionSegmentCapacity);
                    pathProjections[i] = new PhysicalMotionProjection[rangeLayout.Count];
                    for (int j = 0; j < rangeLayout.Count; j++)
                        pathProjections[i][j] = new PhysicalMotionProjection(map, 1.0 / frequency, approach:startApproach,
                            startBehindLineNanometers:checked((startApproach?.LaunchRemainingNanometers??0)+(massGrid == null ? 0 : -massGrid.Get(i).LongitudinalNanometers)),
                            startOverlay:startOverlay, rangePath:rangeLayout.GetPath(j));
                }
                long maximumTickTravel = checked((long)Math.Ceiling(skiModel.Physics.MaximumSpeedMetersPerSecond / frequency * SkiState.NanometersPerMeter) + largest);
                if(this.directedPassages!=null)
                {
                    passageTickReserve=maximumTickTravel;
                    for(int p=0;p<directedPassages.Count;p++)
                    {
                        var passage=directedPassages.Get(p);var region=junctionDefinition.Get(passage.JunctionIndex);
                        var connection=connectionDefinition.Get(passage.ConnectionIndex);
                        PhysicalJunctionPort source=null,target=null;
                        for(int j=0;j<region.PortCount;j++)
                        {var port=region.GetPort(j);if(port.Id==connection.FromPortId)source=port;if(port.Id==connection.ToPortId)target=port;}
                        long entry=checked((source.Anchor.Distance.Millimeters-source.GuardBefore.Millimeters)*SkiState.NanometersPerMillimeter-halfBody-maximumTickTravel);
                        long exit=checked((target.Anchor.Distance.Millimeters+target.GuardAfter.Millimeters)*SkiState.NanometersPerMillimeter+halfBody+maximumTickTravel);
                        long first=checked(TrackDistance.FromMeters(passage.Source.Start).Millimeters*SkiState.NanometersPerMillimeter);
                        long last=checked(TrackDistance.FromMeters(passage.Target.End).Millimeters*SkiState.NanometersPerMillimeter);
                        // Whole-trace coverage must survive both guard boundaries while other
                        // bodies own this passage. Short custom windows retain full exclusivity.
                        passageSharing[p]=first<=entry&&last>=exit;
                    }
                }
                if(sharedApron!=null)
                {
                    this.sharedApron=new PhysicalSharedApronMapV2(map,track,route,profile,rangeLayout,sharedApronVisual,
                        junctionDefinition,connectionDefinition,body,sharedApron,1.0/frequency,maximumTickTravel);
                    apronScratch=new PhysicalApronPairScratchV2(Math.Max(1,largest));
                }
                rangeLayout.ValidatePackTopology(junctionDefinition, connectionDefinition, checked(halfBody + maximumTickTravel));
                if(UsesAuthoredStartApproach)
                    for(int i=0;i<rangeLayout.Count;i++)
                        if(rangeLayout.GetPath(i).FirstForkRouteNanometers(0)<=checked(halfBody+maximumTickTravel))
                            throw new ArgumentException("The authored start seam must clear its incoming body before a first-lap range fork.");
                physicalSamplers = new TrackSampler[track.LoopCount];
                for (int i = 0; i < physicalSamplers.Length; i++) physicalSamplers[i] = new TrackSampler(track.GetLoop(i));
                if (rangePolicy != null || reservations!=null)
                {
                    if(rangePolicy!=null)
                    {
                        if (massGrid == null) throw new ArgumentException("Common-line ordered paths require a Mass Start grid.");
                        pathArrivalOrder = new PhysicalRangeArrivalOrder(profile, massGrid, rangePolicy, frequency);
                    }
                    pathArrivals = new PhysicalRangeArrival[Count]; arrivalSegments = new RaceMotionSegment[largest];
                    pathAdmissionLines = new long[profile.BoutCount];
                    for (int b = 0; b < profile.BoutCount; b++)
                    {
                        var traversal = new RangePenaltyTraversal(profile, track, route, b, GetAthlete(0).AthleteId, frequency, events,
                            rangePath:rangeLayout.GetPath(0));
                        pathAdmissionLines[b] = traversal.RangeEntryNanometers;
                        if (rangeLayout.GetPath(0).FirstForkRouteNanometers(profile.GetBout(b).AfterLap - 1) - pathAdmissionLines[b] <= maximumTickTravel)
                            throw new ArgumentException("Ordered admission needs more than one maximum-speed tick of identical shared path before the earliest fork.");
                    }
                    if(reservations!=null)
                    {reservationReleases=new PhysicalRangeRelease[Count];reservationCaps=new long[Count];reservationWaiting=new bool[Count];preparedReservationWaiting=new bool[Count];}
                }
            }
            DirectedPassageTickReserveNanometers=passageTickReserve;
            lateralStep = movement.LateralSpeed / frequency; minimumWidth = double.MaxValue;
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i);
                for (int j = 0; j < loop.SegmentCount; j++) minimumWidth = Math.Min(minimumWidth, loop.GetSegment(j).WidthMeters);
            }
            if (!widths.FitsWidth(0, minimumWidth)) throw new ArgumentException("Race paths must contain the body.");
            traces = new Trace[Count]; stationary = new Trace[Count]; previews = new SprintStepPreview[Count]; tails = new Tail[Count];
            onCourse = new bool[Count]; active = new bool[Count]; processed = new bool[Count]; priorContact = new bool[Count]; constrained = new bool[Count];
            initialBlocks = new PackMovementBlock[Count];
            lateral = new double[Count]; desiredLateral = new double[Count]; drag = new double[Count]; propulsion = new double[Count];
            extraEffort = new int[Count]; order = new int[Count]; drafts = new DraftOutput[Count]; committedDrafts = new DraftOutput[Count]; draftRoster = new SkiPlacement[Count];
            junctionWaitSince = new long[Count]; preparedJunctionWaitSince = new long[Count];
            int capacity = 0;
            for (int i = 0; i < Count; i++) { traces[i] = new Trace(GetAthlete(i).MotionSegmentCapacity,junctionCount,this.directedPassages!=null); stationary[i] = new Trace(0,junctionCount,this.directedPassages!=null); capacity += traces[i].Segments.Length; }
            allSegments = new RaceMotionSegment[capacity]; grid = new long[2 + capacity * 2]; timeScratch = new double[grid.Length];
            if(this.sharedApron!=null)
                for(int i=0;i<Count;i++)
                {
                    var size=Math.Max(1,GetAthlete(i).MotionSegmentCapacity);
                    PrepareApronBuffers(traces[i],i,size);PrepareApronBuffers(stationary[i],i,size);
                }
            if(massGrid!=null)
            {
                for(int i=0;i<Count;i++)
                {
                    lateral[i]=massGrid.Get(i).LateralNanometers/1e9;onCourse[i]=true;
                    var point=CurrentPoint(i);
                    double startWidth=UsesAuthoredStartApproach?WidthAt(point):track.GetLoop(startApproach.LoopId).GetSegment(0).WidthMeters;
                    if(!widths.FitsWidth(lateral[i],startWidth))throw new ArgumentException("Grid does not fit actual start corridor.");
                    if(UsesAuthoredStartApproach&&(!FitsLoopEnvelope(point,lateral[i])||
                        !FitsApproachExitEnvelope(GetAthlete(i).RemainingStartApproachNanometers,GetAthlete(i).RangePath,lateral[i])))
                        throw new ArgumentException("Grid body does not fit the complete authored start corridor envelope.");
                    var idle=stationary[i];idle.ResetJunctionTouches();idle.Start=idle.End=point;idle.LateralStart=idle.LateralEnd=lateral[i];
                    for(int j=0;j<i;j++)if(Conflict(idle,i,stationary[j],j))throw new ArgumentException("Grid conflicts with physical corridor/junctions: "+i+" and "+j+" at "+point.LoopId+"/"+point.LocalNanometers+".");
                }
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] { "sprint_shared_pack_prefix_v1", field.ContextJson,
                body.ToJson(), draft.ToJson(), movement.ToJson(), junctionDefinition.ToJson(), connectionDefinition.ToJson(),
                "tick_start_periodic_draft;fixed_tick_lateral;prefix_stop_mm_quantum;full_interval_junction;seam_tail_lateral_lock;staging_before_admission;finish_exit;fraction_nearest_even_1e9;precision_keep_first_mover;contact_from_replan;junction_fifo_after_incumbent_v1" });
            if (rangeLanes != null) ContextJson = ShootingModelDefinitions.AppendContext(ContextJson, new[] { rangeLanes.ToJson(), "physical_range_lanes_v1;unique_roster_slot_lane;last_lap_main_bypass" });
            if(startOverlay!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{startOverlay.ContextJson,"grid_bodies_present_before_gun_v1"});
            if(rangePolicy!=null && rangeLayout == null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{rangePolicy.ToJson(),"arrival_at_physical_fork;exclusive_entry_tick_order;first_bib_then_bout_arrival_v1"});
            if(rangeLayout!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{rangeLayout.ContextJson,
                "actual_path_projection_draft_width;gradual_lateral;local_junctions;single_incoming_body;bounded_seam_spacing_v1"});
            if(pathArrivalOrder!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{pathArrivalOrder.ContextJson,
                "final_accepted_common_prefix;reprepare_identical_receipts;athlete_path_order_joint_commit_v1"});
            if(reservations!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{reservations.ContextJson,
                "final_accepted_release_and_arrival;monotone_capacity_caps;reprepare_identical_common_receipts;body_tail_clearance;athlete_reservation_joint_commit_v1"});
            if(routeBoundJunctions)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{junctions.UsageContextJson,
                "reachable_physical_ports_v1;preserve_self_crossings;reject_unbound_points;full_source_region_indices"});
            if(this.startContinuation!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{
                this.startContinuation.ContextJson,"front_incoming_endpoint_until_accepted_handoff_v2_parallel_source_bodies"});
            if(StartContinuationVersion>=1)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,"start_continuation_all_captured_ports_world_proof_v2");
            if(this.directedPassages!=null)
            {
                var coverage=new string[passageSharing.Length+1];
                coverage[0]=passageTickReserve.ToString(System.Globalization.CultureInfo.InvariantCulture);
                for(int p=0;p<passageSharing.Length;p++)coverage[p+1]=passageSharing[p]?"shared":"exclusive";
                ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{this.directedPassages.ContextJson,
                    ShootingModelDefinitions.EncodeContext(coverage),"directed_format_passages_v1;exact_route_occurrences;accepted_connection_tail_provenance;all_receipts_and_tail_pairs;whole_guard_plus_maximum_tick_window_v1;short_windows_exclusive;other_regions_exclusive"});
            }
            if(this.sharedApron!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{this.sharedApron.ContextJson,
                "shared_range_apron_field_v1;exact_selected_path_receipts_and_accepted_tail_occurrences;each_shared_region_independent;same_loop_body_and_other_guards_unchanged;final_precommit_joint_validation"});
            if(matAdmission!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,new[]{matAdmission.ContextJson});
            if(rangeWorldTraffic!=null)
            {
                // Append the new optional proof to the existing JSON string array. Re-encoding
                // the entire nested legacy context doubles its escaping at every new layer.
                // All legacy bytes and every proof value remain present; absent policy is unchanged.
                string added=ShootingModelDefinitions.EncodeContext(new[]{rangeWorldTraffic.ContextJson});
                ContextJson=ContextJson.Substring(0,ContextJson.Length-1)+","+added.Substring(1);
            }
        }

        private void PrepareApronBuffers(Trace trace,int index,int capacity)
        {
            trace.ApronSegments=new RaceMotionSegment[capacity];trace.ApronMotions=new PhysicalApronMotionV2[capacity];
            trace.ApronTrace=sharedApron.CreateTrace(0,GetAthlete(index).AthleteId,capacity,pathProjections[index][0]);
        }
        public void Step(ReadOnlySpan<SprintAthleteInput> inputs, ReadOnlySpan<double> requestedLateral)
        {
            if(HasManualWorldState)throw new InvalidOperationException("A manual world field cannot step through legacy route movement.");
            field.ValidateCommands(inputs);
            if (requestedLateral.Length != Count) throw new ArgumentException("Complete lateral command batch required.");
            for (int i = 0; i < Count; i++) PhysiologyNumbers.RequireRange(requestedLateral[i], -1000000, 1000000, nameof(requestedLateral));
            var tick = Tick.Next();
            preparedApronPairProofs=0;
            if(reservations!=null)for(int i=0;i<Count;i++){reservationCaps[i]=long.MaxValue;preparedReservationWaiting[i]=false;}
            ResetLanePlan();
            // Snapshot all physical starts before preparing any athlete.
            for (int i = 0; i < Count; i++)
            {
                var athlete = GetAthlete(i); var start = CurrentPoint(i); var idle = stationary[i];
                idle.ResetJunctionTouches();
                idle.Start = idle.End = start; idle.LateralStart = idle.LateralEnd = lateral[i]; idle.Count = 0;
                idle.StartRoute=idle.EndRoute=athlete.RouteNanometers;
                if(sharedApron!=null)idle.ApronPath=athlete.RangePath.LaneOrdinal-1;
                idle.OldTail = tails[i]; idle.NewTail = default; idle.EndTail = tails[i];
                active[i] = onCourse[i] && !athlete.HasFinished; processed[i] = false; constrained[i] = false; order[i] = i;
                initialBlocks[i]=default;
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
                double startWidth = rangeLayout == null ? minimumWidth : WidthAt(stationary[i].Start);
                double bound = Math.Floor((startWidth / 2 - widths.Definition.EdgeMargin) * 1e9) / 1e9;
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
                PrepareLaneChoice(i);
                if (!Safe(i, true))
                {
                    constrained[i] = true;
                    long available = previews[i].MovementActive ? previews[i].Movement.Motion.DistanceNanometers - athlete.Motion.DistanceNanometers : 0;
                    // Zero movement is a proven fallback: earlier accepted paths were checked against this start.
                    double acceptedLine = lateral[i];
                    if (rangeLayout != null)
                    {
                        // A body stopped by a narrower corridor may still move inward.
                        // Pay the requested lateral work and keep only a body-safe gradual move.
                        PrepareCandidate(i, inputs[i], 0, desiredLateral[i]);
                        if (Safe(i)) acceptedLine = traces[i].LateralEnd;
                    }
                    PrepareCandidate(i, inputs[i], 0, acceptedLine);
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
                        try { PrepareCandidate(i, inputs[i], mid * SkiState.NanometersPerMillimeter, acceptedLine); valid = Safe(i); }
                        catch (InvalidOperationException) { valid = false; }
                        if (valid) low = mid; else high = mid;
                    }
                    PrepareCandidate(i, inputs[i], low * SkiState.NanometersPerMillimeter, acceptedLine);
                }
                AcceptLaneArrival(i);
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
                if(assignedLane!=null)
                {
                    // Cancel speculative assignments with cancelled trajectories. Rebuild the retained
                    // entrant against committed counters and all stationary bodies before admission.
                    ResetLanePlan();
                    for(int i=0;i<Count;i++)BuildTrace(i,previews[i],i==keep?traces[i].LateralEnd:lateral[i]);
                    if(keep>=0)
                    {
                        PrepareLaneChoice(keep);
                        if(!Safe(keep))PrepareCandidate(keep,inputs[keep],0,lateral[keep]);
                        AcceptLaneArrival(keep);
                    }
                }
                ValidateCommonBoundaries(); precisionFallback = true;
            }
            var arrivalPreview = pathArrivalOrder == null ? default : PrepareAcceptedPathChoices(inputs, tick);
            var reservationPreview=reservations==null?default:PrepareReusableReservations(inputs,tick,ref precisionFallback);
            for (int i = 0; i < Count; i++) if (!GetAthlete(i).HasFinished) GetAthlete(i).ValidatePreparedCommit(previews[i]);
            if (pathArrivalOrder != null) pathArrivalOrder.ValidatePreview(arrivalPreview);
            if(reservations!=null)reservations.ValidatePreview(reservationPreview);
            for (int i = 0; i < Count; i++) if (active[i])
            {
                if(matAdmission!=null)
                {
                    var trace=traces[i];var preview=previews[i];
                    long available=preview.MovementActive?preview.Movement.Motion.DistanceNanometers-GetAthlete(i).Motion.DistanceNanometers:0;
                    if(matAdmission.MaximumAcceptedTravel(GetAthlete(i),preview,trace.ApronPath,trace.LateralStart,trace.LateralEnd,trace.ApronSegments)<available)
                        throw new InvalidOperationException("Final physical mat admission verification failed.");
                }
                if (rangeLayout != null && !FitsActualWidth(i, traces[i]))
                    throw new InvalidOperationException("Final physical corridor verification failed.");
                for (int j = 0; j < i; j++) if (active[j] && Conflict(traces[i], i, traces[j], j))
                    throw new InvalidOperationException("Final pack trajectory verification failed.");
            }
            if(assignedLane!=null)
                for(int i=0;i<Count;i++)if(workingBout[i]!=assignedBout[i])
                    GetAthlete(i).SetRangeLane(orderedShootingLanes[workingLane[i]]);
            field.CommitPrepared(inputs, previews);
            if (pathArrivalOrder != null) pathArrivalOrder.Commit(arrivalPreview);
            if(reservations!=null)
            {
                reservations.Commit(reservationPreview);
                Array.Copy(preparedReservationWaiting,reservationWaiting,Count);
                for(int i=0;i<reservations.LastChangeCount;i++)
                {
                    var change=reservations.GetLastChange(i);
                    events.Record(new RaceEvent(tick,frequency,change.AthleteId,change.IsRelease?RaceEventType.PhysicalRangeReservationReleased:RaceEventType.PhysicalRangeReservationAcquired,change.LaneOrdinal));
                }
            }
            if(assignedLane!=null)
            {
                for(int i=0;i<Count;i++)
                {
                    if(workingBout[i]!=assignedBout[i])assignedLane[i]=workingLane[i];
                    if(workingBout[i]!=assignedBout[i] || tick.Value==GetAthlete(i).ScheduledStart.Value)
                        events.Record(new RaceEvent(tick,frequency,GetAthlete(i).AthleteId,RaceEventType.MassStartRangeLaneAssigned,assignedLane[i]+1));
                }
                Array.Copy(workingBout,assignedBout,Count);Array.Copy(workingArrivals,arrivals,arrivals.Length);
            }
            Array.Copy(preparedJunctionWaitSince, junctionWaitSince, Count);
            if (precisionFallback) PrecisionFallbackTicks++;
            if(sharedApron!=null)SharedRangeApronPairProofs+=preparedApronPairProofs;
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

        /// <summary>Prevalidate one same-section world publication with high-water credit.
        /// Start, range, penalty, seam/contact continuation and world drafting
        /// orchestration remain separate; this is not the live manual runner.</summary>
        internal void ValidateManualBasePublication(ManualWorldRosterPreview proposal,ReadOnlySpan<DraftOutput> nextDrafts)
        {
            var world=proposal.Owner;
            if(world==null||!world.HasPhysicalOdometer||!world.IsBoundTo(Route)||
                world.Count!=Count||world.TickFrequency!=frequency||world.Tick.Value!=Tick.Value||
                world.BodyContextJson!=manualBodyContext||nextDrafts.Length!=Count||
                manualScopedWorld!=null||
                manualWorld!=null&&!ReferenceEquals(manualWorld,world))
                throw new ArgumentException("Exact complete world, body, clock and draft roster required.");
            world.ValidatePrepared(proposal);
            for(int i=0;i<Count;i++)
            {
                var athlete=GetAthlete(i);var before=world.GetActor(i);var point=CurrentPoint(i);
                var cursor=world.GetCursor(i);var after=world.GetProposedActor(proposal,i);
                var next=world.GetProposedCursor(proposal,i);
                long oldLocal=checked((long)Math.Round(before.Projection.LocalMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
                if(!IsOnCourse(i)||athlete.RemainingStartApproachNanometers!=0||tails[i].Active||priorContact[i]||
                    athlete.ActiveShooting!=null||!before.Athlete.Equals(athlete.AthleteId)||
                    cursor.CreditedStationNanometers!=athlete.RouteNanometers||
                    !world.GetPhysicalMotion(i).Equals(athlete.Motion)||
                    before.Projection.LoopId!=point.LoopId||Math.Abs(oldLocal-point.LocalNanometers)>2||
                    Math.Abs(before.Projection.LateralMeters-lateral[i])>2*PhysiologyNumbers.MinimumPositive||
                    cursor.SectionIndex!=next.SectionIndex)
                    throw new ArgumentException("Active admitted same-section base-route roster required.");
                PhysiologyNumbers.RequireRange(nextDrafts[i].Strength,0,1,nameof(nextDrafts));
                PhysiologyNumbers.RequireRange(nextDrafts[i].DragReduction,0,1,nameof(nextDrafts));
                preparedManualPoints[i]=map.MapLoop(after.Projection.LoopId,
                    checked((long)Math.Round(after.Projection.LocalMeters*SkiState.NanometersPerMeter,MidpointRounding.ToEven)));
            }
        }

        internal void CommitManualBasePrepared(ReadOnlySpan<SprintAthleteInput> inputs,
            ReadOnlySpan<SprintStepPreview> prepared,ManualWorldRosterPreview proposal,ReadOnlySpan<DraftOutput> nextDrafts)
        {
            ValidateManualBasePublication(proposal,nextDrafts);field.ValidateCommands(inputs);
            if(prepared.Length!=Count)throw new ArgumentException("Complete prepared manual field required.");
            for(int i=0;i<Count;i++)
            {
                if(!ReferenceEquals(prepared[i].World.Owner,proposal.Owner)||prepared[i].World.Tick.Value!=proposal.Tick.Value)
                    throw new ArgumentException("Each race row needs the same full-world proof.");
                GetAthlete(i).ValidatePreparedCommit(prepared[i]);
            }
            string nextContext=ContextJson;
            if(manualWorld==null)
            {
                // Append encoded values to the existing array without wrapping
                // and escaping the complete legacy context for another level.
                string extra=ShootingModelDefinitions.EncodeContext("manual_pack_same_section_high_water_publication_v2",proposal.Owner.ContextJson);
                nextContext=ContextJson.Substring(0,ContextJson.Length-1)+","+extra.Substring(1);
            }
            field.CommitPrepared(inputs,prepared);
            for(int i=0;i<Count;i++)
            {
                manualFrames[i]=proposal.Owner.GetProposedActor(proposal,i);
                manualPoints[i]=preparedManualPoints[i];lateral[i]=manualFrames[i].Projection.LateralMeters;
                committedDrafts[i]=nextDrafts[i];initialBlocks[i]=default;junctionWaitSince[i]=0;
            }
            proposal.Owner.Commit(proposal);manualWorld=proposal.Owner;ContextJson=nextContext;
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
            if(reservationCaps!=null)cap=Math.Min(cap,reservationCaps[i]);
            var athlete = GetAthlete(i); previews[i] = athlete.PreparePackStep(input, drag[i], propulsion[i], extraEffort[i]);
            if (previews[i].MovementActive)
            {
                // A reusable capacity replan starts unrestricted actors with an unbounded
                // cap. ConstrainPreparedTravel accepts only the available forward prefix.
                if(reservationCaps!=null)cap=Math.Min(cap,previews[i].Movement.Motion.DistanceNanometers-athlete.Motion.DistanceNanometers);
                previews[i] = athlete.ConstrainPreparedTravel(previews[i], cap);
            }
            BuildTrace(i, previews[i], line);
        }
        private PhysicalRangeReservationPreview PrepareReusableReservations(ReadOnlySpan<SprintAthleteInput> inputs,SimulationTick tick,ref bool precisionFallback)
        {
            for(int attempt=0;attempt<=Count;attempt++)
            {
                int arrivalsCount=0,releasesCount=0;
                for(int i=0;i<Count;i++)
                {
                    var actor=GetAthlete(i);if(!active[i]||actor.HasFinished||!previews[i].MovementActive)continue;
                    int count=CopySegments(i,previews[i]);var initial=new RaceMotionPoint(actor.RouteNanometers,actor.Motion.DistanceNanometers);
                    int lane=reservations.GetOwnedLane(i),ownedBout=reservations.GetOwnedBout(i);
                    if(lane>0)
                    {
                        long line=reservations.ClearanceRouteNanometers(lane,ownedBout);
                        if(actor.RouteNanometers>line)throw new InvalidOperationException("An accepted private clearance was not released by its joint transaction.");
                        if(PhysicalRangeRelease.TryFromAcceptedMotion(actor.AthleteId,ownedBout,lane,tick,frequency,line,initial,arrivalSegments.AsSpan(0,count),out var release))
                        {
                            if(traces[i].EndTail.Active&&traces[i].EndTail.Point.LoopId==reservations.PrivateLoopId(lane))
                                throw new InvalidOperationException("A private incoming body is still retained after its proposed clearance.");
                            reservationReleases[releasesCount++]=release;
                        }
                    }
                    int bout=actor.ActiveBout;
                    if(bout<0||bout!=reservations.GetLastAssignedBout(i)+1||
                        actor.CurrentRangeMotion.HasValue&&actor.CurrentRangeMotion.Value.Phase!=RangeTraversalPhase.Approach)continue;
                    if(PhysicalRangeArrival.TryFromAcceptedMotion(actor.AthleteId,bout,tick,frequency,pathAdmissionLines[bout],initial,arrivalSegments.AsSpan(0,count),out var arrival))
                    {
                        if(previews[i].Movement.RouteNanometers>=actor.RangePath.FirstForkRouteNanometers(Profile.GetBout(bout).AfterLap-1))
                            throw new InvalidOperationException("An unassigned reusable arrival reached a private fork.");
                        pathArrivals[arrivalsCount++]=arrival;
                    }
                }
                var prepared=reservations.Prepare(tick,reservationReleases.AsSpan(0,releasesCount),pathArrivals.AsSpan(0,arrivalsCount));
                bool replan=false;
                for(int i=0;i<Count;i++)if(reservations.WasPreparedDenied(prepared,i))
                {
                    var actor=GetAthlete(i);long line=pathAdmissionLines[actor.ActiveBout];
                    if(actor.RouteNanometers>line)throw new InvalidOperationException("An unassigned body passed its common admission line.");
                    long cap=Math.Max(0,line-1-actor.RouteNanometers);
                    if(cap>=reservationCaps[i])throw new InvalidOperationException("A denied admission must reduce its unaccepted forward prefix.");
                    reservationCaps[i]=cap;preparedReservationWaiting[i]=true;constrained[i]=true;replan=true;
                }
                if(replan){ReplanReusablePrefixes(inputs,ref precisionFallback);continue;}
                for(int r=0;r<arrivalsCount;r++)
                {
                    var arrival=pathArrivals[r];int i=0;while(!GetAthlete(i).AthleteId.Equals(arrival.AthleteId))i++;
                    ReprepareAcceptedChoice(i,inputs[i],reservations.GetPreparedRecord(prepared,i,arrival.BoutIndex).LaneOrdinal);
                }
                ValidateCommonBoundaries();return prepared;
            }
            throw new InvalidOperationException("Reusable admission did not converge after every roster prefix was bounded.");
        }
        private void ReplanReusablePrefixes(ReadOnlySpan<SprintAthleteInput> inputs,ref bool precisionFallback)
        {
            for(int i=0;i<Count;i++)
            {
                processed[i]=false;if(GetAthlete(i).HasFinished)continue;
                PrepareCandidate(i,inputs[i],active[i]?long.MaxValue:0,desiredLateral[i]);
            }
            for(int rank=0;rank<Count;rank++)
            {
                int i=order[rank];var actor=GetAthlete(i);if(!active[i]||actor.HasFinished){processed[i]=true;continue;}
                if(!Safe(i))
                {
                    constrained[i]=true;long available=previews[i].MovementActive?previews[i].Movement.Motion.DistanceNanometers-actor.Motion.DistanceNanometers:0;
                    double line=lateral[i];PrepareCandidate(i,inputs[i],0,desiredLateral[i]);if(Safe(i))line=traces[i].LateralEnd;
                    PrepareCandidate(i,inputs[i],0,line);if(!Safe(i))throw new InvalidOperationException("Initial reusable pack geometry is inconsistent.");
                    long low=0,high=available/SkiState.NanometersPerMillimeter+1;
                    for(int n=0;n<64&&high-low>1;n++)
                    {long mid=low+(high-low)/2;bool valid;try{PrepareCandidate(i,inputs[i],mid*SkiState.NanometersPerMillimeter,line);valid=Safe(i);}catch(InvalidOperationException){valid=false;}if(valid)low=mid;else high=mid;}
                    PrepareCandidate(i,inputs[i],low*SkiState.NanometersPerMillimeter,line);
                }
                processed[i]=true;
            }
            try{ValidateCommonBoundaries();}
            catch(InvalidOperationException)
            {
                int keep=-1;for(int rank=0;rank<Count;rank++){int i=order[rank];if(active[i]&&(traces[i].LateralEnd!=lateral[i]||previews[i].MovementActive&&previews[i].Movement.Motion.DistanceNanometers>GetAthlete(i).Motion.DistanceNanometers)){keep=i;break;}}
                if(keep<0)throw;
                for(int i=0;i<Count;i++)if(i!=keep&&active[i]){constrained[i]=true;PrepareCandidate(i,inputs[i],0,lateral[i]);}
                ValidateCommonBoundaries();precisionFallback=true;
            }
        }
        private void ReprepareAcceptedChoice(int i,SprintAthleteInput input,int ordinal)
        {
            var actor=GetAthlete(i);var original=previews[i];int count=CopySegments(i,original);
            long cap=original.Movement.Motion.DistanceNanometers-actor.Motion.DistanceNanometers;double line=traces[i].LateralEnd;
            var choice=actor.CreateRangePathChoice(ordinal);previews[i]=actor.PreparePackStep(input,drag[i],propulsion[i],extraEffort[i],choice);
            previews[i]=actor.ConstrainPreparedTravel(previews[i],cap);
            if(!SameMovement(original.Movement,previews[i].Movement))throw new InvalidOperationException("A reusable path choice changed accepted common-prefix physics.");
            for(int s=0;s<count;s++)if(!SameSegment(arrivalSegments[s],actor.GetPreparedSegment(previews[i],s)))throw new InvalidOperationException("A reusable path choice changed an accepted timed receipt.");
            BuildTrace(i,previews[i],line);
        }
        private PhysicalRangeArrivalPreview PrepareAcceptedPathChoices(ReadOnlySpan<SprintAthleteInput> inputs, SimulationTick tick)
        {
            int count = 0;
            for (int i = 0; i < Count; i++)
            {
                var actor = GetAthlete(i); int bout = actor.ActiveBout;
                if (!active[i] || actor.HasFinished || bout < 0 || bout != pathArrivalOrder.GetLastAssignedBout(i) + 1 ||
                    actor.CurrentRangeMotion.HasValue && actor.CurrentRangeMotion.Value.Phase != RangeTraversalPhase.Approach ||
                    !previews[i].MovementActive) continue;
                int segments = CopySegments(i, previews[i]);
                if (PhysicalRangeArrival.TryFromAcceptedMotion(actor.AthleteId, bout, tick, frequency, pathAdmissionLines[bout],
                    new RaceMotionPoint(actor.RouteNanometers, actor.Motion.DistanceNanometers), arrivalSegments.AsSpan(0, segments), out var arrival))
                {
                    if (previews[i].Movement.RouteNanometers >= actor.RangePath.FirstForkRouteNanometers(Profile.GetBout(bout).AfterLap - 1))
                        throw new InvalidOperationException("A newly accepted arrival reached a private fork before path choice.");
                    pathArrivals[count++] = arrival;
                }
            }
            var prepared = pathArrivalOrder.Prepare(tick, pathArrivals.AsSpan(0, count));
            for (int r = 0; r < count; r++)
            {
                var arrival = pathArrivals[r]; int i = 0;
                while (!GetAthlete(i).AthleteId.Equals(arrival.AthleteId)) i++;
                var actor = GetAthlete(i); var original = previews[i];
                int segments = CopySegments(i, original);
                long cap = original.Movement.Motion.DistanceNanometers - actor.Motion.DistanceNanometers;
                double line = traces[i].LateralEnd;
                var choice = actor.CreateRangePathChoice(pathArrivalOrder.GetPreparedOrdinal(prepared, i, arrival.BoutIndex));
                previews[i] = actor.PreparePackStep(inputs[i], drag[i], propulsion[i], extraEffort[i], choice);
                previews[i] = actor.ConstrainPreparedTravel(previews[i], cap);
                if (!SameMovement(original.Movement, previews[i].Movement))
                    throw new InvalidOperationException("Path choice changed accepted common-prefix physics.");
                for (int s = 0; s < segments; s++)
                    if (!SameSegment(arrivalSegments[s], actor.GetPreparedSegment(previews[i], s)))
                        throw new InvalidOperationException("Path choice changed an accepted timed receipt.");
                BuildTrace(i, previews[i], line);
            }
            // Rebuilt paths must retain the same fractional timeline before all handles,
            // local guards and body sweeps are revalidated by the joint commit below.
            ValidateCommonBoundaries();
            return prepared;
        }
        private int CopySegments(int i, SprintStepPreview preview)
        {
            int count = preview.Movement.SegmentCount;
            for (int s = 0; s < count; s++) arrivalSegments[s] = GetAthlete(i).GetPreparedSegment(preview, s);
            return count;
        }
        private static bool SameMovement(RacePathMotionFrame a, RacePathMotionFrame b) =>
            a.Motion.DistanceNanometers == b.Motion.DistanceNanometers && a.Motion.SpeedMetersPerSecond == b.Motion.SpeedMetersPerSecond &&
            a.RouteNanometers == b.RouteNanometers && a.RangeTravelNanometers == b.RangeTravelNanometers &&
            a.ConsumedSeconds == b.ConsumedSeconds && a.RemainingSeconds == b.RemainingSeconds && a.SegmentCount == b.SegmentCount &&
            a.AtFiringLine == b.AtFiringLine && a.AtRouteStop == b.AtRouteStop;
        private static bool SameSegment(RaceMotionSegment a, RaceMotionSegment b) =>
            a.RouteBefore == b.RouteBefore && a.RouteAfter == b.RouteAfter && a.TravelBefore == b.TravelBefore && a.TravelAfter == b.TravelAfter &&
            a.StartSeconds == b.StartSeconds && a.EndSeconds == b.EndSeconds && a.OnPenaltyLoop == b.OnPenaltyLoop && a.OnStartApproach == b.OnStartApproach;

        private double WidthAt(PhysicalTrackPoint point)
            => physicalSamplers[point.LoopIndex].Sample(new TrackDistance(point.LocalNanometers / SkiState.NanometersPerMillimeter)).WidthMeters;
        private static double LineAt(Trace trace, long fraction)
            => trace.LateralStart + (trace.LateralEnd - trace.LateralStart) * fraction / JunctionAdmissionSession.FractionParts;
        private bool FitsLoopEnvelope(PhysicalTrackPoint point, double line)
        {
            long length = point.LoopLengthNanometers;
            long start = point.LocalNanometers - halfBody, end = point.LocalNanometers + halfBody;
            if (!point.Closed) return FitsLoopInterval(point.LoopIndex, Math.Max(0, start), Math.Min(length, end), line);
            if (start < 0) return FitsLoopInterval(point.LoopIndex, length + start, length, line) && FitsLoopInterval(point.LoopIndex, 0, end, line);
            if (end >= length) return FitsLoopInterval(point.LoopIndex, start, length, line) && FitsLoopInterval(point.LoopIndex, 0, end - length, line);
            return FitsLoopInterval(point.LoopIndex, start, end, line);
        }
        private bool FitsLoopInterval(int loopIndex, long start, long end, double line)
        {
            var loop = Track.GetLoop(loopIndex);
            while (true)
            {
                var sample = physicalSamplers[loopIndex].Sample(new TrackDistance(start / SkiState.NanometersPerMillimeter));
                if (!widths.FitsWidth(line, sample.WidthMeters)) return false;
                if (start >= end) return true;
                var segment = loop.GetSegment(sample.SegmentIndex);
                long next = checked((segment.StartDistance.Millimeters + segment.Length.Millimeters) * SkiState.NanometersPerMillimeter);
                if (next <= start) return true; // the exact open-loop endpoint was already sampled
                start = Math.Min(end, next);
            }
        }
        private bool FitsRouteEnvelope(PhysicalRangePathMap path, long routePoint, double line, long forward = 0)
        {
            long start = Math.Max(0, routePoint - halfBody);
            // Preview the assigned route, including a private mat fork. Checking
            // only the current corridor can repeatedly select a line that fits
            // Main but cannot advance into the narrower path immediately ahead.
            long end = Math.Min(checked(Route.Length.Millimeters * SkiState.NanometersPerMillimeter), checked(routePoint + halfBody + forward));
            while (true)
            {
                if (!widths.FitsWidth(line, path.SampleRoute(start).WidthMeters)) return false;
                if (start == end) return true;
                long next = path.DistanceToNextBoundary(start);
                if (next <= 0) throw new InvalidOperationException("Range width envelope did not advance.");
                start = Math.Min(end, checked(start + next));
            }
        }
        private bool FitsApproachExitEnvelope(long remaining,PhysicalRangePathMap path,double line)
            =>remaining>halfBody||FitsRouteEnvelope(path,0,line);
        private bool FitsActualWidth(int index, Trace trace)
        {
            if (!FitsLoopEnvelope(trace.Start, trace.LateralStart) || !FitsLoopEnvelope(trace.End, trace.LateralEnd)) return false;
            if (trace.OldTail.Active && !widths.FitsWidth(trace.OldTail.Lateral, WidthAt(trace.OldTail.Point)) ||
                trace.NewTail.Active && !widths.FitsWidth(trace.NewTail.Lateral, WidthAt(trace.NewTail.Point))) return false;
            var actor = GetAthlete(index); var path = actor.GetPreparedRangePath(previews[index]);
            var currentRange = actor.CurrentRangeMotion;
            var endRange = actor.PreviewRangeMotion(previews[index]);
            bool startPenalty = currentRange.HasValue && currentRange.Value.Phase == RangeTraversalPhase.PenaltyLoop;
            bool endPenalty = endRange.HasValue && endRange.Value.Phase == RangeTraversalPhase.PenaltyLoop;
            long endRoute = previews[index].MovementActive ? previews[index].Movement.RouteNanometers : actor.RouteNanometers;
            long remainingStart=actor.RemainingStartApproachNanometers;
            long remainingEnd=previews[index].MovementActive?actor.InitialStartApproachNanometers-previews[index].Movement.Motion.DistanceNanometers:remainingStart;
            bool actualStart=UsesAuthoredStartApproach&&remainingStart>0,actualEnd=UsesAuthoredStartApproach&&remainingEnd>0;
            if (!startPenalty && !(actualStart?FitsApproachExitEnvelope(remainingStart,path,trace.LateralStart):FitsRouteEnvelope(path, actor.RouteNanometers, trace.LateralStart)) ||
                !endPenalty && !(actualEnd?FitsApproachExitEnvelope(remainingEnd,path,trace.LateralEnd):FitsRouteEnvelope(path, endRoute, trace.LateralEnd))) return false;
            var front = actor.PreviewRangeMotion(previews[index], halfBody);
            if (front.HasValue && front.Value.Phase == RangeTraversalPhase.PenaltyLoop &&
                !FitsLoopEnvelope(map.MapPenalty(front.Value.PenaltyNanometers), trace.LateralEnd)) return false;
            for (int s = 0; s < trace.Count; s++)
            {
                var receipt = trace.Segments[s];
                // Movement splits at every real terrain boundary. The start sample owns
                // this positive interval; the outgoing end is checked separately above.
                double width = WidthAt(receipt.Start);
                if (!widths.FitsWidth(LineAt(trace, receipt.StartFraction), width) ||
                    !widths.FitsWidth(LineAt(trace, receipt.EndFraction), width)) return false;
                var segment = actor.GetPreparedSegment(previews[index], s);
                if (!FitsLoopEnvelope(receipt.Start, LineAt(trace, receipt.StartFraction)) ||
                    !FitsLoopEnvelope(receipt.End, LineAt(trace, receipt.EndFraction)) ||
                    !segment.OnPenaltyLoop && (UsesAuthoredStartApproach&&segment.OnStartApproach?
                    !FitsApproachExitEnvelope(actor.InitialStartApproachNanometers-segment.TravelBefore,path,LineAt(trace,receipt.StartFraction))||
                    !FitsApproachExitEnvelope(actor.InitialStartApproachNanometers-segment.TravelAfter,path,LineAt(trace,receipt.EndFraction)):
                    !FitsRouteEnvelope(path, segment.RouteBefore, LineAt(trace, receipt.StartFraction)) ||
                    !FitsRouteEnvelope(path, segment.RouteAfter, LineAt(trace, receipt.EndFraction)))) return false;
            }
            return true;
        }
        private PhysicalTrackPoint RoutePoint(int index, long distance, PhysicalBoundarySide side = PhysicalBoundarySide.Outgoing)
            => rangeLayout != null ? GetAthlete(index).GetPreparedRangePath(previews[index]).MapRoute(distance, side) : rangeMaps == null ? map.MapRoute(distance, side) : rangeMaps[workingLane==null?index:workingLane[index]].MapRoute(distance, side);
        private PhysicalMotionProjection Projection(int index) => rangeLayout != null ? pathProjections[index][GetAthlete(index).GetPreparedRangePath(previews[index]).LaneOrdinal - 1] : orderedLaneProjections!=null ? orderedLaneProjections[index][workingLane[index]] : rangeProjections == null ? projection : rangeProjections[index];
        private void ResetLanePlan()
        {
            if(assignedLane==null)return;
            Array.Copy(assignedLane,workingLane,Count);Array.Copy(assignedBout,workingBout,Count);Array.Copy(arrivals,workingArrivals,arrivals.Length);
        }
        private void PrepareLaneChoice(int i)
        {
            if(assignedLane==null)return;var actor=GetAthlete(i);int bout=actor.ActiveBout;
            if(bout<=assignedBout[i])return;
            if(!previews[i].MovementActive || previews[i].Movement.RouteNanometers<rangeForks[bout])return;
            if(workingArrivals[bout]>=Count)throw new InvalidOperationException("Range arrival capacity exhausted.");
            workingLane[i]=workingArrivals[bout];BuildTrace(i,previews[i],traces[i].LateralEnd);
        }
        private void AcceptLaneArrival(int i)
        {
            if(assignedLane==null)return;var actor=GetAthlete(i);int bout=actor.ActiveBout;
            if(bout>assignedBout[i] && previews[i].MovementActive && previews[i].Movement.RouteNanometers>=rangeForks[bout])
            {workingBout[i]=bout;workingArrivals[bout]++;}
        }
        private PhysicalTrackPoint CurrentPoint(int index)
        {
            if(HasManualScopedWorldState)return manualPoints[index];
            if(HasManualWorldState)return manualPoints[index];
            var athlete = GetAthlete(index);
            if(athlete.RemainingStartApproachNanometers>0)return startOverlay.MapRemaining(athlete.RemainingStartApproachNanometers);
            if(startContinuation!=null&&athlete.Motion.DistanceNanometers==0&&!tails[index].Active)
                return startOverlay.MapRemaining(0,PhysicalBoundarySide.Incoming);
            var range = athlete.CurrentRangeMotion;
            return range.HasValue && range.Value.Phase == RangeTraversalPhase.PenaltyLoop ? map.MapPenalty(range.Value.PenaltyNanometers) :
                rangeLayout != null ? athlete.RangePath.MapRoute(athlete.RouteNanometers) : assignedLane==null ? RoutePoint(index, athlete.RouteNanometers) : rangeMaps[assignedLane[index]].MapRoute(athlete.RouteNanometers);
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
                rangeLayout != null ? athlete.RangePath.SampleRoute(athlete.RouteNanometers) : field.Route.Sample(new TrackDistance(athlete.RouteNanometers / SkiState.NanometersPerMillimeter)).Track;
            return drafting.Evaluate(follower, draftRoster.AsSpan(0, count), sample, input.Ski.HeadwindMetersPerSecond);
        }
        private void BuildTrace(int i, SprintStepPreview preview, double line)
        {
            var athlete = GetAthlete(i); var trace = traces[i]; var idle = stationary[i];
            trace.ResetJunctionTouches();
            trace.Start = idle.Start; trace.End = idle.End; trace.LateralStart = lateral[i]; trace.LateralEnd = line;
            trace.StartRoute=trace.EndRoute=athlete.RouteNanometers;
            if(sharedApron!=null)trace.ApronPath=athlete.GetPreparedRangePath(preview).LaneOrdinal-1;
            trace.Count = 0; trace.OldTail = tails[i]; trace.NewTail = default; trace.EndTail = tails[i]; trace.SeamIndex = -1;
            if (trace.OldTail.Active) trace.LateralEnd = trace.LateralStart;
            if (!preview.MovementActive)
            {
                if(matAdmission!=null)matAdmission.MaximumAcceptedTravel(athlete,preview,trace.ApronPath,trace.LateralStart,trace.LateralEnd,trace.ApronSegments);
                return;
            }
            long penalty = athlete.CurrentRangeMotion.HasValue && athlete.CurrentRangeMotion.Value.Phase == RangeTraversalPhase.PenaltyLoop ? athlete.CurrentRangeMotion.Value.PenaltyNanometers : 0;
            var prior = trace.Start;
            for (int s = 0; s < preview.Movement.SegmentCount; s++)
            {
                var segment = athlete.GetPreparedSegment(preview, s);
                var start = segment.OnStartApproach ? startOverlay.MapRemaining(athlete.InitialStartApproachNanometers-segment.TravelBefore) : segment.OnPenaltyLoop ? map.MapPenalty(penalty) : RoutePoint(i, segment.RouteBefore);
                Seam(trace, prior, start, segment.TravelBefore,segment.RouteBefore);
                if(sharedApron!=null)trace.ApronSegments[trace.Count]=segment;
                var receipt = Projection(i).Project(segment, start); trace.Segments[trace.Count] = receipt;
                if(directedPassages!=null){trace.RouteBefore[trace.Count]=segment.RouteBefore;trace.RouteAfter[trace.Count]=segment.RouteAfter;}
                trace.Count++;
                if (segment.OnPenaltyLoop) penalty = receipt.End.LocalNanometers;
                prior = receipt.End;
            }
            var range = athlete.PreviewRangeMotion(preview);
            long remainingApproach=athlete.InitialStartApproachNanometers-preview.Movement.Motion.DistanceNanometers;
            trace.End = remainingApproach>0 ? startOverlay.MapRemaining(remainingApproach) : range.HasValue && range.Value.Phase == RangeTraversalPhase.PenaltyLoop ? map.MapPenalty(range.Value.PenaltyNanometers) : RoutePoint(i, preview.Movement.RouteNanometers);
            trace.EndRoute=preview.Movement.RouteNanometers;
            Seam(trace, prior, trace.End, preview.Movement.Motion.DistanceNanometers,trace.EndRoute);
            trace.EndTail = trace.NewTail.Active ? trace.NewTail : trace.OldTail;
            if (trace.EndTail.Active && preview.Movement.Motion.DistanceNanometers - trace.EndTail.BirthTravel > halfBody) trace.EndTail = default;
            if(matAdmission!=null)
            {
                long available=preview.Movement.Motion.DistanceNanometers-athlete.Motion.DistanceNanometers;
                long cap=matAdmission.MaximumAcceptedTravel(athlete,preview,trace.ApronPath,trace.LateralStart,trace.LateralEnd,trace.ApronSegments);
                if(cap<available)
                {
                    previews[i]=athlete.ConstrainPreparedTravel(preview,cap);
                    BuildTrace(i,previews[i],line);return;
                }
            }
        }
        private void Seam(Trace trace, PhysicalTrackPoint before, PhysicalTrackPoint after, long travel,long routeNanometers)
        {
            if (map.SameLoop(before, after))
            { if (before.LocalNanometers != after.LocalNanometers) throw new InvalidOperationException("Unaccounted physical displacement at tick " + Tick.Value + ": " + before.LoopId + " " + before.LocalNanometers + " -> " + after.LocalNanometers + "; travel=" + travel); return; }
            int connection = ConnectionIndex(before, after);
            if (connection < 0 || trace.NewTail.Active || (trace.OldTail.Active && travel - trace.OldTail.BirthTravel <= halfBody))
                throw new InvalidOperationException("Unsupported or uncleared pack seam.");
            int sourceSection=-1;
            if(directedPassages!=null)
                for(int p=0;p<directedPassages.Count;p++)if(directedPassages.GetConnectionIndex(p)==connection)
                {sourceSection=directedPassages.FindOccurrence(p,routeNanometers,before);break;}
            trace.NewTail = new Tail { Active = true, Point = before, Lateral = trace.LateralStart, BirthTravel = travel,
                ConnectionIndex=connection,SourceSection=sourceSection,SeamRouteNanometers=routeNanometers,
                ApronPath=sharedApron==null?0:trace.ApronPath,
                ApronOccurrence=sharedApron==null?0:sharedApron.FindSeamOccurrence(trace.ApronPath,connection,routeNanometers,before) };
            // Keep the incoming proxy exact laterally throughout a bend/transition. Requested
            // steering work is still paid, but lateral transfer waits until the tail is clear.
            trace.LateralEnd = trace.LateralStart;
            trace.SeamIndex = connection;
        }
        private int ConnectionIndex(PhysicalTrackPoint before, PhysicalTrackPoint after)
        {
            if (rangeLayout != null) return connections.FindTransition(before, after);
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
            if(rangeWorldTraffic!=null)
            {
                bool ap=PenaltyOverEntering(a,b),bp=PenaltyOverEntering(b,a);
                if(ap!=bp)return ap;
            }
            bool ao = OwnsJunction(a), bo = OwnsJunction(b);
            if (ao != bo) return ao;
            long aw = preparedJunctionWaitSince[a], bw = preparedJunctionWaitSince[b];
            if (!ao && aw != bw && (aw != 0 || bw != 0)) return aw != 0 && (bw == 0 || aw < bw);
            long ar = GetAthlete(a).RouteNanometers, br = GetAthlete(b).RouteNanometers;
            if(startOverlay!=null){ar-=GetAthlete(a).RemainingStartApproachNanometers;br-=GetAthlete(b).RemainingStartApproachNanometers;}
            return ar != br ? ar > br : string.CompareOrdinal(GetAthlete(a).AthleteId.Value, GetAthlete(b).AthleteId.Value) < 0;
        }
        private bool PenaltyOverEntering(int priority,int entering)
        {
            var p=GetAthlete(priority);var e=GetAthlete(entering);var phase=p.CurrentRangeMotion?.Phase;
            if(!(phase==RangeTraversalPhase.PenaltyLoop||phase==RangeTraversalPhase.Rejoin&&p.CurrentRangeRequiredPenaltyLoops>0)||
                e.CurrentRangeMotion?.Phase!=RangeTraversalPhase.PenaltyApproach)return false;
            for(int j=0;j<junctionCount;j++)if(rangeWorldTraffic.IsPenaltyRegion(j)&&Touches(stationary[priority],j)&&Touches(stationary[entering],j))return true;
            return false;
        }
        private bool OwnsJunction(int i)
        { if (!active[i]) return false; for (int j = 0; j < junctionCount; j++) if (Touches(stationary[i], j)) return true; return false; }
        private bool Safe(int i, bool recordInitialBlock = false)
        {
            if(rangeLayout!=null&&!FitsActualWidth(i,traces[i]))
            {
                if(recordInitialBlock) initialBlocks[i]=new PackMovementBlock(PackMovementBlockKind.Width);
                return false;
            }
            for(int j=0;j<Count;j++)if(j!=i&&active[j]&&Conflict(traces[i],i,processed[j]?traces[j]:stationary[j],j))
            {
                if(recordInitialBlock)
                {
                    var other=processed[j]?traces[j]:stationary[j];
                    bool world=rangeWorldTraffic!=null&&CanCheckCrossLoopWorldBodies(traces[i],other)&&
                        RangeWorldConflict(traces[i],i,other,j);
                    initialBlocks[i]=new PackMovementBlock(PackMovementBlockKind.Athlete,GetAthlete(j).AthleteId,world);
                }
                return false;
            }
            return true;
        }
        private bool Touches(Trace trace, int junction)
        {
            if(bypassJunctionTouchCache)return TouchesCore(trace,junction);
            byte known=trace.JunctionTouches[junction];
            if(known!=0)return known==2;
            bool result=TouchesCore(trace,junction);
            trace.JunctionTouches[junction]=(byte)(result?2:1);return result;
        }
        private bool TouchesCore(Trace trace, int junction)
        {
            if (junctions.TouchesForwardSweep(junction, trace.Start, 0, halfBody) || junctions.TouchesForwardSweep(junction, trace.End, 0, halfBody)) return true;
            if (trace.OldTail.Active && junctions.TouchesForwardSweep(junction, trace.OldTail.Point, 0, halfBody)) return true;
            if (trace.NewTail.Active && junctions.TouchesForwardSweep(junction, trace.NewTail.Point, 0, halfBody)) return true;
            for (int i = 0; i < trace.Count; i++) if (junctions.TouchesForwardSweep(junction, trace.Segments[i].Start, trace.Segments[i].TravelNanometers, halfBody)) return true;
            return false;
        }
        private bool Conflict(Trace a, int ai, Trace b, int bi)
        {
            if(rangeWorldTraffic!=null&&CanCheckCrossLoopWorldBodies(a,b)&&RangeWorldConflict(a,ai,b,bi))return true;
            bool departureSeparated=false;
            if(sharedApron!=null&&LocalExitLeaseConflict(a,b))
            {
                departureSeparated=rangeWorldTraffic!=null&&!RangeWorldConflict(a,ai,b,bi)||
                    sharedApron.UsesParallelPostShootDepartures&&TryPrepareApron(a,ai)&&TryPrepareApron(b,bi)&&
                    !sharedApron.DepartureBodiesConflict(a.ApronTrace,b.ApronTrace,apronScratch);
                if(!departureSeparated)return true;
                preparedApronPairProofs++;
            }
            bool worldContinuation=startContinuation!=null&&CanContinueStart(a,ai)&&CanContinueStart(b,bi);
            int passage=-1;
            for (int j = 0; j < junctionCount; j++) if (Touches(a, j) && Touches(b, j))
            {
                if(worldContinuation&&(j==startContinuation.JunctionIndex||
                    a.Start.LoopId==startContinuation.SourceLoopId&&a.End.LoopId==startContinuation.SourceLoopId&&
                    b.Start.LoopId==startContinuation.SourceLoopId&&b.End.LoopId==startContinuation.SourceLoopId))continue;
                if(completeStartCorridorProof&&worldContinuation)
                {
                    // Every branch in this guard is inside the two immutable captured
                    // corridors. The unchanged whole-interval body proof below decides
                    // contact even when one receipt crosses the approach/main seam.
                    bool covered=true;var region=junctionDefinition.Get(j);
                    for(int p=0;p<region.PortCount;p++)
                    {string loop=region.GetPort(p).Anchor.LoopId;if(loop!=startContinuation.SourceLoopId&&loop!=startContinuation.MainLoopId){covered=false;break;}}
                    if(covered)continue;
                }
                // A newly measured branch guard on the same start corridor
                // must not reject parallel grid rows. The existing full start
                // continuation body proof below still decides actual contact.
                if(worldContinuation&&a.Start.LoopId==b.Start.LoopId&&a.End.LoopId==a.Start.LoopId&&b.End.LoopId==a.Start.LoopId)
                {
                    int incidentPorts=0;var region=junctionDefinition.Get(j);
                    for(int p=0;p<region.PortCount;p++)if(region.GetPort(p).Anchor.LoopId==a.Start.LoopId)incidentPorts++;
                    if(incidentPorts==1)continue;
                }
                // A branch lease must not serialize later-lap traffic on one incident
                // loop. Admit only the unchanged full world bodies, including wraps
                // and incoming tails. A same-loop self-crossing has multiple ports
                // and retains its exclusive guard.
                if(rangeWorldTraffic!=null&&CanShareIncidentLoop(j,a,b)&&!RangeWorldConflict(a,ai,b,bi))
                {preparedApronPairProofs++;continue;}
                if(rangeWorldTraffic!=null&&(rangeWorldTraffic.Applies(j)||rangeWorldTraffic.CanShareCapturedBranch(j))&&!RangeWorldConflict(a,ai,b,bi))
                {preparedApronPairProofs++;continue;}
                int apron=sharedApron==null?-1:sharedApron.FindRegionForJunction(j);
                if(apron>=0&&LocalExitClearanceConflict(a,b,sharedApron.GetExitClearance(apron))&&!departureSeparated)
                {
                    departureSeparated=sharedApron.UsesParallelPostShootDepartures&&TryPrepareApron(a,ai)&&TryPrepareApron(b,bi)&&
                        !sharedApron.DepartureBodiesConflict(a.ApronTrace,b.ApronTrace,apronScratch);
                    if(!departureSeparated)return true;
                    preparedApronPairProofs++;
                }
                if(apron>=0&&sharedApron.IsEligible(apron)&&TryPrepareApron(a,ai)&&TryPrepareApron(b,bi)&&
                    !sharedApron.Conflicts(apron,a.ApronTrace,b.ApronTrace,apronScratch))
                {preparedApronPairProofs++;continue;}
                int candidate=directedPassages==null?-1:directedPassages.FindPassageForJunction(j);
                if(candidate<0||!CanContinuePassage(a,ai,candidate)||!CanContinuePassage(b,bi,candidate)||
                    passage>=0&&passage!=candidate)return true;
                passage=candidate;
            }
            int count = 2; grid[0] = 0; grid[1] = JunctionAdmissionSession.FractionParts;
            for (int i = 0; i < a.Count; i++) { Insert(a.Segments[i].StartFraction, ref count); Insert(a.Segments[i].EndFraction, ref count); }
            for (int i = 0; i < b.Count; i++) { Insert(b.Segments[i].StartFraction, ref count); Insert(b.Segments[i].EndFraction, ref count); }
            for (int i = 1; i < count; i++)
            {
                var aa = Sweep(a, ai, grid[i - 1], grid[i]); var bb = Sweep(b, bi, grid[i - 1], grid[i]);
                if(worldContinuation&&(startContinuation.Conflicts(aa,bb)||WorldGhostHit(a.OldTail,ai,bb)||WorldGhostHit(a.NewTail,ai,bb)||
                    WorldGhostHit(b.OldTail,bi,aa)||WorldGhostHit(b.NewTail,bi,aa)))return true;
                if(passage>=0&&(directedPassages.Conflicts(passage,aa,bb)||PassageGhostHit(passage,a.OldTail,ai,bb)||
                    PassageGhostHit(passage,a.NewTail,ai,bb)||PassageGhostHit(passage,b.OldTail,bi,aa)||PassageGhostHit(passage,b.NewTail,bi,aa)))return true;
                if (occupancy.QueryRelativeMove(aa, bb).Blocked || GhostHit(a.OldTail, ai, bb) || GhostHit(a.NewTail, ai, bb) ||
                    GhostHit(b.OldTail, bi, aa) || GhostHit(b.NewTail, bi, aa)) return true;
            }
            if(worldContinuation&&(WorldGhostPair(a.OldTail,ai,b.OldTail,bi)||WorldGhostPair(a.NewTail,ai,b.OldTail,bi)||
                WorldGhostPair(a.OldTail,ai,b.NewTail,bi)||WorldGhostPair(a.NewTail,ai,b.NewTail,bi)))return true;
            if(passage>=0&&(PassageGhostPair(passage,a.OldTail,ai,b.OldTail,bi)||PassageGhostPair(passage,a.NewTail,ai,b.OldTail,bi)||
                PassageGhostPair(passage,a.OldTail,ai,b.NewTail,bi)||PassageGhostPair(passage,a.NewTail,ai,b.NewTail,bi)))return true;
            return GhostPair(a.OldTail, ai, b.OldTail, bi) || GhostPair(a.NewTail, ai, b.OldTail, bi) ||
                GhostPair(a.OldTail, ai, b.NewTail, bi) || GhostPair(a.NewTail, ai, b.NewTail, bi);
        }
        private bool CanShareIncidentLoop(int junction,Trace a,Trace b)
        {
            string loop=a.Start.LoopId;
            return rangeWorldTraffic.CanShareSingleLoopIncidentPort(junction,a.Start)&&Within(a)&&Within(b);
            bool Within(Trace trace)
            {
                if(trace.Start.LoopId!=loop||trace.End.LoopId!=loop||
                    trace.OldTail.Active&&trace.OldTail.Point.LoopId!=loop||
                    trace.NewTail.Active&&trace.NewTail.Point.LoopId!=loop)return false;
                for(int i=0;i<trace.Count;i++)if(trace.Segments[i].Start.LoopId!=loop||trace.Segments[i].End.LoopId!=loop)return false;
                return true;
            }
        }
        private bool RangeWorldConflict(Trace a,int ai,Trace b,int bi)
        {
            // These are the field's exact staged receipts from the chosen path projections,
            // not reconstructed route-distance guesses. Final joint validation repeats them.
            int count=2;grid[0]=0;grid[1]=JunctionAdmissionSession.FractionParts;
            bool pose=UsesRangePoseEnvelope(a,ai)||UsesRangePoseEnvelope(b,bi);
            for(int i=0;i<a.Count;i++){Insert(a.Segments[i].StartFraction,ref count);Insert(a.Segments[i].EndFraction,ref count);}
            for(int i=0;i<b.Count;i++){Insert(b.Segments[i].StartFraction,ref count);Insert(b.Segments[i].EndFraction,ref count);}
            for(int i=1;i<count;i++)
            {
                var aa=Sweep(a,ai,grid[i-1],grid[i]);var bb=Sweep(b,bi,grid[i-1],grid[i]);
                if(rangeWorldTraffic.Conflicts(aa,bb,pose)||TailHit(a.OldTail,bb)||TailHit(a.NewTail,bb)||TailHit(b.OldTail,aa)||TailHit(b.NewTail,aa))return true;
            }
            return Pair(a.OldTail,b.OldTail)||Pair(a.NewTail,b.OldTail)||Pair(a.OldTail,b.NewTail)||Pair(a.NewTail,b.NewTail);
            bool TailHit(Tail tail,PhysicalOccupancySweep other)=>tail.Active&&rangeWorldTraffic.TailConflicts(tail.Point,tail.Lateral,other,tail.ConnectionIndex,pose);
            bool Pair(Tail first,Tail second)=>first.Active&&second.Active&&rangeWorldTraffic.TailPairConflicts(first.Point,first.Lateral,first.ConnectionIndex,second.Point,second.Lateral,second.ConnectionIndex,pose);
        }
        private bool CanCheckCrossLoopWorldBodies(Trace a,Trace b)
        {
            // Protect before a named point guard is reached: a rectangular body can
            // contact another physical branch several metres before their centre lines join.
            // Ordinary same-loop spacing and unknown geometry keep their existing rules.
            if(!Known(a)||!Known(b))return false;
            if(a.Start.LoopId!=b.Start.LoopId||a.End.LoopId!=b.End.LoopId||a.Start.LoopId!=a.End.LoopId||
                b.Start.LoopId!=b.End.LoopId)return true;
            return a.OldTail.Active||a.NewTail.Active||b.OldTail.Active||b.NewTail.Active;
            bool Known(Trace trace)
            {
                if(!rangeWorldTraffic.Contains(trace.Start)||!rangeWorldTraffic.Contains(trace.End)||
                    trace.OldTail.Active&&!rangeWorldTraffic.Contains(trace.OldTail.Point)||
                    trace.NewTail.Active&&!rangeWorldTraffic.Contains(trace.NewTail.Point))return false;
                for(int i=0;i<trace.Count;i++)if(!rangeWorldTraffic.Contains(trace.Segments[i].Start)||!rangeWorldTraffic.Contains(trace.Segments[i].End))return false;
                return true;
            }
        }
        private bool UsesRangePoseEnvelope(Trace trace,int index)
        {
            var actor=GetAthlete(index);bool shooting=actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
            if(ReferenceEquals(trace,traces[index]))shooting|=actor.PreviewRangeMotion(previews[index])?.Phase==RangeTraversalPhase.Shooting||
                previews[index].MovementActive&&previews[index].Movement.AtFiringLine;
            string mat=rangeLayout.GetPath(trace.ApronPath).MatAnchor.LoopId;
            return shooting||trace.OldTail.Active&&trace.OldTail.Point.LoopId==mat||trace.NewTail.Active&&trace.NewTail.Point.LoopId==mat;
        }
        private static bool LocalExitClearanceConflict(Trace a,Trace b,PhysicalApronExitClearance clearance)
        {
            if(clearance==null)return false;
            return ClaimsExitClearance(a,clearance)&&UsesMain(b)||ClaimsExitClearance(b,clearance)&&UsesMain(a);
            bool UsesMain(Trace trace)
            {
                if(trace.Start.LoopId==clearance.ThroughLoopId||trace.End.LoopId==clearance.ThroughLoopId||
                    trace.OldTail.Active&&trace.OldTail.Point.LoopId==clearance.ThroughLoopId||
                    trace.NewTail.Active&&trace.NewTail.Point.LoopId==clearance.ThroughLoopId)return true;
                for(int i=0;i<trace.Count;i++)if(trace.Segments[i].Start.LoopId==clearance.ThroughLoopId||trace.Segments[i].End.LoopId==clearance.ThroughLoopId)return true;
                return false;
            }
        }
        private static bool ClaimsExitClearance(Trace trace,PhysicalApronExitClearance clearance)
        {
            if(trace.OldTail.Active&&trace.OldTail.Point.LoopId==clearance.PrivateLoopId||
                trace.NewTail.Active&&trace.NewTail.Point.LoopId==clearance.PrivateLoopId)return true;
            if(trace.Start.LoopId!=clearance.PrivateLoopId)return false;
            long stop=checked(clearance.ClaimAfterStopMillimeters*SkiState.NanometersPerMillimeter);
            long release=checked(clearance.ReleaseAfterMillimeters*SkiState.NanometersPerMillimeter);
            if(trace.Start.LocalNanometers>stop&&trace.Start.LocalNanometers<=release)return true;
            return trace.Start.LocalNanometers<=stop&&trace.End.LoopId==clearance.PrivateLoopId&&trace.End.LocalNanometers>stop;
        }
        private bool LocalExitLeaseConflict(Trace a,Trace b)
        {
            string al=LeaseSource(a),bl=LeaseSource(b);
            int ac=sharedApron.ExitClearanceCount(al),bc=sharedApron.ExitClearanceCount(bl);
            for(int i=0;i<ac;i++)
            {
                var aa=sharedApron.GetExitClearance(al,i);if(!ClaimsExitClearance(a,aa))continue;
                for(int j=0;j<bc;j++)
                {
                    var bb=sharedApron.GetExitClearance(bl,j);
                    if(ClaimsExitClearance(b,bb)&&aa.ThroughLoopId==bb.ThroughLoopId&&
                        aa.ThroughGuardStartMillimeters<=bb.ThroughGuardEndMillimeters&&bb.ThroughGuardStartMillimeters<=aa.ThroughGuardEndMillimeters)return true;
                }
            }
            return false;
            string LeaseSource(Trace trace)
            {
                if(sharedApron.ExitClearanceCount(trace.Start.LoopId)>0)return trace.Start.LoopId;
                if(trace.OldTail.Active&&sharedApron.ExitClearanceCount(trace.OldTail.Point.LoopId)>0)return trace.OldTail.Point.LoopId;
                if(trace.NewTail.Active&&sharedApron.ExitClearanceCount(trace.NewTail.Point.LoopId)>0)return trace.NewTail.Point.LoopId;
                return null;
            }
        }
        private bool TryPrepareApron(Trace trace,int index)
        {
            // This is scratch only. Every final field pair calls Conflict again before commit.
            // Unknown approach/penalty geometry and unproven incoming tails retain exclusivity.
            if(!TryApronTail(trace.OldTail,trace.ApronPath,out var oldTail)||
                !TryApronTail(trace.NewTail,trace.ApronPath,out var newTail))return false;
            for(int i=0;i<trace.Count;i++)
            {
                var segment=trace.ApronSegments[i];
                if(segment.OnPenaltyLoop||segment.OnStartApproach)return false;
                trace.ApronMotions[i]=new PhysicalApronMotionV2(segment,trace.Segments[i]);
            }
            trace.ApronTrace.RebindPath(trace.ApronPath,pathProjections[index][trace.ApronPath]);
            var actor=GetAthlete(index);
            bool shooting=actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
            if(ReferenceEquals(trace,traces[index]))shooting|=actor.PreviewRangeMotion(previews[index])?.Phase==RangeTraversalPhase.Shooting||
                previews[index].MovementActive&&previews[index].Movement.AtFiringLine;
            if(trace.OldTail.Active&&trace.OldTail.Point.LoopId==rangeLayout.GetPath(trace.ApronPath).MatAnchor.LoopId||
                trace.NewTail.Active&&trace.NewTail.Point.LoopId==rangeLayout.GetPath(trace.ApronPath).MatAnchor.LoopId)shooting=true;
            trace.ApronTrace.SetAllBodyHeadings(shooting);
            try
            {
                trace.ApronTrace.Prepare(trace.StartRoute,trace.EndRoute,trace.Start,trace.End,trace.LateralStart,
                    trace.LateralEnd,oldTail,newTail,trace.ApronMotions.AsSpan(0,trace.Count));return true;
            }
            catch(ArgumentException){return false;}
            catch(InvalidOperationException){return false;}
        }
        private bool TryApronTail(Tail tail,int path,out PhysicalApronTailV2 result)
        {
            result=default;if(!tail.Active)return true;
            if(tail.ApronPath!=path||tail.ApronOccurrence<0||
                sharedApron.FindSeamOccurrence(path,tail.ConnectionIndex,tail.SeamRouteNanometers,tail.Point)!=tail.ApronOccurrence)return false;
            result=sharedApron.CreateTail(path,tail.ApronOccurrence,tail.Lateral);return true;
        }
        private bool CanContinueStart(Trace trace,int index)
        {
            var athlete=GetAthlete(index);
            if(athlete.Lifecycle.CompletedLaps!=0||athlete.RouteNanometers>startContinuation.MainPrefixNanometers||
                !startContinuation.Contains(trace.Start)||!startContinuation.Contains(trace.End))return false;
            if(trace.OldTail.Active&&trace.OldTail.Point.LoopId!=startContinuation.SourceLoopId||
                trace.NewTail.Active&&trace.NewTail.Point.LoopId!=startContinuation.SourceLoopId)return false;
            for(int i=0;i<trace.Count;i++)
            {
                var receipt=trace.Segments[i];
                if(!startContinuation.Contains(new PhysicalOccupancySweep(athlete.AthleteId,receipt.Start,receipt.TravelNanometers,
                    trace.LateralStart,trace.LateralEnd)))return false;
            }
            return true;
        }
        private bool WorldGhostHit(Tail tail,int index,PhysicalOccupancySweep other)
            =>tail.Active&&startContinuation.Conflicts(new PhysicalOccupancySweep(GetAthlete(index).AthleteId,tail.Point,0,tail.Lateral,tail.Lateral),other);
        private bool WorldGhostPair(Tail a,int ai,Tail b,int bi)=>a.Active&&b.Active&&WorldGhostHit(a,ai,
            new PhysicalOccupancySweep(GetAthlete(bi).AthleteId,b.Point,0,b.Lateral,b.Lateral));
        private bool CanContinuePassage(Trace trace,int index,int passage)
        {
            if(!passageSharing[passage])return false;
            int occurrence=directedPassages.FindOccurrence(passage,trace.StartRoute,trace.Start);
            if(occurrence<0||!directedPassages.Contains(passage,occurrence,trace.EndRoute,trace.End)||
                !PassageTailMatches(passage,occurrence,trace.OldTail)||!PassageTailMatches(passage,occurrence,trace.NewTail))return false;
            for(int i=0;i<trace.Count;i++)
            {
                var receipt=trace.Segments[i];
                if(!directedPassages.Contains(passage,occurrence,trace.RouteBefore[i],receipt.Start)||
                    !directedPassages.Contains(passage,occurrence,trace.RouteAfter[i],receipt.End)||
                    !directedPassages.Contains(passage,new PhysicalOccupancySweep(GetAthlete(index).AthleteId,receipt.Start,
                        receipt.TravelNanometers,trace.LateralStart,trace.LateralEnd)))return false;
            }
            return true;
        }
        private bool PassageTailMatches(int passage,int occurrence,Tail tail)
            =>!tail.Active||tail.ConnectionIndex==directedPassages.GetConnectionIndex(passage)&&tail.SourceSection==occurrence&&
                tail.Point.LoopId==directedPassages.SourceLoopId(passage)&&
                directedPassages.Contains(passage,occurrence,tail.SeamRouteNanometers,tail.Point);
        private bool PassageGhostHit(int passage,Tail tail,int index,PhysicalOccupancySweep other)
            =>tail.Active&&directedPassages.Conflicts(passage,new PhysicalOccupancySweep(GetAthlete(index).AthleteId,
                tail.Point,0,tail.Lateral,tail.Lateral),other);
        private bool PassageGhostPair(int passage,Tail a,int ai,Tail b,int bi)=>a.Active&&b.Active&&PassageGhostHit(passage,a,ai,
            new PhysicalOccupancySweep(GetAthlete(bi).AthleteId,b.Point,0,b.Lateral,b.Lateral));
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

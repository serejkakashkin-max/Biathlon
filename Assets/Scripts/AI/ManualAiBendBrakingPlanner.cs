using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ManualAiBendBrakingPlan
    {
        public double SpeedLimit {get;}
        public double BrakeFraction {get;}
        public double ShortLookaheadMeters {get;}
        public bool ReduceEffort=>BrakeFraction>0;
        internal ManualAiBendBrakingPlan(double limit,double fraction,double lookahead)
        {SpeedLimit=limit;BrakeFraction=fraction;ShortLookaheadMeters=lookahead;}
    }
    /// <summary>Ordered, already selected ordinary path geometry becomes braking
    /// intent before a tight bend. Querying a successor neither admits a phase
    /// nor changes velocity. The shared controlled drive owns all braking.</summary>
    public sealed class ManualAiBendBrakingPlanner
    {
        private readonly ManualRacePathCatalog catalog;
        private readonly TrackLoopDefinition[] loops;
        private readonly TrackSampler[] samplers;
        private readonly CorneringSystem cornering;
        private readonly CorneringDefinition corners;
        private readonly SkiPhysicsDefinition physics;
        private readonly ManualAiSteeringDefinition policy;
        private readonly ManualSkiSteeringDefinition steering;
        private readonly double bodyReach;
        public string ContextJson {get;}
        public ManualAiBendBrakingPlanner(TrackDefinition track,ManualRacePathCatalog catalog,
            SkiPhysicsDefinition physics,CorneringDefinition corners,ManualSkiSteeringDefinition steering,
            ManualAiSteeringDefinition policy,OccupancyDefinition body)
        {
            this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));
            this.physics=physics??throw new ArgumentNullException(nameof(physics));
            this.corners=corners??throw new ArgumentNullException(nameof(corners));
            this.steering=steering??throw new ArgumentNullException(nameof(steering));
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            if(track==null||body==null)throw new ArgumentNullException();
            cornering=new CorneringSystem(corners,physics);bodyReach=body.LongitudinalSeparation*.5+body.EdgeClearance;
            loops=new TrackLoopDefinition[catalog.Count];samplers=new TrackSampler[loops.Length];
            for(int i=0;i<loops.Length;i++)
            {loops[i]=track.GetLoop(catalog.Get(i).LoopId);samplers[i]=new TrackSampler(loops[i]);}
            ContextJson=ManualScopedAiMovement.Encode(new[]{"manual_ai_ordered_bend_braking_v1",
                ManualContextIdentity.Sha256(track.ToJson()),ManualContextIdentity.Sha256(catalog.ContextJson),
                physics.ToJson(),corners.ToJson(),steering.ToJson(),policy.ToJson(),body.ToJson(),
                "ordinary_selected_chain_only;closed_phase_stops_lookahead;shared_grip_and_yaw_speed_limits;existing_braking_cap;body_and_response_distance_reserve;intent_only;future_neutral_grip_is_not_weather_qualification"});
        }
        public ManualAiBendBrakingPlan Plan(ManualRacePathObservation at,double speed,int corneringSkill,int downhillSkill,
            double fatigue,double grip=1,double steeringResponse=1)
        {
            catalog.GetGeometryObservation(at);
            PhysiologyNumbers.RequireRange(speed,0,physics.MaximumSpeedMetersPerSecond,nameof(speed));
            PhysiologyNumbers.RequireRange(corneringSkill,0,100,nameof(corneringSkill));
            PhysiologyNumbers.RequireRange(downhillSkill,0,100,nameof(downhillSkill));
            PhysiologyNumbers.RequireRange(fatigue,0,1,nameof(fatigue));
            PhysiologyNumbers.RequireRange(grip,PhysiologyNumbers.MinimumPositive,PhysiologyNumbers.MaximumMagnitude,nameof(grip));
            PhysiologyNumbers.RequireRange(steeringResponse,PhysiologyNumbers.MinimumPositive,1,nameof(steeringResponse));
            double limit=physics.MaximumSpeedMetersPerSecond,shortLookahead=0,offset=0,fraction=0;
            int occurrence=at.OccurrenceIndex;long arc=at.PhysicalArcNanometers;
            while(offset<policy.LookaheadMaximumMeters)
            {
                var row=catalog.Get(occurrence);var loop=loops[occurrence];
                double local=(row.LocalStartNanometers+arc)/(double)SkiState.NanometersPerMeter;
                double end=Math.Min(row.LocalEndNanometers/(double)SkiState.NanometersPerMeter,
                    local+policy.LookaheadMaximumMeters-offset);
                for(int k=0;k<loop.SegmentCount;k++)
                {
                    var segment=loop.GetSegment(k);double from=Math.Max(local,segment.StartDistance.Meters);
                    double to=Math.Min(end,segment.StartDistance.Meters+segment.Length.Meters);
                    if(to<=from)continue;
                    double curve=Math.Abs(segment.CurvaturePerMeter);if(curve==0)continue;
                    var surface=samplers[occurrence].Sample(new TrackDistance((long)Math.Floor(from*1000)));
                    double safe=cornering.Evaluate(new SkiState(0,speed),surface,corneringSkill,downhillSkill,1,fatigue,grip).SafeSpeedMetersPerSecond;
                    safe=Math.Min(safe,steering.MaximumYawRate*steering.HighSpeedResponseMultiplier*steeringResponse/curve);
                    double distance=offset+from-local;
                    double available=Math.Max(0,distance-bodyReach-speed*policy.HeadingResponseSeconds);
                    limit=Math.Min(limit,Math.Sqrt(safe*safe+2*corners.MaximumBraking*available));
                    // A moving stopping-distance envelope needs deceleration
                    // before speed exceeds its bound. A speed-error servo alone
                    // follows that falling envelope too late.
                    if(speed>safe)
                        fraction=Math.Max(fraction,available==0?
                            Math.Min(1,(speed-safe)/(corners.MaximumBraking*policy.HeadingResponseSeconds)):
                            Math.Min(1,(speed*speed-safe*safe)/(2*available*corners.MaximumBraking)));
                    // Shorten before entering the bend, rather than waiting
                    // until an already cut chord projects onto its first edge.
                    double shortTarget=distance+policy.HeadingResponseSeconds*steering.MaximumYawRate*steeringResponse/curve;
                    if(shortTarget<policy.LookaheadBaseMeters)shortLookahead=shortLookahead==0?shortTarget:Math.Min(shortLookahead,shortTarget);
                }
                offset+=row.LengthNanometers/(double)SkiState.NanometersPerMeter-arc/(double)SkiState.NanometersPerMeter;
                if(row.End!=ManualRacePathEnd.Continue||row.Index+1>=catalog.BaseFlowCount)break;
                occurrence=row.Index+1;arc=0;
            }
            return new ManualAiBendBrakingPlan(limit,fraction,shortLookahead);
        }
    }
}

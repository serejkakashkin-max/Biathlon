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
        public double MinimumWidthMeters {get;}
        public bool ReduceEffort=>BrakeFraction>0;
        internal ManualAiBendBrakingPlan(double limit,double fraction,double lookahead,double width)
        {SpeedLimit=limit;BrakeFraction=fraction;ShortLookaheadMeters=lookahead;MinimumWidthMeters=width;}
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
        private readonly ManualSkiSteeringSystem commands;
        private readonly double bodyReach;
        private readonly MatBend[][] matBends;
        private readonly struct MatBend
        {
            public readonly double From,To,Curve;
            public MatBend(double from,double to,double curve){From=from;To=to;Curve=curve;}
        }
        public string ContextJson {get;}
        public ManualAiBendBrakingPlanner(TrackDefinition track,ManualRacePathCatalog catalog,
            SkiPhysicsDefinition physics,CorneringDefinition corners,ManualSkiSteeringDefinition steering,
            ManualAiSteeringDefinition policy,OccupancyDefinition body)
        {
            this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));
            this.physics=physics??throw new ArgumentNullException(nameof(physics));
            this.corners=corners??throw new ArgumentNullException(nameof(corners));
            this.steering=steering??throw new ArgumentNullException(nameof(steering));
            commands=new ManualSkiSteeringSystem(steering,1);
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            if(track==null||body==null)throw new ArgumentNullException();
            cornering=new CorneringSystem(corners,physics);bodyReach=body.LongitudinalSeparation*.5+body.EdgeClearance;
            loops=new TrackLoopDefinition[catalog.Count];samplers=new TrackSampler[loops.Length];
            for(int i=0;i<loops.Length;i++)
            {loops[i]=track.GetLoop(catalog.Get(i).LoopId);samplers[i]=new TrackSampler(loops[i]);}
            matBends=new MatBend[catalog.Count][];
            for(int i=0;i<catalog.Count;i++)
            {
                var row=catalog.Get(i);
                bool mat=row.End==ManualRacePathEnd.FiringLine||row.Kind==ManualRacePathKind.RangeExit&&
                    row.BoutIndex>=0&&row.Index==catalog.GetFiringOccurrence(row.BoutIndex)+1;
                if(!mat)continue;
                // Cold sampling at half a ski body's width resolves the actual
                // authored turn and straight departure, without per-tick lists.
                int n=(int)Math.Ceiling(row.LengthNanometers/(SkiState.NanometersPerMeter*body.LateralSeparation*.5));
                matBends[i]=new MatBend[n];
                for(int k=0;k<n;k++)
                {
                    long from=row.LengthNanometers*k/n,to=row.LengthNanometers*(k+1)/n;
                    catalog.TrySample(i,from,out var a);catalog.TrySample(i,to,out var b);
                    double chord=Math.Sqrt((b.X-a.X)*(b.X-a.X)+(b.Z-a.Z)*(b.Z-a.Z));
                    double turn=Math.Abs(Math.Atan2(a.ForwardX*b.ForwardZ-a.ForwardZ*b.ForwardX,
                        a.ForwardX*b.ForwardX+a.ForwardZ*b.ForwardZ));
                    matBends[i][k]=new MatBend((row.LocalStartNanometers+from)/(double)SkiState.NanometersPerMeter,
                        (row.LocalStartNanometers+to)/(double)SkiState.NanometersPerMeter,chord>PhysiologyNumbers.MinimumPositive?turn/chord:0);
                }
            }
            ContextJson=ManualScopedAiMovement.Encode(new[]{"manual_ai_ordered_bend_braking_v4_actual_mat_geometry_low_speed_yaw",
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
            double limit=physics.MaximumSpeedMetersPerSecond,shortLookahead=0,offset=0,fraction=0,width=catalog.SampleSurface(at).WidthMeters;
            int occurrence=at.OccurrenceIndex;long arc=at.PhysicalArcNanometers;
            while(offset<policy.LookaheadMaximumMeters)
            {
                var row=catalog.Get(occurrence);var loop=loops[occurrence];
                double local=(row.LocalStartNanometers+arc)/(double)SkiState.NanometersPerMeter;
                double end=Math.Min(row.LocalEndNanometers/(double)SkiState.NanometersPerMeter,
                    local+policy.LookaheadMaximumMeters-offset);
                if(matBends[occurrence]!=null)
                {
                    foreach(var bend in matBends[occurrence])
                    {
                        double from=Math.Max(local,bend.From),to=Math.Min(end,bend.To);
                        if(to<=from)continue;
                        var surface=samplers[occurrence].Sample(new TrackDistance((long)Math.Floor(from*1000)));
                        width=Math.Min(width,surface.WidthMeters);
                        Accumulate(surface.WithTravelGeometry(surface.Grade,bend.Curve),bend.Curve,from,true);
                    }
                }
                else for(int k=samplers[occurrence].Sample(new TrackDistance((long)Math.Floor(local*1000))).SegmentIndex;k<loop.SegmentCount;k++)
                {
                    var segment=loop.GetSegment(k);if(segment.StartDistance.Meters>=end)break;
                    double from=Math.Max(local,segment.StartDistance.Meters);
                    double to=Math.Min(end,segment.StartDistance.Meters+segment.Length.Meters);
                    if(to<=from)continue;
                    width=Math.Min(width,segment.WidthMeters);
                    var surface=samplers[occurrence].Sample(new TrackDistance((long)Math.Floor(from*1000)));
                    Accumulate(surface,Math.Abs(segment.CurvaturePerMeter),from,false);
                }
                void Accumulate(TrackSample surface,double curve,double from,bool privateMat)
                {
                    if(curve<=PhysiologyNumbers.MinimumPositive)return;
                    double safe=cornering.Evaluate(new SkiState(0,speed),surface,corneringSkill,downhillSkill,1,fatigue,grip).SafeSpeedMetersPerSecond;
                    double yaw=privateMat?Math.Abs(commands.GetRequest(ManualSkiCommand.SteeringUnits,speed,
                        PhysiologyNumbers.MaximumMagnitude,steeringResponse).RequestedYawRate):
                        steering.MaximumYawRate*steering.HighSpeedResponseMultiplier*steeringResponse;
                    safe=Math.Min(safe,yaw/curve);
                    // Decision margin under the same physical limits. Only the
                    // AI's recorded brake/effort intent changes, never capacity.
                    if(policy.BendSpeedSafetyFactor>0&&(row.Kind==ManualRacePathKind.BaseRoute||
                        row.Kind==ManualRacePathKind.StartApproach||row.Kind==ManualRacePathKind.PenaltyLoop))safe*=policy.BendSpeedSafetyFactor;
                    double distance=offset+from-local;
                    double available=Math.Max(0,distance-bodyReach-speed*policy.HeadingResponseSeconds);
                    double envelope=Math.Sqrt(safe*safe+2*corners.MaximumBraking*available);
                    limit=Math.Min(limit,envelope);
                    // A distant turn does not require coasting through the
                    // whole straight. Brake only when its stopping envelope
                    // is reached, retaining the body/response distance reserve.
                    if(speed>envelope)
                        fraction=Math.Max(fraction,available==0?
                            Math.Min(1,(speed-safe)/(corners.MaximumBraking*policy.HeadingResponseSeconds)):
                            Math.Min(1,(speed*speed-safe*safe)/(2*available*corners.MaximumBraking)));
                    double shortTarget=distance+policy.HeadingResponseSeconds*yaw/curve;
                    if(shortTarget<policy.LookaheadBaseMeters)shortLookahead=shortLookahead==0?shortTarget:Math.Min(shortLookahead,shortTarget);
                }
                offset+=row.LengthNanometers/(double)SkiState.NanometersPerMeter-arc/(double)SkiState.NanometersPerMeter;
                if(row.End!=ManualRacePathEnd.Continue||row.Index+1>=catalog.BaseFlowCount)break;
                occurrence=row.Index+1;arc=0;
            }
            return new ManualAiBendBrakingPlan(limit,fraction,shortLookahead,width);
        }
        /// <summary>AI explicitly manages a demanding ski deflection. The
        /// player's steering has no implicit brake; every brake still uses
        /// the same public force cap and is recorded as an ordinary intent.</summary>
        public double SteeringBrake(int input,double speed,double yaw,TrackSample surface,int corneringSkill,int downhillSkill,double fatigue)
        {
            double requested=commands.GetRequest(input,speed,PhysiologyNumbers.MaximumMagnitude).RequestedYawRate;
            double demand=Math.Max(Math.Abs(yaw),Math.Abs(requested));
            var turn=cornering.EvaluateWorldHeading(new SkiState(0,speed),surface,demand,corneringSkill,downhillSkill,1,fatigue);
            return turn.BrakingNewtons/(physics.MassKilograms*corners.MaximumBraking);
        }
    }
}

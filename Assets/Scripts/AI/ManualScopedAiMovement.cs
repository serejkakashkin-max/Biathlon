using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>AI route/line decisions become signed intent before the shared
    /// world step. Player intent is passed through unchanged. This bounded
    /// integration candidate cannot resume lateral-v1 movement after failure.</summary>
    public sealed class ManualScopedAiMovement
    {
        private readonly ManualScopedFieldStepper stepper;
        private readonly ManualAiSteeringController[] controllers;
        private readonly ManualRacePathCatalog[] catalogs;
        private readonly int[] steering;
        private readonly double[] passingLine;
        private readonly bool[] passing;
        private readonly int[] passingObstacle;
        private readonly int[] passingOccurrence;
        private readonly long[] stationaryTicks;
        private readonly double[] homeLine;
        private readonly ManualAiSteeringDefinition policy;
        private readonly ManualAiBendBrakingPlanner[] bends;
        private readonly double[] brakes;
        private readonly ManualAiObstacleCorridor corridor;
        public string ContextJson {get;}
        public ManualScopedAiMovement(ManualScopedFieldStepper stepper,ManualAiSteeringDefinition policy,
            ManualSkiSteeringDefinition physicalSteering)
        {
            this.stepper=stepper??throw new ArgumentNullException(nameof(stepper));
            if(policy==null||physicalSteering==null)throw new ArgumentNullException();
            this.policy=policy;
            corridor=new ManualAiObstacleCorridor(stepper.BodyDefinition);
            if(physicalSteering.ToJson()!=stepper.SteeringDefinitionJson)throw new ArgumentException("AI and movement must use the same physical steering definition.");
            int n=stepper.World.Count;controllers=new ManualAiSteeringController[n];catalogs=new ManualRacePathCatalog[n];steering=new int[n];
            bends=new ManualAiBendBrakingPlanner[n];brakes=new double[n];
            passingLine=new double[n];passing=new bool[n];
            passingObstacle=new int[n];
            passingOccurrence=new int[n];
            stationaryTicks=new long[n];
            homeLine=new double[n];for(int i=0;i<n;i++)homeLine[i]=stepper.Field.GetLateral(i);
            for(int i=0;i<n;i++)
            {catalogs[i]=stepper.World.GetCatalog(i);
                controllers[i]=new ManualAiSteeringController(catalogs[i].ProjectionGeometry,policy,physicalSteering);
                bends[i]=new ManualAiBendBrakingPlanner(stepper.Field.Track,catalogs[i],stepper.PhysicsDefinition,
                    stepper.CorneringDefinition,physicalSteering,policy,stepper.BodyDefinition);}
            var context=new string[n+5];
            context[0]="manual_scoped_ai_shared_clock_movement_v7_owned_range_pass_and_closing_brake";
            context[1]=stepper.ContextJson;context[2]=policy.ToJson();context[3]=physicalSteering.ToJson();
            context[4]="player_signed_input_unchanged;ai_target_not_lateral_teleport;stationary_obstacle_detection_from_policy_response_or_exact_owned_stationary_mat;full_rotated_body_corridor_and_achieved_velocity_forecast;candidate_line_query_excludes_old_velocity_forecast;moving_range_body_forecast_four_separating_axes_and_relative_achieved_velocity;only_observed_ahead_bodies;ordinary_racing_keeps_physical_contacts;pass_line_owned_by_current_occurrence;hold_pass_line_until_body_clear_or_occurrence_handoff;owned_gate_lookahead_cap;geometric_chord_sagitta_reserve;physical_pursuit_actual_velocity_and_shared_speed_response;ordered_bend_braking_and_short_chord_before_entry;common_physical_solver;no_legacy_fallback";
            for(int i=0;i<n;i++)context[i+5]=ManualContextIdentity.Sha256(bends[i].ContextJson);
            ContextJson=Encode(context);
        }
        public int GetSteering(int index)=>index>=0&&index<steering.Length?steering[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public double GetBrakeFraction(int index)=>index>=0&&index<brakes.Length?brakes[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public double PreferredLine(int index)
        {
            var cursor=stepper.World.GetCursor(index);var kind=catalogs[index].Get(cursor.OccurrenceIndex).Kind;
            return kind==ManualRacePathKind.BaseRoute||kind==ManualRacePathKind.StartApproach?homeLine[index]:0;
        }
        internal static string Encode(string[] values)
        {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,values);return Encoding.UTF8.GetString(stream.ToArray());}}
        public void Step(SprintAthleteInput[] inputs,double[] desiredLateral,int playerIndex=-1,int playerSteering=0)
        {
            if(inputs==null||desiredLateral==null||inputs.Length!=steering.Length||desiredLateral.Length!=steering.Length||
                playerIndex<-1||playerIndex>=steering.Length||playerSteering<-ManualSkiCommand.SteeringUnits||playerSteering>ManualSkiCommand.SteeringUnits)
                throw new ArgumentException("Exact roster and signed player intent required.");
            var world=stepper.World;
            for(int i=0;i<world.Count;i++)stationaryTicks[i]=world.GetPhysicalMotion(i).SpeedMetersPerSecond==0?stationaryTicks[i]+1:0;
            for(int i=0;i<steering.Length;i++)
            {
                brakes[i]=0;
                if(i==playerIndex){steering[i]=playerSteering;continue;}
                var actor=world.GetActor(i);var motion=world.GetPhysicalMotion(i);var catalog=catalogs[i];
                var observed=catalog.GetGeometryObservation(actor.Observation);
                var surface=catalog.SampleSurface(actor.Observation);double width=surface.WidthMeters;
                var cursor=world.GetCursor(i);var row=catalog.Get(cursor.OccurrenceIndex);
                var athlete=stepper.Field.GetAthlete(i);
                var bend=bends[i].Plan(actor.Observation,Math.Sqrt(actor.Velocity.X*actor.Velocity.X+actor.Velocity.Z*actor.Velocity.Z),
                    athlete.CorneringSkill,athlete.DownhillSkill,athlete.PhysiologyState.MuscularFatigue);
                brakes[i]=bend.BrakeFraction;
                if(bend.ReduceEffort)
                {
                    var prior=inputs[i];var ski=prior.Ski;
                    var reduced=new TimeTrialInput(new SimulationInput(ski.Effort.Tick,ski.Effort.AthleteId,0),
                        ski.Form,ski.LineQuality,ski.HeadwindMetersPerSecond,false,false,ski.TechniqueId,ski.MovementEfficiency);
                    inputs[i]=new SprintAthleteInput(reduced,prior.Aim,prior.Trigger,prior.HoldBreath,prior.Wind,prior.Pressure);
                }
                double chosen=desiredLateral[i],extent=Math.Max(0,width*.5-stepper.BodyCourseRadius);
                if(passing[i]&&passingOccurrence[i]!=row.Index)passing[i]=false;
                // Lateral-v1 grid holding is not a physical obstacle-avoidance
                // controller. Detect slower world bodies early and request yaw
                // toward a free side; do not wait for the skier to stop first.
                double horizon=Math.Min(policy.LookaheadMaximumMeters,policy.LookaheadBaseMeters+motion.SpeedMetersPerSecond*policy.LookaheadSpeedSeconds);
                // A chord aimed far along a bend cuts inside its offset curve.
                // Reserve the circular small-angle sagitta in the AI target,
                // in addition to full rotated-body clearance. This only changes
                // AI intent; actual ribbon admission remains independent.
                extent=Math.Max(0,extent-Math.Abs(surface.CurvaturePerMeter)*horizon*horizon/8);
                double separation=stepper.BodyEdgeMargin*2;
                int blocker=FindBlocker(i,actor.Observation.LateralMeters,horizon,true);
                bool obstructed=blocker>=0;
                double ahead=passing[i]?Ahead(i,passingObstacle[i]):0;
                if(passing[i]&&ahead< -stepper.BodyLongitudinalSeparation)passing[i]=false;
                if(obstructed)
                {
                    if(!passing[i]||Blocked(i,passingLine[i],horizon))
                    {
                        passing[i]=false;
                        double left=Math.Max(-extent,actor.Observation.LateralMeters-separation);
                        double right=Math.Min(extent,actor.Observation.LateralMeters+separation);
                        double first=actor.Observation.LateralMeters<=0?left:right,second=actor.Observation.LateralMeters<=0?right:left;
                        if(Math.Abs(first-actor.Observation.LateralMeters)>=separation&&!Blocked(i,first,horizon))
                        {passing[i]=true;passingLine[i]=first;passingObstacle[i]=blocker;passingOccurrence[i]=row.Index;}
                        else if(Math.Abs(second-actor.Observation.LateralMeters)>=separation&&!Blocked(i,second,horizon))
                        {passing[i]=true;passingLine[i]=second;passingObstacle[i]=blocker;passingOccurrence[i]=row.Index;}
                    }
                }
                if(passing[i])chosen=passingLine[i];
                if(obstructed)
                {
                    var other=world.GetActor(blocker);
                    if(other.Velocity.X!=0||other.Velocity.Z!=0)
                    {
                        double fraction=corridor.ClosingBrakeFraction(actor.Velocity,other.Velocity,
                            observed.ForwardX,observed.ForwardZ,stepper.CorneringDefinition,policy);
                        if(fraction>0)
                        {
                            brakes[i]=Math.Max(brakes[i],fraction);
                            var prior=inputs[i];var ski=prior.Ski;
                            inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(ski.Effort.Tick,ski.Effort.AthleteId,0),
                                ski.Form,ski.LineQuality,ski.HeadwindMetersPerSecond,false,false,ski.TechniqueId,ski.MovementEfficiency),
                                prior.Aim,prior.Trigger,prior.HoldBreath,prior.Wind,prior.Pressure);
                        }
                    }
                }
                // A controller target is only an intent. The complete body/
                // course/phase solver is still the sole movement authority.
                bool ordinaryLookahead=row.End==ManualRacePathEnd.Continue&&row.Index+1<catalog.BaseFlowCount;
                double gateDistance=(row.LengthNanometers-cursor.ObservedArcNanometers)/(double)SkiState.NanometersPerMeter;
                if(!ordinaryLookahead&&gateDistance==0){steering[i]=0;continue;}
                double available=gateDistance+(ordinaryLookahead?catalog.Get(row.Index+1).LengthNanometers/(double)SkiState.NanometersPerMeter:0);
                if(bend.ShortLookaheadMeters>0)available=Math.Min(available,bend.ShortLookaheadMeters);
                var plan=passing[i]?controllers[i].PlanPassing(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,
                    chosen,extent,Math.Min(available,Math.Max(stepper.BodyLongitudinalSeparation,Ahead(i,passingObstacle[i])-stepper.BodyLongitudinalSeparation))):
                    controllers[i].PlanOwnedTarget(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,chosen,extent,available);
                if(!plan.IsValid)throw new InvalidOperationException("Missing owned AI steering target for "+actor.Athlete);
                double targetTurn=Math.Atan2(observed.ForwardX*plan.Target.ForwardZ-observed.ForwardZ*plan.Target.ForwardX,
                    observed.ForwardX*plan.Target.ForwardX+observed.ForwardZ*plan.Target.ForwardZ);
                // Physical visual geometry can begin bending before the scalar
                // surface metadata changes. Reserve the chord's sagitta from
                // that actual future tangent as well, before committing intent.
                double geometricExtent=Math.Max(0,width*.5-stepper.BodyCourseRadius-Math.Abs(targetTurn)*plan.LookaheadMeters/8);
                if(geometricExtent<extent)
                {
                    extent=geometricExtent;
                    plan=passing[i]?controllers[i].PlanPassing(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,
                        chosen,extent,Math.Min(available,Math.Max(stepper.BodyLongitudinalSeparation,Ahead(i,passingObstacle[i])-stepper.BodyLongitudinalSeparation))):
                        controllers[i].PlanOwnedTarget(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,chosen,extent,available);
                    if(!plan.IsValid)throw new InvalidOperationException("Missing narrowed owned AI target for "+actor.Athlete);
                }
                steering[i]=passing[i]||Math.Abs(targetTurn)<=64*2.2204460492503131e-16?plan.SteeringInput:
                    controllers[i].PhysicalPursuitIntent(plan,actor.Pose,actor.Velocity);
            }
            if(!stepper.TryStep(inputs,steering,brakes))
                throw new InvalidOperationException("Manual field step rejected: "+stepper.LastStep.Status+" / "+
                    stepper.LastStep.WorldAttempt.Status+" / "+stepper.LastStep.WorldAttempt.Athlete+"; candidate clock not paid.");
            bool Blocked(int self,double lateral,double horizon)
                =>FindBlocker(self,lateral,horizon,false)>=0;
            double Ahead(int self,int other)
            {
                var a=world.GetActor(self);var b=world.GetActor(other);
                return (b.Pose.X-a.Pose.X)*a.Observation.ForwardX+(b.Pose.Z-a.Pose.Z)*a.Observation.ForwardZ;
            }
            int FindBlocker(int self,double lateral,double horizon,bool anticipate)
            {
                var a=world.GetActor(self);double fx=a.Observation.ForwardX,fz=a.Observation.ForwardZ;
                int closest=-1;double closestDistance=double.PositiveInfinity;
                for(int j=0;j<world.Count;j++)
                {
                    if(j==self)continue;var b=world.GetActor(j);
                    double dx=b.Pose.X-a.Pose.X,dz=b.Pose.Z-a.Pose.Z;
                    double ahead=dx*fx+dz*fz;
                    var otherRow=catalogs[j].Get(b.Observation.OccurrenceIndex);
                    bool stationaryMat=otherRow.End==ManualRacePathEnd.FiringLine&&b.Observation.PhysicalArcNanometers==otherRow.LengthNanometers&&
                        b.Velocity.X==0&&b.Velocity.Z==0&&b.Pose.YawRateRadiansPerSecond==0;
                    if(b.Velocity.X!=0||b.Velocity.Z!=0)
                    {
                        var selfRow=catalogs[self].Get(a.Observation.OccurrenceIndex);
                        if(ahead<=0||ahead>horizon||selfRow.Kind!=ManualRacePathKind.RangeApproach&&selfRow.Kind!=ManualRacePathKind.RangeExit)continue;
                        double speed=Math.Sqrt(a.Velocity.X*a.Velocity.X+a.Velocity.Z*a.Velocity.Z);
                        double forecastSeconds=policy.HeadingResponseSeconds+horizon/Math.Max(speed,stepper.BodyLongitudinalSeparation/policy.HeadingResponseSeconds);
                        var pose=anticipate?a.Pose:new ManualSkiHeadingState(a.Pose.X+(lateral-a.Observation.LateralMeters)*fz,
                            a.Pose.Z-(lateral-a.Observation.LateralMeters)*fx,a.Pose.HeadingRadians,a.Pose.YawRateRadiansPerSecond);
                        if(corridor.TryClosingInterval(pose,a.Velocity,b.Pose,b.Velocity,forecastSeconds,out double when))
                        {
                            double distance=Math.Max(stepper.BodyLongitudinalSeparation,when*speed);
                            if(distance<closestDistance){closest=j;closestDistance=distance;}
                        }
                        continue;
                    }
                    if(ahead<=0||ahead>horizon||!stationaryMat&&stationaryTicks[j]<Math.Ceiling(policy.HeadingResponseSeconds*world.TickFrequency))continue;
                    if(corridor.Overlaps(a.Pose,a.Velocity,fx,fz,a.Observation.LateralMeters,b.Pose,lateral,anticipate)&&ahead<closestDistance)
                    {closest=j;closestDistance=ahead;}
                }
                return closest;
            }
        }
    }
}

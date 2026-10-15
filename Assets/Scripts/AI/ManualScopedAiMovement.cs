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
        private readonly ManualAiSteeringController[] familyControllers;
        private readonly ManualAiBendBrakingPlanner[] familyBends;
        private readonly double[] brakes;
        private readonly ManualAiObstacleCorridor corridor;
        private readonly ManualAiSnowSteeringPlanner snowPlanner;
        private readonly CorneringSystem steeringGrip;
        private readonly PlayerCourseReturnSystem playerReturn;
        private readonly AiClimbRestartController[] climbRestart;
        public string ContextJson {get;}
        public ManualScopedAiMovement(ManualScopedFieldStepper stepper,ManualAiSteeringDefinition policy,
            ManualSkiSteeringDefinition physicalSteering,PlayerCourseAssistanceDefinition playerAssistance=null)
        {
            this.stepper=stepper??throw new ArgumentNullException(nameof(stepper));
            if(policy==null||physicalSteering==null)throw new ArgumentNullException();
            this.policy=policy;
            if(playerAssistance!=null)playerReturn=new PlayerCourseReturnSystem(playerAssistance,stepper.World.TickFrequency);
            corridor=new ManualAiObstacleCorridor(stepper.BodyDefinition);
            snowPlanner=new ManualAiSnowSteeringPlanner(stepper.BodyDefinition,physicalSteering,stepper.World.TickFrequency);
            steeringGrip=new CorneringSystem(stepper.CorneringDefinition,stepper.PhysicsDefinition);
            if(physicalSteering.ToJson()!=stepper.SteeringDefinitionJson)throw new ArgumentException("AI and movement must use the same physical steering definition.");
            int n=stepper.World.Count;controllers=new ManualAiSteeringController[n];catalogs=new ManualRacePathCatalog[n];steering=new int[n];
            if(policy.ClimbRestart!=null){climbRestart=new AiClimbRestartController[n];for(int i=0;i<n;i++)climbRestart[i]=new AiClimbRestartController(policy.ClimbRestart,stepper.World.TickFrequency);}
            bends=new ManualAiBendBrakingPlanner[n];brakes=new double[n];
            passingLine=new double[n];passing=new bool[n];
            passingObstacle=new int[n];
            passingOccurrence=new int[n];
            stationaryTicks=new long[n];
            homeLine=new double[n];for(int i=0;i<n;i++)homeLine[i]=stepper.Field.GetLateral(i);
            int choices=stepper.World.PathChoiceCount;
            familyControllers=new ManualAiSteeringController[choices];familyBends=new ManualAiBendBrakingPlanner[choices];
            for(int p=0;p<choices;p++)
            {
                var c=stepper.World.GetPathChoice(p);
                familyControllers[p]=new ManualAiSteeringController(c.ProjectionGeometry,policy,physicalSteering);
                familyBends[p]=new ManualAiBendBrakingPlanner(stepper.Field.Track,c,stepper.PhysicsDefinition,
                    stepper.CorneringDefinition,physicalSteering,policy,stepper.BodyDefinition);
            }
            for(int i=0;i<n;i++)
            {catalogs[i]=stepper.World.GetCatalog(i);
                if(choices>0){int p=FindPathChoice(catalogs[i]);controllers[i]=familyControllers[p];bends[i]=familyBends[p];}
                else
                {controllers[i]=new ManualAiSteeringController(catalogs[i].ProjectionGeometry,policy,physicalSteering);
                    bends[i]=new ManualAiBendBrakingPlanner(stepper.Field.Track,catalogs[i],stepper.PhysicsDefinition,
                        stepper.CorneringDefinition,physicalSteering,policy,stepper.BodyDefinition);}}
            var context=new string[n+choices+5];
            context[0]="manual_scoped_ai_shared_clock_movement_v27_bounded_course_pursuit";
            if(climbRestart!=null)context[0]="manual_scoped_ai_shared_clock_movement_v28_paid_climb_restart";
            context[1]=stepper.ContextJson;context[2]=policy.ToJson();context[3]=physicalSteering.ToJson()+";snow_planner_sha256:"+ManualContextIdentity.Sha256(snowPlanner.ContextJson);
            context[4]="player_signed_input_unchanged;ai_target_not_lateral_teleport;field_attested_scheduled_start_forecast;stationary_obstacle_detection_from_policy_response_or_exact_owned_stationary_mat;full_rotated_body_corridor_and_achieved_velocity_forecast;candidate_line_query_excludes_old_velocity_forecast;moving_range_body_forecast_four_separating_axes_and_relative_achieved_velocity;only_observed_ahead_bodies;ordinary_racing_keeps_physical_contacts;pass_line_owned_by_current_occurrence;hold_pass_line_until_body_clear_or_occurrence_handoff;owned_gate_lookahead_cap;owned_future_width_and_achieved_side_velocity_response_reserve;resource_family_common_to_private_cue_only;all_owned_paths_actual_snow_pursuit;bounded_mat_foot_placement_by_world_owner;shared_course_follows_moving_bodies_and_passes_stationary_ones;assigned_private_mat_center_target;current_private_exit_and_fixed_mass_passing_retained;geometric_chord_sagitta_reserve;physical_pursuit_actual_velocity_and_shared_speed_response_v2_actual_chord;ordered_bend_braking_and_short_chord_before_entry;common_physical_solver;no_legacy_fallback";
            for(int i=0;i<n;i++)context[i+5]=ManualContextIdentity.Sha256(bends[i].ContextJson);
            for(int p=0;p<choices;p++)context[n+p+5]=ManualContextIdentity.Sha256(familyBends[p].ContextJson);
            ContextJson=Encode(context);
            if(playerAssistance!=null)ContextJson=Encode(new[]{"manual_owner_release_heading_alignment_v2_no_course_following",ContextJson,playerAssistance.ToJson()});
        }
        public int GetSteering(int index)=>index>=0&&index<steering.Length?steering[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public double GetBrakeFraction(int index)=>index>=0&&index<brakes.Length?brakes[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public AiClimbRestartPhase GetClimbRestartPhase(int index)=>index>=0&&index<steering.Length?(climbRestart==null?AiClimbRestartPhase.Ordinary:climbRestart[index].Phase):throw new ArgumentOutOfRangeException(nameof(index));
        public double PreferredLine(int index)
        {
            var cursor=stepper.World.GetCursor(index);var kind=stepper.World.GetCatalog(index).Get(cursor.OccurrenceIndex).Kind;
            return kind==ManualRacePathKind.BaseRoute||kind==ManualRacePathKind.StartApproach?homeLine[index]:0;
        }
        internal static string Encode(string[] values)
        {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,values);return Encoding.UTF8.GetString(stream.ToArray());}}
        public void Step(SprintAthleteInput[] inputs,double[] desiredLateral,int playerIndex=-1,int playerSteering=0,double playerBrake=0)
        {
            if(inputs==null||desiredLateral==null||inputs.Length!=steering.Length||desiredLateral.Length!=steering.Length||
                playerIndex<-1||playerIndex>=steering.Length||playerSteering<-ManualSkiCommand.SteeringUnits||playerSteering>ManualSkiCommand.SteeringUnits)
                throw new ArgumentException("Exact roster and signed player intent required.");
            PhysiologyNumbers.RequireRange(playerBrake,0,1,nameof(playerBrake));
            var world=stepper.World;
            for(int i=0;i<world.Count;i++)
            {
                var c=world.GetCursor(i);var row=world.GetCatalog(i).Get(c.OccurrenceIndex);
                inputs[i]=inputs[i].WithRangeTransit(policy.RangeTransitSoftContacts&&i!=playerIndex&&
                    (row.Kind==ManualRacePathKind.RangeApproach||row.Kind==ManualRacePathKind.RangeExit));
            }
            // An automatic range turn replaces the previous manual heading.
            // Never resume a stale release target when controls are handed back.
            if(playerIndex<0)playerReturn?.Reset();
            // A paid resource choice changes only the future path. Use its cold
            // controller immediately; never keep intent for the previous mat.
            for(int i=0;i<world.Count;i++)
            {
                var current=world.GetCatalog(i);if(ReferenceEquals(current,catalogs[i]))continue;
                int p=FindPathChoice(current);catalogs[i]=current;controllers[i]=familyControllers[p];bends[i]=familyBends[p];
                passing[i]=false;
            }
            for(int i=0;i<world.Count;i++)stationaryTicks[i]=world.GetPhysicalMotion(i).SpeedMetersPerSecond==0?stationaryTicks[i]+1:0;
            for(int i=0;i<steering.Length;i++)
            {
                brakes[i]=0;
                if(i==playerIndex)
                {
                    climbRestart?[i].Step(false,0,0,0,inputs[i].Ski.Effort.RequestedEffort);
                    brakes[i]=playerBrake;
                    var body=world.GetActor(i);
                    steering[i]=playerReturn==null?playerSteering:playerReturn.Evaluate(playerSteering,body.Pose,
                        Math.Atan2(body.Observation.ForwardX,body.Observation.ForwardZ),world.GetPhysicalMotion(i).SpeedMetersPerSecond>0);
                    continue;
                }
                var actor=world.GetActor(i);var motion=world.GetPhysicalMotion(i);var catalog=catalogs[i];
                var observed=catalog.GetGeometryObservation(actor.Observation);
                var surface=catalog.SampleSurface(actor.Observation);double width=surface.WidthMeters;
                var cursor=world.GetCursor(i);var row=catalog.Get(cursor.OccurrenceIndex);
                var athlete=stepper.Field.GetAthlete(i);
                if(climbRestart!=null)
                {
                    var prior=inputs[i];var ski=prior.Ski;
                    int effort=climbRestart[i].Step(row.Kind==ManualRacePathKind.BaseRoute&&!athlete.HasFinished&&athlete.ActiveShooting==null,
                        actor.Observation.GradeAlong(actor.Pose),motion.SpeedMetersPerSecond,athlete.PhysiologyState.AnaerobicReserve,ski.Effort.RequestedEffort);
                    if(effort!=ski.Effort.RequestedEffort)
                        inputs[i]=prior.WithSki(new TimeTrialInput(new SimulationInput(ski.Effort.Tick,ski.Effort.AthleteId,effort),
                            ski.Form,ski.LineQuality,ski.HeadwindMetersPerSecond,false,false,ski.TechniqueId,ski.MovementEfficiency));
                }
                var bend=bends[i].Plan(actor.Observation,Math.Sqrt(actor.Velocity.X*actor.Velocity.X+actor.Velocity.Z*actor.Velocity.Z),
                    athlete.CorneringSkill,athlete.DownhillSkill,athlete.PhysiologyState.MuscularFatigue);
                width=Math.Min(width,bend.MinimumWidthMeters);
                brakes[i]=bend.BrakeFraction;
                if(bend.ReduceEffort)
                {
                    var prior=inputs[i];var ski=prior.Ski;
                    var reduced=new TimeTrialInput(new SimulationInput(ski.Effort.Tick,ski.Effort.AthleteId,0),
                        ski.Form,ski.LineQuality,ski.HeadwindMetersPerSecond,false,false,ski.TechniqueId,ski.MovementEfficiency);
                    inputs[i]=prior.WithSki(reduced);
                }
                double lateralReserve=Math.Abs(actor.Velocity.X*observed.ForwardZ-actor.Velocity.Z*observed.ForwardX)*policy.HeadingResponseSeconds;
                double chosen=desiredLateral[i],extent=Math.Max(0,width*.5-stepper.BodyCourseRadius-lateralReserve);
                bool privateMat=row.LoopId==athlete.RangePath?.MatAnchor.LoopId&&
                    (row.Kind==ManualRacePathKind.RangeApproach||row.Kind==ManualRacePathKind.RangeExit);
                // Shared-aisle lines and passing offsets must end at the fork.
                // The assigned mat's curve is followed through its centre.
                if(privateMat){chosen=0;passing[i]=false;}
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
                int blocker=FindBlocker(i,actor.Observation.LateralMeters,horizon,true,out double closingSeconds);
                bool obstructed=blocker>=0;
                double ahead=passing[i]?Ahead(i,passingObstacle[i]):0;
                if(passing[i]&&ahead< -stepper.BodyLongitudinalSeparation)passing[i]=false;
                bool movingSharedBlocker=obstructed&&(row.Kind==ManualRacePathKind.BaseRoute||row.Kind==ManualRacePathKind.StartApproach)&&
                    (world.GetActor(blocker).Velocity.X!=0||world.GetActor(blocker).Velocity.Z!=0);
                if(obstructed&&!movingSharedBlocker&&!privateMat)
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
                if(row.Kind==ManualRacePathKind.RangeApproach&&!privateMat)
                    chosen=Math.Max(chosen,policy.RangeApproachMinimumLateral);
                if(obstructed&&!passing[i]&&row.Kind!=ManualRacePathKind.RangeExit&&
                    !(policy.RangeTransitSoftContacts&&row.Kind==ManualRacePathKind.RangeApproach))
                {
                    var other=world.GetActor(blocker);
                    if(other.Velocity.X!=0||other.Velocity.Z!=0)
                    {
                        double fraction=corridor.ClosingBrakeFraction(actor.Velocity,other.Velocity,
                            observed.ForwardX,observed.ForwardZ,stepper.CorneringDefinition,policy,closingSeconds);
                        if(fraction>0)
                        {
                            brakes[i]=Math.Max(brakes[i],fraction);
                            var prior=inputs[i];var ski=prior.Ski;
                            inputs[i]=prior.WithSki(new TimeTrialInput(new SimulationInput(ski.Effort.Tick,ski.Effort.AthleteId,0),
                                ski.Form,ski.LineQuality,ski.HeadwindMetersPerSecond,false,false,ski.TechniqueId,ski.MovementEfficiency));
                        }
                    }
                }
                // A controller target is only an intent. The complete body/
                // course/phase solver is still the sole movement authority.
                bool ordinaryLookahead=row.End==ManualRacePathEnd.Continue&&row.Index+1<catalog.BaseFlowCount;
                // Stay in the shared aisle until the owned mat fork. Looking
                // across a neighbouring mat's future hairpin cut its surface.
                if(ordinaryLookahead&&row.Kind==ManualRacePathKind.RangeApproach&&!privateMat&&
                    catalog.Get(row.Index+1).LoopId==athlete.RangePath?.MatAnchor.LoopId)ordinaryLookahead=false;
                double gateDistance=(row.LengthNanometers-cursor.ObservedArcNanometers)/(double)SkiState.NanometersPerMeter;
                if(!ordinaryLookahead&&gateDistance==0){steering[i]=0;continue;}
                double available=gateDistance+(ordinaryLookahead?catalog.Get(row.Index+1).LengthNanometers/(double)SkiState.NanometersPerMeter:0);
                // A long chord across the private departure's two bends points
                // behind the skier. Follow the local turn before the shared aisle.
                if(privateMat&&row.Kind==ManualRacePathKind.RangeExit&&policy.PrivateDepartureLookahead>0)
                    available=Math.Min(available,policy.PrivateDepartureLookahead);
                if(bend.ShortLookaheadMeters>0)available=Math.Min(available,bend.ShortLookaheadMeters);
                var plan=passing[i]?controllers[i].PlanPassing(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,
                    chosen,extent,Math.Min(available,Math.Max(stepper.BodyLongitudinalSeparation,Ahead(i,passingObstacle[i])-stepper.BodyLongitudinalSeparation))):
                    controllers[i].PlanOwnedTarget(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,chosen,extent,available);
                if(!plan.IsValid)throw new InvalidOperationException("Missing owned AI steering target for "+actor.Athlete);
                // A shared-aisle passing offset cannot be carried into the
                // selected mat's exclusive turn. Aim for its centre before the
                // occurrence handoff, through the same bounded yaw controller.
                if(world.PathChoiceCount>0&&passing[i]&&row.Kind==ManualRacePathKind.RangeApproach&&
                    row.LoopId!=athlete.RangePath.MatAnchor.LoopId&&plan.Target.LoopId==athlete.RangePath.MatAnchor.LoopId)
                {
                    passing[i]=false;chosen=0;
                    plan=controllers[i].PlanOwnedTarget(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,chosen,extent,available);
                    if(!plan.IsValid)throw new InvalidOperationException("Missing selected private AI target for "+actor.Athlete);
                }
                double targetTurn=Math.Atan2(observed.ForwardX*plan.Target.ForwardZ-observed.ForwardZ*plan.Target.ForwardX,
                    observed.ForwardX*plan.Target.ForwardX+observed.ForwardZ*plan.Target.ForwardZ);
                // Physical visual geometry can begin bending before the scalar
                // surface metadata changes. Reserve the chord's sagitta from
                // that actual future tangent as well, before committing intent.
                double geometricExtent=Math.Max(0,width*.5-stepper.BodyCourseRadius-lateralReserve-Math.Abs(targetTurn)*plan.LookaheadMeters/8);
                if(geometricExtent<extent)
                {
                    extent=geometricExtent;
                    plan=passing[i]?controllers[i].PlanPassing(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,
                        chosen,extent,Math.Min(available,Math.Max(stepper.BodyLongitudinalSeparation,Ahead(i,passingObstacle[i])-stepper.BodyLongitudinalSeparation))):
                        controllers[i].PlanOwnedTarget(actor.Pose,observed,motion.SpeedMetersPerSecond,ordinaryLookahead,chosen,extent,available);
                    if(!plan.IsValid)throw new InvalidOperationException("Missing narrowed owned AI target for "+actor.Athlete);
                }
                // A curved penalty pass must steer from achieved velocity too;
                // a current bearing alone cuts inside the physical circuit.
                // The assigned firing mat is a bounded physical position.
                // Keep its centre as the final target; carrying an arbitrary
                // parallel aisle offset can aim beyond the firing line.
                steering[i]=(passing[i]&&row.Kind!=ManualRacePathKind.PenaltyLoop)||Math.Abs(targetTurn)<=64*2.2204460492503131e-16?plan.SteeringInput:
                    controllers[i].PhysicalPursuitIntent(plan,actor.Pose,actor.Velocity);
                // Recover a missed mat or exit target with bounded steering.
                // Once a penalty target is behind the skis, sin(pi) pure pursuit
                // cannot turn a stopped skier back. Retain ordinary pursuit on
                // forward targets, but recover through the same bounded control.
                bool ordinaryRecovery=row.Kind!=ManualRacePathKind.PenaltyLoop||policy.RecoverBehindPenaltyTarget;
                double pursuitYaw=controllers[i].PhysicalPursuitYaw(plan,actor.Pose,actor.Velocity,ordinaryRecovery);
                if(policy.RangeTransitSoftContacts&&privateMat&&row.Kind==ManualRacePathKind.RangeExit)
                    pursuitYaw=plan.HeadingErrorRadians/policy.HeadingResponseSeconds-policy.YawRateFeedback*actor.Pose.YawRateRadiansPerSecond;
                if(row.Kind!=ManualRacePathKind.FinishRunout)
                {
                    double grip=steeringGrip.EvaluateWorldHeading(new SkiState(0,motion.SpeedMetersPerSecond),surface,0,
                        athlete.CorneringSkill,athlete.DownhillSkill,inputs[i].Ski.LineQuality,athlete.PhysiologyState.MuscularFatigue).LateralAccelerationLimit;
                    // Ordinary course pursuit already requests a bounded physical
                    // deflection. Minimizing one-tick passive yaw error instead can
                    // select near-transverse skis for an unattainable correction,
                    // repeatedly shedding speed. It also omits paid active pivoting.
                    // Keep the pursuit command; shared grip/drive/contact still own
                    // its achieved turn and cost. Private mat precision retains the inverse.
                    bool ordinaryCourse=row.Kind==ManualRacePathKind.PenaltyLoop||row.Kind==ManualRacePathKind.BaseRoute||row.Kind==ManualRacePathKind.StartApproach;
                    if(!(policy.RecoverLowSpeedCourseSteering&&ordinaryCourse))
                        steering[i]=snowPlanner.Plan(new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,stepper.PhysicsDefinition.MassKilograms),
                            new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),pursuitYaw,grip);
                }
                if(policy.BrakeForSteeringDemand&&(row.Kind==ManualRacePathKind.BaseRoute||row.Kind==ManualRacePathKind.StartApproach||row.Kind==ManualRacePathKind.PenaltyLoop))
                    brakes[i]=Math.Max(brakes[i],bends[i].SteeringBrake(steering[i],motion.SpeedMetersPerSecond,actor.Pose.YawRateRadiansPerSecond,
                        surface.WithTravelGeometry(observed.GradeAlong(actor.Pose),0),athlete.CorneringSkill,athlete.DownhillSkill,athlete.PhysiologyState.MuscularFatigue));

            }
            if(!stepper.TryStep(inputs,steering,brakes))
                throw new InvalidOperationException("Manual field step rejected: "+stepper.LastStep.Status+" / "+
                    stepper.LastStep.WorldAttempt.Status+" / "+stepper.LastStep.WorldAttempt.Athlete+"; candidate clock not paid. "+FailureDetails());
            bool Blocked(int self,double lateral,double horizon)
                =>FindBlocker(self,lateral,horizon,false,out _)>=0;
            double Ahead(int self,int other)
            {
                var a=world.GetActor(self);var b=world.GetActor(other);
                return (b.Pose.X-a.Pose.X)*a.Observation.ForwardX+(b.Pose.Z-a.Pose.Z)*a.Observation.ForwardZ;
            }
            int FindBlocker(int self,double lateral,double horizon,bool anticipate,out double closingSeconds)
            {
                closingSeconds=0;
                var a=world.GetActor(self);double fx=a.Observation.ForwardX,fz=a.Observation.ForwardZ;
                int closest=-1;double closestDistance=double.PositiveInfinity;
                for(int j=0;j<world.Count;j++)
                {
                    if(j==self)continue;
                    if(!world.IsPhysicalParticipant(j,world.Tick.Next()))
                    {
                        double selfSpeed=world.GetPhysicalMotion(self).SpeedMetersPerSecond;
                        double preview=policy.HeadingResponseSeconds+horizon/Math.Max(selfSpeed,stepper.BodyLongitudinalSeparation/policy.HeadingResponseSeconds);
                        long until=stepper.Field.GetAthlete(j).ScheduledStart.Value-world.Tick.Value;
                        if(until<0||until>preview*world.TickFrequency)continue;
                    }
                    var b=world.GetActor(j);
                    double dx=b.Pose.X-a.Pose.X,dz=b.Pose.Z-a.Pose.Z;
                    double ahead=dx*fx+dz*fz;
                    var otherRow=catalogs[j].Get(b.Observation.OccurrenceIndex);
                    bool stationaryMat=otherRow.End==ManualRacePathEnd.FiringLine&&b.Observation.PhysicalArcNanometers==otherRow.LengthNanometers&&
                        b.Velocity.X==0&&b.Velocity.Z==0&&b.Pose.YawRateRadiansPerSecond==0;
                    if(b.Velocity.X!=0||b.Velocity.Z!=0)
                    {
                        var selfRow=catalogs[self].Get(a.Observation.OccurrenceIndex);
                        if(ahead<=0||ahead>horizon)continue;
                        double speed=Math.Sqrt(a.Velocity.X*a.Velocity.X+a.Velocity.Z*a.Velocity.Z);
                        double forecastSeconds=policy.HeadingResponseSeconds+horizon/Math.Max(speed,stepper.BodyLongitudinalSeparation/policy.HeadingResponseSeconds);
                        var pose=anticipate?a.Pose:new ManualSkiHeadingState(a.Pose.X+(lateral-a.Observation.LateralMeters)*fz,
                            a.Pose.Z-(lateral-a.Observation.LateralMeters)*fx,a.Pose.HeadingRadians,a.Pose.YawRateRadiansPerSecond);
                        if(corridor.TryClosingInterval(pose,a.Velocity,b.Pose,b.Velocity,forecastSeconds,out double when))
                        {
                            double distance=Math.Max(stepper.BodyLongitudinalSeparation,when*speed);
                            if(distance<closestDistance){closest=j;closestDistance=distance;closingSeconds=when;}
                        }
                        continue;
                    }
                    if(ahead<=0||ahead>horizon||!stationaryMat&&stationaryTicks[j]<Math.Ceiling(policy.HeadingResponseSeconds*world.TickFrequency))continue;
                    if(corridor.Overlaps(a.Pose,a.Velocity,fx,fz,a.Observation.LateralMeters,b.Pose,lateral,anticipate)&&ahead<closestDistance)
                    {closest=j;closestDistance=ahead;closingSeconds=0;}
                }
                return closest;
            }
        }
        private string FailureDetails()
        {
            int index=-1;for(int i=0;i<stepper.World.Count;i++)
                if(stepper.World.GetActor(i).Athlete.Equals(stepper.LastStep.WorldAttempt.Athlete)){index=i;break;}
            if(index<0)return "actor_not_found";
            var a=stepper.LastRejectedWorkingActor;var source=stepper.World.GetActor(index);var c=stepper.World.GetCursor(index);
            var d=stepper.World.GetRibbonDiagnostics(index);
            return FormattableString.Invariant($"tick={stepper.LastStep.Tick.Value};actor={index};occurrence={c.OccurrenceIndex};loop={source.Observation.LoopId};arc={c.ObservedArcNanometers};local={source.Observation.LocalMeters:R};seconds={stepper.LastStep.ProvisionalSeconds:R};pieces={stepper.LastStep.Intervals};course={stepper.LastStep.WorldAttempt.Course.Status};responses={stepper.World.CourseEdgeResponses};actor_responses={stepper.World.GetCourseEdgeResponses(index)};source_gap={d.LastStartClearance:R};minimum_gap={d.LastMinimumSampledClearance:R};source_pose={source.Pose.X:R},{source.Pose.Z:R},{source.Pose.HeadingRadians:R};working_pose={a.Pose.X:R},{a.Pose.Z:R},{a.Pose.HeadingRadians:R};velocity={a.Velocity.X:R},{a.Velocity.Z:R};spin={a.Pose.YawRateRadiansPerSecond:R};normal={stepper.World.LastCourseNormalX:R},{stepper.World.LastCourseNormalZ:R};lever={stepper.World.LastCourseAngularLever:R}");
        }
        private int FindPathChoice(ManualRacePathCatalog catalog)
        {
            for(int p=0;p<stepper.World.PathChoiceCount;p++)if(ReferenceEquals(catalog,stepper.World.GetPathChoice(p)))return p;
            throw new InvalidOperationException("AI catalog must be an attested cold resource choice.");
        }
    }
}

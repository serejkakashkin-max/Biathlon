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
        private readonly long[] stationaryTicks;
        private readonly double[] homeLine;
        private readonly ManualAiSteeringDefinition policy;
        public string ContextJson {get;}
        public ManualScopedAiMovement(ManualScopedFieldStepper stepper,ManualAiSteeringDefinition policy,
            ManualSkiSteeringDefinition physicalSteering)
        {
            this.stepper=stepper??throw new ArgumentNullException(nameof(stepper));
            if(policy==null||physicalSteering==null)throw new ArgumentNullException();
            this.policy=policy;
            if(physicalSteering.ToJson()!=stepper.SteeringDefinitionJson)throw new ArgumentException("AI and movement must use the same physical steering definition.");
            int n=stepper.World.Count;controllers=new ManualAiSteeringController[n];catalogs=new ManualRacePathCatalog[n];steering=new int[n];
            passingLine=new double[n];passing=new bool[n];
            passingObstacle=new int[n];
            stationaryTicks=new long[n];
            homeLine=new double[n];for(int i=0;i<n;i++)homeLine[i]=stepper.Field.GetLateral(i);
            for(int i=0;i<n;i++)
            {catalogs[i]=stepper.World.GetCatalog(i);
                controllers[i]=new ManualAiSteeringController(catalogs[i].ProjectionGeometry,policy,physicalSteering);}
            ContextJson=Encode(new[]{"manual_scoped_ai_shared_clock_movement_v3_physical_pursuit",stepper.ContextJson,
                policy.ToJson(),physicalSteering.ToJson(),"player_signed_input_unchanged;ai_target_not_lateral_teleport;stationary_obstacle_detection_from_policy_response;hold_pass_line_until_body_clear;owned_gate_lookahead_cap;geometric_chord_sagitta_reserve;physical_pursuit_actual_velocity_and_shared_speed_response;common_physical_solver;no_legacy_fallback"});
        }
        public int GetSteering(int index)=>index>=0&&index<steering.Length?steering[index]:throw new ArgumentOutOfRangeException(nameof(index));
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
                if(i==playerIndex){steering[i]=playerSteering;continue;}
                var actor=world.GetActor(i);var motion=world.GetPhysicalMotion(i);var catalog=catalogs[i];
                var observed=catalog.GetGeometryObservation(actor.Observation);
                var surface=catalog.SampleSurface(actor.Observation);double width=surface.WidthMeters;
                var cursor=world.GetCursor(i);var row=catalog.Get(cursor.OccurrenceIndex);
                double chosen=desiredLateral[i],extent=Math.Max(0,width*.5-stepper.BodyCourseRadius);
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
                int blocker=FindBlocker(i,actor.Observation.LateralMeters,horizon);
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
                        {passing[i]=true;passingLine[i]=first;passingObstacle[i]=blocker;}
                        else if(Math.Abs(second-actor.Observation.LateralMeters)>=separation&&!Blocked(i,second,horizon))
                        {passing[i]=true;passingLine[i]=second;passingObstacle[i]=blocker;}
                    }
                }
                if(passing[i])chosen=passingLine[i];
                // A controller target is only an intent. The complete body/
                // course/phase solver is still the sole movement authority.
                bool ordinaryLookahead=row.End==ManualRacePathEnd.Continue&&row.Index+1<catalog.BaseFlowCount;
                double gateDistance=(row.LengthNanometers-cursor.ObservedArcNanometers)/(double)SkiState.NanometersPerMeter;
                if(!ordinaryLookahead&&gateDistance==0){steering[i]=0;continue;}
                double available=gateDistance+(ordinaryLookahead?catalog.Get(row.Index+1).LengthNanometers/(double)SkiState.NanometersPerMeter:0);
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
            if(!stepper.TryStep(inputs,steering))
                throw new InvalidOperationException("Manual field step rejected: "+stepper.LastStep.Status+" / "+
                    stepper.LastStep.WorldAttempt.Status+" / "+stepper.LastStep.WorldAttempt.Athlete+"; candidate clock not paid.");
            bool Blocked(int self,double lateral,double horizon)
                =>FindBlocker(self,lateral,horizon)>=0;
            double Ahead(int self,int other)
            {
                var a=world.GetActor(self);var b=world.GetActor(other);
                return (b.Pose.X-a.Pose.X)*a.Observation.ForwardX+(b.Pose.Z-a.Pose.Z)*a.Observation.ForwardZ;
            }
            int FindBlocker(int self,double lateral,double horizon)
            {
                var a=world.GetActor(self);double fx=a.Observation.ForwardX,fz=a.Observation.ForwardZ;
                int closest=-1;double closestDistance=double.PositiveInfinity;
                for(int j=0;j<world.Count;j++)
                {
                    if(j==self)continue;var b=world.GetActor(j);
                    double dx=b.Pose.X-a.Pose.X,dz=b.Pose.Z-a.Pose.Z;
                    double ahead=dx*fx+dz*fz;
                    if(ahead<=0||ahead>horizon||stationaryTicks[j]<Math.Ceiling(policy.HeadingResponseSeconds*world.TickFrequency))continue;
                    double line=a.Observation.LateralMeters+dx*fz-dz*fx;
                    if(Math.Abs(line-lateral)<stepper.BodyEdgeMargin*2&&ahead<closestDistance){closest=j;closestDistance=ahead;}
                }
                return closest;
            }
        }
    }
}

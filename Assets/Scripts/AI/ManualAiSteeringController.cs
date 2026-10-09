using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ManualAiSteeringPlan
    {
        public bool IsValid {get;}
        public int SteeringInput {get;}
        public double HeadingErrorRadians {get;}
        public double LookaheadMeters {get;}
        public ManualRouteTarget Target {get;}
        public double TargetLateralMeters {get;}
        internal ManualAiSteeringPlan(int steering,double error,double lookahead,ManualRouteTarget target,double lateral)
        {IsValid=true;SteeringInput=steering;HeadingErrorRadians=error;LookaheadMeters=lookahead;Target=target;
         TargetLateralMeters=lateral;}
    }

    /// <summary>Observed route geometry becomes signed yaw intent for AI only.
    /// A failed target query grants no steering or movement. Player input is never
    /// passed through this controller, and both actors use the same physical solver.</summary>
    public sealed class ManualAiSteeringController
    {
        private readonly ManualRouteProjectionMap route;
        private readonly ManualAiSteeringDefinition policy;
        private readonly double maximumYawRate;
        private readonly double lowSpeedReference;
        private readonly ManualSkiSteeringDefinition physicalCommands;
        public string ContextJson {get;}
        public ManualAiSteeringController(ManualRouteProjectionMap route,ManualAiSteeringDefinition policy,
            ManualSkiSteeringDefinition physicalSteering)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            if(physicalSteering==null)throw new ArgumentNullException(nameof(physicalSteering));
            maximumYawRate=physicalSteering.MaximumYawRate;
            lowSpeedReference=physicalSteering.MinimumSpeedForGripLimit;
            physicalCommands=physicalSteering;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{"manual_ai_signed_yaw_intent_v5_parallel_endpoint",
                    route.ContextJson,policy.ToJson(),physicalSteering.ToJson(),
                    "manual_ai_bounded_pass_line_yaw_v2;caller_width;owned_gate_lookahead_cap;fitting_parallel_endpoint_intent;physical_pursuit_actual_velocity_and_shared_speed_response_v2_actual_chord;body_course_sweep_required"});
                ContextJson=Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public ManualAiSteeringPlan Plan(ManualSkiHeadingState pose,ManualRouteProjection observation,
            double planarSpeedMetersPerSecond,bool admitNextSection)
            =>PlanCore(pose,observation,planarSpeedMetersPerSecond,admitNextSection,0);

        /// <summary>Steer AI toward a caller-selected passing line through yaw.
        /// The usable half-width is supplied by the course/body policy. This
        /// intent neither teleports laterally nor grants body/course admission.</summary>
        public ManualAiSteeringPlan Plan(ManualSkiHeadingState pose,ManualRouteProjection observation,
            double planarSpeedMetersPerSecond,bool admitNextSection,
            double desiredLateralMeters,double usableHalfWidthMeters)
        {
            PhysiologyNumbers.RequireRange(desiredLateralMeters,-PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude,nameof(desiredLateralMeters));
            PhysiologyNumbers.RequireRange(usableHalfWidthMeters,0,
                PhysiologyNumbers.MaximumMagnitude,nameof(usableHalfWidthMeters));
            double target=Math.Max(-usableHalfWidthMeters,
                Math.Min(usableHalfWidthMeters,desiredLateralMeters));
            return PlanCore(pose,observation,planarSpeedMetersPerSecond,admitNextSection,target);
        }

        private ManualAiSteeringPlan PlanCore(ManualSkiHeadingState pose,ManualRouteProjection observation,
            double planarSpeedMetersPerSecond,bool admitNextSection,double targetLateral,double lookaheadOverride=0)
        {
            if(!pose.IsValid||!observation.IsValid)throw new ArgumentException("Initialized pose and local route observation required.");
            PhysiologyNumbers.RequireRange(planarSpeedMetersPerSecond,0,PhysiologyNumbers.MaximumMagnitude,nameof(planarSpeedMetersPerSecond));
            double lookahead=Math.Min(policy.LookaheadMaximumMeters,
                policy.LookaheadBaseMeters+planarSpeedMetersPerSecond*policy.LookaheadSpeedSeconds);
            if(lookaheadOverride>0)lookahead=Math.Min(lookahead,lookaheadOverride);
            if(!route.TrySampleAhead(observation.SectionIndex,observation.RouteNanometers,lookahead,
                admitNextSection,out var target))return default;
            return TargetPlan(pose,lookahead,target,targetLateral);
        }
        private ManualAiSteeringPlan TargetPlan(ManualSkiHeadingState pose,double lookahead,ManualRouteTarget target,double targetLateral)
        {
            double x=target.X+target.ForwardZ*targetLateral-pose.X;
            double z=target.Z-target.ForwardX*targetLateral-pose.Z;
            double desired=Math.Abs(x)+Math.Abs(z)<PhysiologyNumbers.MinimumPositive
                ?Math.Atan2(target.ForwardX,target.ForwardZ):Math.Atan2(x,z);
            double delta=desired-pose.HeadingRadians;
            double error=Math.Atan2(Math.Sin(delta),Math.Cos(delta));
            double normalized=error/(policy.HeadingResponseSeconds*maximumYawRate)-
                policy.YawRateFeedback*pose.YawRateRadiansPerSecond/maximumYawRate;
            int steering=(int)Math.Round(Math.Max(-1,Math.Min(1,normalized))*ManualSkiCommand.SteeringUnits,
                MidpointRounding.ToEven);
            return new ManualAiSteeringPlan(steering,error,lookahead,target,targetLateral);
        }
        /// <summary>An endpoint is a transverse line, not a compulsory centre
        /// point. Retain a fitting lateral arrival line through steering intent.</summary>
        public ManualAiSteeringPlan PlanParallelEndpoint(ManualSkiHeadingState pose,ManualAiSteeringPlan plan,double usableHalfWidth)
        {
            if(!pose.IsValid||!plan.IsValid)throw new ArgumentException("Initialized pose and target required.");
            PhysiologyNumbers.RequireRange(usableHalfWidth,0,PhysiologyNumbers.MaximumMagnitude,nameof(usableHalfWidth));
            var target=plan.Target;
            double side=(pose.X-target.X)*target.ForwardZ-(pose.Z-target.Z)*target.ForwardX;
            return TargetPlan(pose,plan.LookaheadMeters,target,Math.Max(-usableHalfWidth,Math.Min(usableHalfWidth,side)));
        }
        public ManualAiSteeringPlan PlanPassing(ManualSkiHeadingState pose,ManualRouteProjection observation,
            double speed,bool admitNextSection,double targetLateral,double usableHalfWidth,double clearanceDistance)
        {
            PhysiologyNumbers.RequireRange(clearanceDistance,PhysiologyNumbers.MinimumPositive,PhysiologyNumbers.MaximumMagnitude,nameof(clearanceDistance));
            PhysiologyNumbers.RequireRange(usableHalfWidth,0,PhysiologyNumbers.MaximumMagnitude,nameof(usableHalfWidth));
            PhysiologyNumbers.RequireRange(targetLateral,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(targetLateral));
            return PlanCore(pose,observation,speed,admitNextSection,Math.Max(-usableHalfWidth,Math.Min(usableHalfWidth,targetLateral)),clearanceDistance);
        }
        /// <summary>An explicitly closed lifecycle gate still owns its endpoint
        /// as a steering target. Shorten the query; never sample beyond the gate.</summary>
        public ManualAiSteeringPlan PlanOwnedTarget(ManualSkiHeadingState pose,ManualRouteProjection observation,
            double speed,bool admitNextSection,double targetLateral,double usableHalfWidth,double remainingMeters)
        {
            PhysiologyNumbers.RequireRange(remainingMeters,PhysiologyNumbers.MinimumPositive,PhysiologyNumbers.MaximumMagnitude,nameof(remainingMeters));
            PhysiologyNumbers.RequireRange(usableHalfWidth,0,PhysiologyNumbers.MaximumMagnitude,nameof(usableHalfWidth));
            PhysiologyNumbers.RequireRange(targetLateral,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(targetLateral));
            return PlanCore(pose,observation,speed,admitNextSection,Math.Max(-usableHalfWidth,Math.Min(usableHalfWidth,targetLateral)),remainingMeters);
        }
        /// <summary>Scoped AI uses path curvature implied by the actual target
        /// chord. A bearing servo alone keeps cutting tight bends because its
        /// nonzero lookahead angle persists even on the correct circular line.
        /// The chord error uses achieved velocity direction, including physical
        /// slip. Invert the shared speed-dependent command response and retain
        /// bounded spin feedback. This is intent only, never physical yaw.</summary>
        public int PhysicalPursuitIntent(ManualAiSteeringPlan plan,ManualSkiHeadingState pose,SkiPlanarVelocity velocity)
        {
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double response=ManualSkiSteeringSystem.GetRequest(physicalCommands,ManualSkiCommand.SteeringUnits,speed,PhysiologyNumbers.MaximumMagnitude).RequestedYawRate;
            double input=PhysicalPursuitYaw(plan,pose,velocity)/response;
            return (int)Math.Round(Math.Max(-1,Math.Min(1,input))*ManualSkiCommand.SteeringUnits,MidpointRounding.ToEven);
        }
        public double PhysicalPursuitYaw(ManualAiSteeringPlan plan,ManualSkiHeadingState pose,SkiPlanarVelocity velocity)
        {
            if(!plan.IsValid||!pose.IsValid)throw new ArgumentException("Initialized owned plan and pose required.");
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double direction=speed>=lowSpeedReference?Math.Atan2(velocity.X,velocity.Z):pose.HeadingRadians;
            double error=pose.HeadingRadians+plan.HeadingErrorRadians-direction;
            double dx=plan.Target.X+plan.Target.ForwardZ*plan.TargetLateralMeters-pose.X;
            double dz=plan.Target.Z-plan.Target.ForwardX*plan.TargetLateralMeters-pose.Z;
            double chord=Math.Sqrt(dx*dx+dz*dz);
            double desired=2*Math.Max(speed,lowSpeedReference)*Math.Sin(error)/Math.Max(chord,PhysiologyNumbers.MinimumPositive);
            return desired-policy.YawRateFeedback*(pose.YawRateRadiansPerSecond-desired);
        }
    }
}

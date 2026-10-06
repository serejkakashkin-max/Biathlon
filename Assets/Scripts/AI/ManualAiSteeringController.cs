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
        public string ContextJson {get;}
        public ManualAiSteeringController(ManualRouteProjectionMap route,ManualAiSteeringDefinition policy,
            ManualSkiSteeringDefinition physicalSteering)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            this.policy=policy??throw new ArgumentNullException(nameof(policy));
            if(physicalSteering==null)throw new ArgumentNullException(nameof(physicalSteering));
            maximumYawRate=physicalSteering.MaximumYawRate;
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(string[])).WriteObject(stream,new[]{"manual_ai_signed_yaw_intent_v1",
                    route.ContextJson,policy.ToJson(),physicalSteering.ToJson(),
                    "manual_ai_bounded_pass_line_yaw_v2;caller_width;body_course_sweep_required"});
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
            double planarSpeedMetersPerSecond,bool admitNextSection,double targetLateral)
        {
            if(!pose.IsValid||!observation.IsValid)throw new ArgumentException("Initialized pose and local route observation required.");
            PhysiologyNumbers.RequireRange(planarSpeedMetersPerSecond,0,PhysiologyNumbers.MaximumMagnitude,nameof(planarSpeedMetersPerSecond));
            double lookahead=Math.Min(policy.LookaheadMaximumMeters,
                policy.LookaheadBaseMeters+planarSpeedMetersPerSecond*policy.LookaheadSpeedSeconds);
            if(!route.TrySampleAhead(observation.SectionIndex,observation.RouteNanometers,lookahead,
                admitNextSection,out var target))return default;
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
    }
}

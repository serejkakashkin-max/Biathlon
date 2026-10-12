using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ManualSkiStoppingPreview
    {
        public ManualSkiControlledDriveFrame Frame {get;}
        public double BrakeFraction {get;}
        public bool WithinBrakingBudget {get;}
        public bool CoastStopsEarlier {get;}
        internal ManualSkiStoppingPreview(ManualSkiControlledDriveFrame frame,double brake,bool within,bool early)
        {Frame=frame;BrakeFraction=brake;WithinBrakingBudget=within;CoastStopsEarlier=early;}
    }
    public readonly struct ManualSkiControlledDriveFrame
    {
        public bool IsValid=>Drive.IsValid&&SteeredSnow.IsValid;
        public SkiDrivenContactTrajectory Drive {get;}
        public ManualSkiSteeredSnowPreview SteeredSnow {get;}
        public CorneringOutput Cornering {get;}
        public double ActivePivotWorkJoules {get;}
        internal ManualSkiControlledDriveFrame(SkiDrivenContactTrajectory drive,ManualSkiSteeredSnowPreview snow,CorneringOutput cornering,double pivotWork=0)
        {Drive=drive;SteeredSnow=snow;Cornering=cornering;ActivePivotWorkJoules=pivotWork;}
    }
    /// <summary>Signed manual intent with shared ski forces and independent
    /// physical slip/spin. Output still requires common contact, body, course,
    /// boundary and fixed-clock/session admission.</summary>
    public sealed class ManualSkiControlledDriveSystem
    {
        private readonly ManualSkiContactDriveSystem drive;
        private readonly ManualSkiSteeredSnowSystem snow;
        private readonly CorneringSystem cornering;
        private readonly ManualSkiSteeringSystem commands;
        private readonly double mass,maximumBrakingNewtons;
        private readonly ManualSkiSteeringDefinition pivotDefinition;
        private readonly double pivotArm,pivotInertia;
        private readonly bool steeringBraking;
        public string ContextJson {get;}
        public ManualSkiControlledDriveSystem(SkiPhysicsDefinition physics,OccupancyDefinition body,
            ManualSkiSteeringDefinition steering,CorneringDefinition turns,int frequency)
        {
            drive=new ManualSkiContactDriveSystem(physics,body,frequency);
            snow=new ManualSkiSteeredSnowSystem(body,steering,frequency);cornering=new CorneringSystem(turns,physics);
            commands=new ManualSkiSteeringSystem(steering,frequency);
            pivotDefinition=steering;pivotArm=body.BodyLength*.5;
            pivotInertia=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            mass=physics.MassKilograms;maximumBrakingNewtons=mass*turns.MaximumBraking;
            steeringBraking=turns.ControlledSteeringBraking??true;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{steering.ActivePivotBelowSpeed>0?"manual_controlled_slip_drive_v7_paid_low_speed_pivot":steeringBraking?"manual_controlled_slip_drive_v5_paid_inverse_stopping":"manual_controlled_slip_drive_v6_explicit_gameplay_braking",drive.ContextJson,snow.ContextJson,
                turns.ToJson(),(steeringBraking?"one_joint_snow_budget;actual_heading_grade;max_actual_or_requested_yaw_braking;":"one_joint_snow_budget;actual_heading_grade;steering_has_no_automatic_brake;")+"explicit_brake_fraction_uses_same_force_cap;max_not_sum;shared_corner_force_limits;inverse_frozen_stopping_intent_with_explicit_infeasibility;outward_64ulp_inverse_force_reserve_within_cap;unchanged_shared_longitudinal_forces;no_admission"});
        }
        /// <summary>Invert only the shared frozen longitudinal force law for a
        /// neutral, unpowered stopping intent. The requested future stop time
        /// is not an arrival proof. Grip, spin, contact, course and phase proofs
        /// remain independent, and an infeasible request retains the real motion.</summary>
        public ManualSkiStoppingPreview PreviewStopping(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,
            double seconds,double stopAfterSeconds)
        {
            if(!input.IsValid||input.SteeringInput!=0||input.DeliveredPowerWatts!=0||input.BrakeFraction!=0)
                throw new ArgumentException("Neutral unpowered input without a competing brake intent required.");
            return PreviewStoppingCore(body,motion,observation,surface,input,seconds,stopAfterSeconds);
        }
        // The owning field may retain some already paid propulsion when passive
        // resistance would stop before the portal. No extra effort or force cap.
        internal ManualSkiStoppingPreview PreviewStoppingWithPaidPower(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,double seconds,double stopAfterSeconds)
        {
            if(!input.IsValid||input.SteeringInput!=0||input.BrakeFraction!=0)
                throw new ArgumentException("Neutral paid input without a competing brake intent required.");
            return PreviewStoppingCore(body,motion,observation,surface,input,seconds,stopAfterSeconds);
        }
        private ManualSkiStoppingPreview PreviewStoppingCore(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,double seconds,double stopAfterSeconds)
        {
            PhysiologyNumbers.RequireRange(stopAfterSeconds,Math.Max(seconds,PhysiologyNumbers.MinimumPositive),
                PhysiologyNumbers.MaximumMagnitude,nameof(stopAfterSeconds));
            var baseline=Preview(body,motion,observation,surface,input,seconds);
            double required=baseline.Cornering.BrakingNewtons+mass*(baseline.Drive.FirstDrive.Acceleration+
                baseline.Drive.FirstDrive.InitialState.SpeedMetersPerSecond/stopAfterSeconds);
            double reserve=64*2.2204460492503131e-16*(Math.Abs(baseline.Cornering.BrakingNewtons)+
                mass*(Math.Abs(baseline.Drive.FirstDrive.Acceleration)+baseline.Drive.FirstDrive.InitialState.SpeedMetersPerSecond/stopAfterSeconds));
            double fraction=Math.Max(0,Math.Min(1,(required+reserve)/maximumBrakingNewtons));
            var frame=Preview(body,motion,observation,surface,input.WithBrakeFraction(fraction),seconds);
            return new ManualSkiStoppingPreview(frame,fraction,required>=baseline.Cornering.BrakingNewtons&&required<=maximumBrakingNewtons,
                required<baseline.Cornering.BrakingNewtons);
        }
        public ManualSkiControlledDriveFrame Preview(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,double seconds)
        {
            if(!body.IsValid||!observation.IsValid||!input.IsValid)throw new ArgumentException("Exact current physical source and prepared force input required.");
            double grade=observation.GradeAlong(body.Pose);
            var state=new SkiState(0,Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z)*Math.Sqrt(1+grade*grade));
            // Grip/slip stays physical for every athlete. The production preset
            // leaves braking to an explicit intent; steering itself is not a brake.
            double requested=commands.GetRequest(input.SteeringInput,Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z),
                PhysiologyNumbers.MaximumMagnitude,input.SteeringResponse).RequestedYawRate;
            double demand=Math.Max(Math.Abs(motion.YawRate),Math.Abs(requested));
            var turns=cornering.EvaluateWorldHeading(state,surface.WithTravelGeometry(grade,0),demand,input.CorneringSkill,input.DownhillSkill,
                input.LineQuality,input.MuscularFatigue,input.GripMultiplier);
            if(!steeringBraking)turns=new CorneringOutput(turns.LateralAccelerationLimit,turns.SafeSpeedMetersPerSecond,turns.ExcessSpeedMetersPerSecond,0);
            var steered=snow.Preview(body,motion,input.SteeringInput,turns.LateralAccelerationLimit,seconds,input.SteeringResponse);
            double pivotWork=0;
            double planarSpeed=Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z);
            if(input.SteeringInput!=0&&input.DeliveredPowerWatts>0&&planarSpeed<pivotDefinition.ActivePivotBelowSpeed)
            {
                // Pole-assisted stepping at low speed. Pay rotational work from the already delivered
                // power, taper at the speed boundary, and reserve the remaining support force budget.
                var receipt=steered.Snow;double inertia=pivotInertia*body.MassKilograms;
                double support=.5*turns.LateralAccelerationLimit*seconds*body.MassKilograms;
                double free=Math.Max(0,Math.Min(support-Math.Abs(receipt.FrontImpulseNewtonSeconds),support-Math.Abs(receipt.RearImpulseNewtonSeconds)));
                double taper=1-planarSpeed/pivotDefinition.ActivePivotBelowSpeed;
                double limit=Math.Min(steered.Request.AngularAccelerationLimit*seconds*taper,2*pivotArm*free/inertia);
                double prior=receipt.Motion.YawRate,delta=Math.Clamp(steered.Request.TargetYawRate-prior,-limit,limit);
                double next=prior+delta;double budget=input.DeliveredPowerWatts*seconds;
                if(.5*inertia*(next*next-prior*prior)>budget)next=Math.Sign(next)*Math.Sqrt(prior*prior+2*budget/inertia);
                pivotWork=Math.Max(0,.5*inertia*(next*next-prior*prior));
                var active=new SkiSnowContactPreview(new SkiContactMotion(receipt.Motion.Velocity,next),receipt.AvailableLateralAcceleration,
                    receipt.FrontImpulseNewtonSeconds,receipt.RearImpulseNewtonSeconds,receipt.DissipatedEnergyJoules,false);
                steered=new ManualSkiSteeredSnowPreview(steered.Request,active,steered.FrontDeflectionRadians,steered.AngularResponseScale);
            }
            var trajectory=drive.PreviewWithSnow(body,motion,observation.GeometryProjection,surface,Math.Max(0,input.DeliveredPowerWatts-pivotWork/seconds),steered.Snow,seconds,
                input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.WindX,input.WindZ,
                cornering.ControlledBrakingNewtons(turns.BrakingNewtons,input.BrakeFraction));
            return new ManualSkiControlledDriveFrame(trajectory,steered,turns,pivotWork);
        }
    }
}

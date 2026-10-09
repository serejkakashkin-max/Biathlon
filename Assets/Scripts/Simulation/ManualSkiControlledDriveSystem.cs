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
        internal ManualSkiControlledDriveFrame(SkiDrivenContactTrajectory drive,ManualSkiSteeredSnowPreview snow,CorneringOutput cornering)
        {Drive=drive;SteeredSnow=snow;Cornering=cornering;}
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
        public string ContextJson {get;}
        public ManualSkiControlledDriveSystem(SkiPhysicsDefinition physics,OccupancyDefinition body,
            ManualSkiSteeringDefinition steering,CorneringDefinition turns,int frequency)
        {
            drive=new ManualSkiContactDriveSystem(physics,body,frequency);
            snow=new ManualSkiSteeredSnowSystem(body,steering,frequency);cornering=new CorneringSystem(turns,physics);
            commands=new ManualSkiSteeringSystem(steering,frequency);
            mass=physics.MassKilograms;maximumBrakingNewtons=mass*turns.MaximumBraking;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_controlled_slip_drive_v4_inverse_stopping",drive.ContextJson,snow.ContextJson,
                turns.ToJson(),"one_joint_snow_budget;actual_heading_grade;max_actual_or_requested_yaw_braking;explicit_brake_fraction_uses_same_force_cap;max_not_sum;shared_corner_force_limits;inverse_frozen_stopping_intent_with_explicit_infeasibility;outward_64ulp_inverse_force_reserve_within_cap;unchanged_shared_longitudinal_forces;no_admission"});
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
            // Grip limits the achieved turn, but must not erase the requested
            // high-speed ski deflection's braking cost. Use the same bounded
            // command and corner force law for every controlled athlete.
            double requested=commands.GetRequest(input.SteeringInput,Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z),
                PhysiologyNumbers.MaximumMagnitude,input.SteeringResponse).RequestedYawRate;
            double demand=Math.Max(Math.Abs(motion.YawRate),Math.Abs(requested));
            var turns=cornering.EvaluateWorldHeading(state,surface.WithTravelGeometry(grade,0),demand,input.CorneringSkill,input.DownhillSkill,
                input.LineQuality,input.MuscularFatigue,input.GripMultiplier);
            var steered=snow.Preview(body,motion,input.SteeringInput,turns.LateralAccelerationLimit,seconds,input.SteeringResponse);
            var trajectory=drive.PreviewWithSnow(body,motion,observation.GeometryProjection,surface,input.DeliveredPowerWatts,steered.Snow,seconds,
                input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.WindX,input.WindZ,
                cornering.ControlledBrakingNewtons(turns.BrakingNewtons,input.BrakeFraction));
            return new ManualSkiControlledDriveFrame(trajectory,steered,turns);
        }
    }
}

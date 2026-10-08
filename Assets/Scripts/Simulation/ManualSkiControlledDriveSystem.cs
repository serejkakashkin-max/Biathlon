using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
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
        public string ContextJson {get;}
        public ManualSkiControlledDriveSystem(SkiPhysicsDefinition physics,OccupancyDefinition body,
            ManualSkiSteeringDefinition steering,CorneringDefinition turns,int frequency)
        {
            drive=new ManualSkiContactDriveSystem(physics,body,frequency);
            snow=new ManualSkiSteeredSnowSystem(body,steering,frequency);cornering=new CorneringSystem(turns,physics);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_controlled_slip_drive_v1",drive.ContextJson,snow.ContextJson,
                turns.ToJson(),"one_joint_snow_budget;actual_heading_grade_and_yaw;existing_corner_braking;unchanged_shared_longitudinal_forces;no_admission"});
        }
        public ManualSkiControlledDriveFrame Preview(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,ManualSkiMotionInput input,double seconds)
        {
            if(!body.IsValid||!observation.IsValid||!input.IsValid)throw new ArgumentException("Exact current physical source and prepared force input required.");
            double grade=observation.GradeAlong(body.Pose);
            var state=new SkiState(0,Math.Sqrt(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z)*Math.Sqrt(1+grade*grade));
            var turns=cornering.EvaluateWorldHeading(state,surface.WithTravelGeometry(grade,0),motion.YawRate,input.CorneringSkill,input.DownhillSkill,
                input.LineQuality,input.MuscularFatigue,input.GripMultiplier);
            var steered=snow.Preview(body,motion,input.SteeringInput,turns.LateralAccelerationLimit,seconds,input.SteeringResponse);
            var trajectory=drive.PreviewWithSnow(body,motion,observation.GeometryProjection,surface,input.DeliveredPowerWatts,steered.Snow,seconds,
                input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.WindX,input.WindZ,turns.BrakingNewtons);
            return new ManualSkiControlledDriveFrame(trajectory,steered,turns);
        }
    }
}

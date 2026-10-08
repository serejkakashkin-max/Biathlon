using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Shared longitudinal ski forces plus the passive frozen-pose snow
    /// operator after a physical contact. Retains independent slip/spin. This
    /// preview neither applies a new steering command nor admits any movement.</summary>
    public sealed class ManualSkiContactDriveSystem
    {
        private readonly SkiForceSystem forces;
        private readonly SkiPhysicsDefinition physics;
        private readonly ManualSkiSnowContactSystem snow;
        private readonly double duration;
        public string ContextJson {get;}
        public ManualSkiContactDriveSystem(SkiPhysicsDefinition physics,OccupancyDefinition body,int frequency)
        {
            this.physics=physics??throw new ArgumentNullException(nameof(physics));
            forces=new SkiForceSystem(physics,frequency);snow=new ManualSkiSnowContactSystem(body,frequency);duration=1.0/frequency;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_contact_longitudinal_drive_v1",physics.ToJson(),snow.TrajectoryContextJson,
                "frozen_ski_axis;forward_work_speed;shared_force_stop_cap;backward_propulsion_is_braking;paid_restart_at_physical_zero;passive_slip_spin;no_new_steering_or_admission"});
        }
        public SkiDrivenContactTrajectory Preview(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRacePathObservation observation,TrackSample surface,double deliveredPowerWatts,
            double lateralAccelerationLimit,double reservedTurningAcceleration,double seconds,
            double dragAreaMultiplier=1,double snowResistanceMultiplier=1,double windX=0,double windZ=0,double brakingNewtons=0)
        {
            if(!observation.IsValid)throw new ArgumentException("Initialized scoped physical observation required.");
            return Preview(body,motion,observation.GeometryProjection,surface,deliveredPowerWatts,lateralAccelerationLimit,
                reservedTurningAcceleration,seconds,dragAreaMultiplier,snowResistanceMultiplier,windX,windZ,brakingNewtons);
        }
        public SkiDrivenContactTrajectory Preview(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRouteProjection observation,TrackSample surface,double deliveredPowerWatts,
            double lateralAccelerationLimit,double reservedTurningAcceleration,double seconds,
            double dragAreaMultiplier=1,double snowResistanceMultiplier=1,double windX=0,double windZ=0,double brakingNewtons=0)
            =>PreviewWithSnow(body,motion,observation,surface,deliveredPowerWatts,
                snow.Preview(body,motion,lateralAccelerationLimit,reservedTurningAcceleration,seconds),seconds,
                dragAreaMultiplier,snowResistanceMultiplier,windX,windZ,brakingNewtons);
        internal SkiDrivenContactTrajectory PreviewWithSnow(ManualSkiContactBody body,SkiContactMotion motion,
            ManualRouteProjection observation,TrackSample surface,double deliveredPowerWatts,SkiSnowContactPreview snowPreview,double seconds,
            double dragAreaMultiplier,double snowResistanceMultiplier,double windX,double windZ,double brakingNewtons)
        {
            if(!body.IsValid||!observation.IsValid||body.MassKilograms!=physics.MassKilograms||surface.LoopId!=observation.LoopId||
                Math.Abs(surface.Distance.Meters-observation.LocalMeters)>.001+2*PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Exact physical body mass and current surface observation required.");
            PhysiologyNumbers.RequireRange(seconds,double.Epsilon,duration,nameof(seconds));
            PhysiologyNumbers.RequireRange(windX,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(windX));
            PhysiologyNumbers.RequireRange(windZ,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(windZ));
            double h=body.Pose.HeadingRadians,sx=Math.Sin(h),sz=Math.Cos(h);
            double forward=motion.Velocity.X*sx+motion.Velocity.Z*sz;
            double grade=observation.GradeAlong(body.Pose),scalar=Math.Abs(forward)*Math.Sqrt(1+grade*grade);
            double encoded=Math.Ceiling(scalar/PhysiologyNumbers.MinimumPositive)*PhysiologyNumbers.MinimumPositive;
            if(encoded>physics.MaximumSpeedMetersPerSecond&&scalar<=physics.MaximumSpeedMetersPerSecond*(1+16*2.2204460492503131e-16))
                encoded=physics.MaximumSpeedMetersPerSecond;
            double scale=forward==0?1/Math.Sqrt(1+grade*grade):Math.Abs(forward)/encoded;
            var start=new SkiState(0,encoded);double wind=-(windX*sx+windZ*sz);
            var along=surface.WithTravelGeometry(grade,0);
            SkiForceTrajectory first,second=default;int direction=forward<0?-1:1;double firstSeconds=seconds;
            if(direction>0)
                first=forces.PreviewTrajectory(start,deliveredPowerWatts,along,seconds,wind,dragAreaMultiplier,brakingNewtons,snowResistanceMultiplier);
            else
            {
                // Work against backwards travel brakes it; it cannot be added as
                // a backwards speed bonus. Reuse the common power/force law.
                var powered=forces.PreviewTrajectory(start,deliveredPowerWatts,along,seconds,wind,dragAreaMultiplier,brakingNewtons,snowResistanceMultiplier);
                first=forces.PreviewTrajectory(start,0,surface.WithTravelGeometry(-grade,0),seconds,-wind,
                    dragAreaMultiplier,brakingNewtons+powered.Output.PropulsiveNewtons,snowResistanceMultiplier);
                if(first.Acceleration<0&&first.SpeedBoundarySeconds<seconds)
                {
                    firstSeconds=first.SpeedBoundarySeconds;
                    second=forces.PreviewTrajectory(default,deliveredPowerWatts,along,seconds-firstSeconds,wind,
                        dragAreaMultiplier,brakingNewtons,snowResistanceMultiplier);
                }
            }
            if(!snowPreview.IsValid)throw new ArgumentException("Initialized frozen snow preview required.");
            return new SkiDrivenContactTrajectory(body.Pose,motion,snowPreview,first,second,direction,scale,
                1/Math.Sqrt(1+grade*grade),firstSeconds,seconds,body.MassKilograms);
        }
    }

    /// <summary>Exact shared longitudinal distance/stop/cap along a frozen ski
    /// axis, summed with the existing lateral/spin snow ramp. Prefixes retain
    /// their original force and snow previews; no accepted pose is snapped.</summary>
    public readonly struct SkiDrivenContactTrajectory
    {
        public ManualSkiHeadingState InitialPose {get;}
        public ManualSkiHeadingState State {get;}
        public SkiContactMotion InitialMotion {get;}
        public SkiSnowContactPreview Snow {get;}
        public SkiForceTrajectory FirstDrive {get;}
        public SkiForceTrajectory RestartDrive {get;}
        public double DurationSeconds {get;}
        public double FirstDriveSeconds {get;}
        public double MassKilograms {get;}
        public bool IsValid=>FirstDrive.IsValid&&Snow.IsValid&&InitialPose.IsValid&&DurationSeconds>0;
        private readonly int direction;
        private readonly double scale,restartScale,forwardX,forwardZ,initialForward;
        /// <summary>The first positive force-law breakpoint. A coupled force
        /// preview must split here rather than average across a stop or cap.</summary>
        public double ConstantAccelerationSeconds
        {
            get
            {
                double end=DurationSeconds;
                if(FirstDriveSeconds>0&&FirstDriveSeconds<end)end=FirstDriveSeconds;
                double boundary=FirstDrive.SpeedBoundarySeconds;
                if(boundary>0&&boundary<FirstDriveSeconds&&boundary<end)end=boundary;
                boundary=FirstDriveSeconds+RestartDrive.SpeedBoundarySeconds;
                if(RestartDrive.IsValid&&boundary>0&&boundary<end)end=boundary;
                return end;
            }
        }
        public double PeakYawRate=>Math.Max(Math.Abs(InitialMotion.YawRate),Math.Abs(Snow.Motion.YawRate));
        public double PlanarAccelerationBound=>Math.Max(Math.Abs(FirstDrive.Acceleration),RestartDrive.IsValid?Math.Abs(RestartDrive.Acceleration):0)*
            Math.Max(scale,restartScale)+SnowAcceleration;
        private double SnowAcceleration
        {
            get{double x=Snow.Motion.Velocity.X-InitialMotion.Velocity.X,z=Snow.Motion.Velocity.Z-InitialMotion.Velocity.Z;
                return Math.Sqrt(x*x+z*z)/DurationSeconds;}
        }
        public double HeadingErrorBoundRadians=>2e-9*(1+PeakYawRate*DurationSeconds);
        public double PositionErrorBoundMeters=>1e-9+64*2.2204460492503131e-16*(1+Math.Abs(InitialPose.X)+Math.Abs(InitialPose.Z)+
            DurationSeconds*(Math.Abs(InitialMotion.Velocity.X)+Math.Abs(InitialMotion.Velocity.Z)+Math.Abs(Snow.Motion.Velocity.X)+
                Math.Abs(Snow.Motion.Velocity.Z)+FirstDrive.PeakSpeed+(RestartDrive.IsValid?RestartDrive.PeakSpeed:0)));
        internal SkiDrivenContactTrajectory(ManualSkiHeadingState pose,SkiContactMotion initial,SkiSnowContactPreview snow,
            SkiForceTrajectory first,SkiForceTrajectory restart,int direction,double scale,double restartScale,double firstSeconds,double seconds,double mass)
        {
            InitialPose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,initial.YawRate);InitialMotion=initial;Snow=snow;
            FirstDrive=first;RestartDrive=restart;this.direction=direction;this.scale=scale;this.restartScale=restartScale;
            FirstDriveSeconds=firstSeconds;DurationSeconds=seconds;MassKilograms=mass;
            forwardX=Math.Sin(pose.HeadingRadians);forwardZ=Math.Cos(pose.HeadingRadians);
            initialForward=initial.Velocity.X*forwardX+initial.Velocity.Z*forwardZ;
            State=default;State=Sample(1);ValidateCoordinates();
        }
        private double ForwardDistance(double time)
            =>direction*FirstDrive.DistanceAt(Math.Min(time,FirstDriveSeconds))*scale+
                (RestartDrive.IsValid&&time>FirstDriveSeconds?RestartDrive.DistanceAt(time-FirstDriveSeconds)*restartScale:0);
        private double ForwardSpeed(double time)
            =>RestartDrive.IsValid&&time>FirstDriveSeconds?RestartDrive.SpeedRaw(time-FirstDriveSeconds)*restartScale:
                direction*FirstDrive.SpeedRaw(Math.Min(time,FirstDriveSeconds))*scale;
        public ManualSkiHeadingState Sample(double fraction)
        {
            Validate(fraction);if(fraction==0)return InitialPose;
            double time=DurationSeconds*fraction,travel=ForwardDistance(time)-initialForward*time;
            var end=MotionAt(fraction);
            double dx=Snow.Motion.Velocity.X-InitialMotion.Velocity.X,dz=Snow.Motion.Velocity.Z-InitialMotion.Velocity.Z;
            return new ManualSkiHeadingState(InitialPose.X+InitialMotion.Velocity.X*time+forwardX*travel+dx*time*fraction*.5,
                InitialPose.Z+InitialMotion.Velocity.Z*time+forwardZ*travel+dz*time*fraction*.5,
                InitialPose.HeadingRadians+time*(InitialMotion.YawRate+end.YawRate)*.5,end.YawRate);
        }
        public SkiContactMotion MotionAt(double fraction)
        {
            Validate(fraction);if(fraction==0)return InitialMotion;
            double change=ForwardSpeed(DurationSeconds*fraction)-initialForward;
            return new SkiContactMotion(new SkiPlanarVelocity(InitialMotion.Velocity.X+forwardX*change+(Snow.Motion.Velocity.X-InitialMotion.Velocity.X)*fraction,
                InitialMotion.Velocity.Z+forwardZ*change+(Snow.Motion.Velocity.Z-InitialMotion.Velocity.Z)*fraction),
                InitialMotion.YawRate+(Snow.Motion.YawRate-InitialMotion.YawRate)*fraction);
        }
        private void Validate(double fraction)
        {if(!IsValid)throw new InvalidOperationException("Initialized driven contact trajectory required.");PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));}
        private void ValidateCoordinates()
        {
            double from=0;
            while(from<DurationSeconds)
            {
                double to=DurationSeconds;
                if(FirstDriveSeconds>from&&FirstDriveSeconds<to)to=FirstDriveSeconds;
                if(FirstDrive.SpeedBoundarySeconds>from&&FirstDrive.SpeedBoundarySeconds<FirstDriveSeconds&&FirstDrive.SpeedBoundarySeconds<to)
                    to=FirstDrive.SpeedBoundarySeconds;
                double boundary=FirstDriveSeconds+RestartDrive.SpeedBoundarySeconds;
                if(RestartDrive.IsValid&&boundary>from&&boundary<to)to=boundary;
                var a=MotionAt(from/DurationSeconds).Velocity;var b=MotionAt(to/DurationSeconds).Velocity;
                CheckAxis(a.X,b.X,from,to);CheckAxis(a.Z,b.Z,from,to);Sample(to/DurationSeconds);
                from=to;
            }
        }
        private void CheckAxis(double initial,double final,double from,double to)
        {
            if(initial*final<0)
            {double at=from+(to-from)*(-initial)/(final-initial);Sample(at/DurationSeconds);}
        }
    }
}

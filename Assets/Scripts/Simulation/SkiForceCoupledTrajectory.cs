using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Centre and angular acceleration; units are m/s² and rad/s².
    /// Independent of heading, with no velocity or pose correction.</summary>
    public readonly struct SkiContactAcceleration
    {
        public double X {get;} public double Z {get;} public double Yaw {get;}
        public SkiContactAcceleration(double x,double z,double yaw)
        {
            X=PhysiologyNumbers.RequireRange(x,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(x));
            Z=PhysiologyNumbers.RequireRange(z,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(z));
            Yaw=PhysiologyNumbers.RequireRange(yaw,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(yaw));
        }
    }
    /// <summary>One constant-force piece of a jointly coupled frozen drive.
    /// No free-force breakpoint may lie inside it. Normal/tangent reactions
    /// are physical accelerations, not endpoint impulses or a movement proof.</summary>
    public readonly struct SkiForceCoupledTrajectory
    {
        public ManualSkiHeadingState InitialPose {get;}
        public ManualSkiHeadingState State {get;}
        public SkiContactMotion InitialMotion {get;}
        public SkiContactAcceleration Acceleration {get;}
        public double DurationSeconds {get;}
        public bool IsValid=>InitialPose.IsValid&&DurationSeconds>0;
        public double PeakYawRate=>Math.Max(Math.Abs(InitialMotion.YawRate),Math.Abs(InitialMotion.YawRate+Acceleration.Yaw*DurationSeconds));
        public double PlanarAccelerationBound=>Math.Sqrt(Acceleration.X*Acceleration.X+Acceleration.Z*Acceleration.Z);
        public double PositionErrorBoundMeters=>1e-9+64*2.2204460492503131e-16*(1+Math.Abs(InitialPose.X)+Math.Abs(InitialPose.Z)+
            DurationSeconds*(Math.Abs(InitialMotion.Velocity.X)+Math.Abs(InitialMotion.Velocity.Z)+DurationSeconds*(Math.Abs(Acceleration.X)+Math.Abs(Acceleration.Z))));
        public double HeadingErrorBoundRadians=>2e-9*(1+PeakYawRate*DurationSeconds);
        internal SkiForceCoupledTrajectory(ManualSkiHeadingState pose,SkiContactMotion motion,SkiContactAcceleration acceleration,double seconds)
        {
            if(!pose.IsValid)throw new ArgumentException("Initialized force source pose required.");
            PhysiologyNumbers.RequireRange(seconds,double.Epsilon,PhysiologyNumbers.MaximumMagnitude,nameof(seconds));
            InitialPose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,motion.YawRate);
            InitialMotion=motion;Acceleration=acceleration;DurationSeconds=seconds;State=default;
            State=Sample(1);
            ValidateAxis(motion.Velocity.X,acceleration.X);ValidateAxis(motion.Velocity.Z,acceleration.Z);
            if(acceleration.Yaw!=0)
            {
                double at=-motion.YawRate/acceleration.Yaw;
                if(at>0&&at<seconds)Sample(at/seconds);
            }
        }
        public ManualSkiHeadingState Sample(double fraction)
        {
            Validate(fraction);if(fraction==0)return InitialPose;
            double t=DurationSeconds*fraction;var motion=MotionAt(fraction);
            return new ManualSkiHeadingState(InitialPose.X+t*(InitialMotion.Velocity.X+Acceleration.X*t*.5),
                InitialPose.Z+t*(InitialMotion.Velocity.Z+Acceleration.Z*t*.5),
                InitialPose.HeadingRadians+t*(InitialMotion.YawRate+Acceleration.Yaw*t*.5),motion.YawRate);
        }
        public SkiContactMotion MotionAt(double fraction)
        {
            Validate(fraction);if(fraction==0)return InitialMotion;
            double t=DurationSeconds*fraction;
            return new SkiContactMotion(new SkiPlanarVelocity(InitialMotion.Velocity.X+Acceleration.X*t,InitialMotion.Velocity.Z+Acceleration.Z*t),
                InitialMotion.YawRate+Acceleration.Yaw*t);
        }
        private void Validate(double fraction)
        {if(!IsValid)throw new InvalidOperationException("Initialized coupled force trajectory required.");PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));}
        private void ValidateAxis(double velocity,double acceleration)
        {
            if(acceleration!=0)
            {double at=-velocity/acceleration;if(at>0&&at<DurationSeconds)Sample(at/DurationSeconds);}
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Frozen-interval spatial continuation of post-contact velocity and
    /// a single snow impulse. World velocity remains independent of ski heading.</summary>
    public readonly struct SkiContactTrajectory
    {
        public ManualSkiHeadingState InitialPose {get;}
        public ManualSkiHeadingState State {get;}
        public SkiContactMotion InitialMotion {get;}
        public SkiSnowContactPreview Snow {get;}
        public double DurationSeconds {get;}
        public bool IsValid=>DurationSeconds>0&&InitialPose.IsValid&&Snow.IsValid;
        public double PeakYawRate=>Math.Max(Math.Abs(InitialMotion.YawRate),Math.Abs(Snow.Motion.YawRate));
        public double PlanarAccelerationBound
        {
            get
            {
                double x=Snow.Motion.Velocity.X-InitialMotion.Velocity.X,z=Snow.Motion.Velocity.Z-InitialMotion.Velocity.Z;
                return Math.Sqrt(x*x+z*z)/DurationSeconds;
            }
        }
        public double PositionErrorBoundMeters
        {
            get
            {
                double magnitude=Math.Abs(InitialPose.X)+Math.Abs(InitialPose.Z)+1+
                    DurationSeconds*(Math.Abs(InitialMotion.Velocity.X)+Math.Abs(InitialMotion.Velocity.Z)+Math.Abs(Snow.Motion.Velocity.X)+Math.Abs(Snow.Motion.Velocity.Z));
                return 1e-9+64*2.2204460492503131e-16*magnitude;
            }
        }
        public double HeadingErrorBoundRadians=>2e-9*(1+PeakYawRate*DurationSeconds);

        internal SkiContactTrajectory(ManualSkiHeadingState pose,SkiContactMotion initial,SkiSnowContactPreview snow,double seconds)
        {
            InitialPose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,initial.YawRate);
            InitialMotion=initial;Snow=snow;DurationSeconds=seconds;State=default;
            ValidateAxis(pose.X,initial.Velocity.X,snow.Motion.Velocity.X,seconds);
            ValidateAxis(pose.Z,initial.Velocity.Z,snow.Motion.Velocity.Z,seconds);
            State=Sample(1);
        }

        /// <summary>Samples the same complete frozen interval; do not recalculate a
        /// different duration's snow impulse when accepting a prefix.</summary>
        public ManualSkiHeadingState Sample(double fraction)
        {
            Validate(fraction);
            if(fraction==0)return InitialPose;
            var motion=MotionAt(fraction);double time=DurationSeconds*fraction;
            return new ManualSkiHeadingState(
                InitialPose.X+time*(InitialMotion.Velocity.X+motion.Velocity.X)*.5,
                InitialPose.Z+time*(InitialMotion.Velocity.Z+motion.Velocity.Z)*.5,
                InitialPose.HeadingRadians+time*(InitialMotion.YawRate+motion.YawRate)*.5,motion.YawRate);
        }
        public SkiContactMotion MotionAt(double fraction)
        {
            Validate(fraction);
            if(fraction==0)return InitialMotion;
            if(fraction==1)return Snow.Motion;
            return new SkiContactMotion(new SkiPlanarVelocity(
                InitialMotion.Velocity.X+(Snow.Motion.Velocity.X-InitialMotion.Velocity.X)*fraction,
                InitialMotion.Velocity.Z+(Snow.Motion.Velocity.Z-InitialMotion.Velocity.Z)*fraction),
                InitialMotion.YawRate+(Snow.Motion.YawRate-InitialMotion.YawRate)*fraction);
        }
        private void Validate(double fraction)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized contact trajectory required.");
            PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));
        }
        private static void ValidateAxis(double origin,double initial,double final,double seconds)
        {
            Check(origin+seconds*(initial+final)*.5);
            double delta=final-initial;
            if(delta!=0)
            {
                double fraction=-initial/delta;
                if(fraction>0&&fraction<1)Check(origin+seconds*fraction*(initial+delta*fraction*.5));
            }
            void Check(double value)=>PhysiologyNumbers.RequireRange(value,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,"contact trajectory coordinate");
        }
    }

    /// <summary>Allocation-free sum type for commanded skiing or post-contact
    /// continuation. Keeps the same sweep contract for both actors.</summary>
    public readonly struct ManualSkiWorldTrajectory
    {
        private readonly ManualSkiHeadingFrame heading;
        private readonly SkiContactTrajectory contact;
        public bool IsValid=>heading.HasForceTrajectory||contact.IsValid;
        public double DurationSeconds=>contact.IsValid?contact.DurationSeconds:heading.DurationSeconds;
        public double PeakYawRate=>contact.IsValid?contact.PeakYawRate:heading.PeakYawRate;
        public double PlanarAccelerationBound=>contact.IsValid?contact.PlanarAccelerationBound:heading.PlanarAccelerationBound;
        public double PositionErrorBoundMeters=>contact.IsValid?contact.PositionErrorBoundMeters:heading.PositionErrorBoundMeters;
        public double HeadingErrorBoundRadians=>contact.IsValid?contact.HeadingErrorBoundRadians:heading.HeadingErrorBoundRadians;
        public ManualSkiWorldTrajectory(ManualSkiHeadingFrame frame)
        {
            if(!frame.HasForceTrajectory)throw new ArgumentException("Force-weighted heading required.");
            heading=frame;contact=default;
        }
        public ManualSkiWorldTrajectory(SkiContactTrajectory frame)
        {
            if(!frame.IsValid)throw new ArgumentException("Initialized contact trajectory required.");
            heading=default;contact=frame;
        }
        public ManualSkiHeadingState Sample(double fraction)=>contact.IsValid?contact.Sample(fraction):heading.Sample(fraction);
        public SkiPlanarVelocity VelocityAt(double fraction)=>contact.IsValid?contact.MotionAt(fraction).Velocity:heading.VelocityAt(fraction);
        /// <summary>Physical travel along the selected world trajectory. Contact
        /// side-slip counts as distance even when projected course credit is zero.</summary>
        public double PlanarTravelDistanceAt(double fraction)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized world trajectory required.");
            PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));
            if(!contact.IsValid)
            {
                double total=heading.TravelDistanceAt(1);
                return total==0?0:heading.TravelDistanceAt(fraction)*heading.PlanarTravelDistanceMeters/total;
            }
            if(fraction==0)return 0;
            // Integrate the norm of the exact linear contact velocity ramp.
            // Fixed panels keep deterministic replay independent of frame rate.
            const int panels=64;
            double step=fraction/panels,sum=0;
            for(int i=0;i<=panels;i++)
            {
                var v=contact.MotionAt(i*step).Velocity;
                double speed=Math.Sqrt(v.X*v.X+v.Z*v.Z);
                sum+=(i==0||i==panels?1:i%2==0?2:4)*speed;
            }
            return sum*step*contact.DurationSeconds/3;
        }
        public double PhysicalTravelDistanceAt(double fraction,double directionalGrade)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized world trajectory required.");
            PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));
            PhysiologyNumbers.RequireRange(directionalGrade,-PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude,nameof(directionalGrade));
            return contact.IsValid ? PlanarTravelDistanceAt(fraction)*Math.Sqrt(1+directionalGrade*directionalGrade)
                : heading.TravelDistanceAt(fraction);
        }
        public double PhysicalSpeedAt(double fraction,double directionalGrade)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized world trajectory required.");
            PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));
            PhysiologyNumbers.RequireRange(directionalGrade,-PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude,nameof(directionalGrade));
            if(!contact.IsValid)return heading.SlopeSpeedAt(fraction);
            var velocity=contact.MotionAt(fraction).Velocity;
            return Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z)*
                Math.Sqrt(1+directionalGrade*directionalGrade);
        }
    }
}

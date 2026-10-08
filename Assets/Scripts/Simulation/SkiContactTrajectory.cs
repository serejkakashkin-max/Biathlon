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
        private const int ContactPanels=64;
        public const string SlicePolicy="frozen_root_interval_slices_v1;force_cumulative_difference;contact_slice_positive_norm_simpson64;whole_query_policy_unchanged";
        private readonly ManualSkiHeadingFrame heading;
        private readonly SkiContactTrajectory contact;
        private readonly SkiDrivenContactTrajectory driven;
        private readonly double fromSeconds,toSeconds;
        public bool IsValid=>heading.HasForceTrajectory||contact.IsValid||driven.IsValid;
        private bool HasContact=>contact.IsValid||driven.IsValid;
        private double RootDurationSeconds=>driven.IsValid?driven.DurationSeconds:contact.IsValid?contact.DurationSeconds:heading.DurationSeconds;
        public double DurationSeconds=>toSeconds-fromSeconds;
        public double PeakYawRate=>driven.IsValid?driven.PeakYawRate:contact.IsValid?contact.PeakYawRate:heading.PeakYawRate;
        public double PlanarAccelerationBound=>driven.IsValid?driven.PlanarAccelerationBound:contact.IsValid?contact.PlanarAccelerationBound:heading.PlanarAccelerationBound;
        public double PositionErrorBoundMeters=>driven.IsValid?driven.PositionErrorBoundMeters:contact.IsValid?contact.PositionErrorBoundMeters:heading.PositionErrorBoundMeters;
        public double HeadingErrorBoundRadians=>driven.IsValid?driven.HeadingErrorBoundRadians:contact.IsValid?contact.HeadingErrorBoundRadians:heading.HeadingErrorBoundRadians;
        public ManualSkiWorldTrajectory(ManualSkiHeadingFrame frame)
        {
            if(!frame.HasForceTrajectory)throw new ArgumentException("Force-weighted heading required.");
            heading=frame;contact=default;driven=default;fromSeconds=0;toSeconds=frame.DurationSeconds;
        }
        public ManualSkiWorldTrajectory(SkiContactTrajectory frame)
        {
            if(!frame.IsValid)throw new ArgumentException("Initialized contact trajectory required.");
            heading=default;contact=frame;driven=default;fromSeconds=0;toSeconds=frame.DurationSeconds;
        }
        public ManualSkiWorldTrajectory(SkiDrivenContactTrajectory frame)
        {
            if(!frame.IsValid)throw new ArgumentException("Initialized driven contact trajectory required.");
            heading=default;contact=default;driven=frame;fromSeconds=0;toSeconds=frame.DurationSeconds;
        }
        private ManualSkiWorldTrajectory(ManualSkiHeadingFrame heading,SkiContactTrajectory contact,SkiDrivenContactTrajectory driven,double from,double to)
        {this.heading=heading;this.contact=contact;this.driven=driven;fromSeconds=from;toSeconds=to;}
        /// <summary>Reuse a portion of the same frozen force/snow interval. Does
        /// not recalculate an impulse, apply a command twice or admit any body.
        /// Times are relative to this slice; nested slices keep the original root.</summary>
        public ManualSkiWorldTrajectory Slice(double from,double to)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized world trajectory required.");
            PhysiologyNumbers.RequireRange(from,0,DurationSeconds,nameof(from));
            PhysiologyNumbers.RequireRange(to,0,DurationSeconds,nameof(to));
            if(to<=from)throw new ArgumentException("A positive frozen interval is required.");
            double start=from==0?fromSeconds:fromSeconds+from,end=to==DurationSeconds?toSeconds:fromSeconds+to;
            if(end<=start)throw new ArgumentException("Frozen interval is below the source clock's resolution.");
            return new ManualSkiWorldTrajectory(heading,contact,driven,start,end);
        }
        private double RootFraction(double fraction)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized world trajectory required.");
            PhysiologyNumbers.RequireRange(fraction,0,1,nameof(fraction));
            // Preserve the exact pre-slice whole-trajectory query policy.
            if(fromSeconds==0&&toSeconds==RootDurationSeconds)return fraction;
            double time=fraction==0?fromSeconds:fraction==1?toSeconds:fromSeconds+DurationSeconds*fraction;
            return time/RootDurationSeconds;
        }
        public ManualSkiHeadingState Sample(double fraction)
        {double root=RootFraction(fraction);return driven.IsValid?driven.Sample(root):contact.IsValid?contact.Sample(root):heading.Sample(root);}
        public SkiPlanarVelocity VelocityAt(double fraction)
        {double root=RootFraction(fraction);return HasContact?ContactMotionAt(root).Velocity:heading.VelocityAt(root);}
        private SkiContactMotion ContactMotionAt(double fraction)=>driven.IsValid?driven.MotionAt(fraction):contact.MotionAt(fraction);
        /// <summary>Physical travel along the selected world trajectory. Contact
        /// side-slip counts as distance even when projected course credit is zero.</summary>
        public double PlanarTravelDistanceAt(double fraction)
        {
            double root=RootFraction(fraction);
            if(fraction==0)return 0;
            if(HasContact&&(fromSeconds!=0||toSeconds!=RootDurationSeconds))
                return ContactSliceTravel(fromSeconds/RootDurationSeconds,root);
            return RootPlanarTravelDistanceAt(root)-(fromSeconds==0?0:RootPlanarTravelDistanceAt(fromSeconds/RootDurationSeconds));
        }
        private double ContactSliceTravel(double from,double to)
        {
            // Integrate the nonnegative norm on this actual slice. Subtracting
            // two independently rescaled quadratures is not monotone near a
            // velocity zero and can invent negative travel for a tiny interval.
            // Fragment sums have the usual quadrature error, not an exact claim
            // of bitwise telescoping to the whole interval's approximation.
            double step=(to-from)/ContactPanels,sum=0;
            for(int i=0;i<=ContactPanels;i++)
            {
                double at=i==0?from:i==ContactPanels?to:from+i*step;
                var v=ContactMotionAt(at).Velocity;
                double speed=Math.Sqrt(v.X*v.X+v.Z*v.Z);
                sum+=(i==0||i==ContactPanels?1:i%2==0?2:4)*speed;
            }
            return sum*step*RootDurationSeconds/3;
        }
        private double RootPlanarTravelDistanceAt(double fraction)
        {
            if(!HasContact)
            {
                double total=heading.TravelDistanceAt(1);
                return total==0?0:heading.TravelDistanceAt(fraction)*heading.PlanarTravelDistanceMeters/total;
            }
            if(fraction==0)return 0;
            // Integrate the norm of the exact linear contact velocity ramp.
            // Fixed panels keep deterministic replay independent of frame rate.
            const int panels=ContactPanels;
            double step=fraction/panels,sum=0;
            for(int i=0;i<=panels;i++)
            {
                var v=ContactMotionAt(i*step).Velocity;
                double speed=Math.Sqrt(v.X*v.X+v.Z*v.Z);
                sum+=(i==0||i==panels?1:i%2==0?2:4)*speed;
            }
            return sum*step*RootDurationSeconds/3;
        }
        public double PhysicalTravelDistanceAt(double fraction,double directionalGrade)
        {
            double root=RootFraction(fraction);
            PhysiologyNumbers.RequireRange(directionalGrade,-PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude,nameof(directionalGrade));
            return HasContact ? PlanarTravelDistanceAt(fraction)*Math.Sqrt(1+directionalGrade*directionalGrade)
                : heading.TravelDistanceAt(root)-(fromSeconds==0?0:heading.TravelDistanceAt(fromSeconds/RootDurationSeconds));
        }
        public double PhysicalSpeedAt(double fraction,double directionalGrade)
        {
            double root=RootFraction(fraction);
            PhysiologyNumbers.RequireRange(directionalGrade,-PhysiologyNumbers.MaximumMagnitude,
                PhysiologyNumbers.MaximumMagnitude,nameof(directionalGrade));
            if(!HasContact)return heading.SlopeSpeedAt(root);
            var velocity=ContactMotionAt(root).Velocity;
            return Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z)*
                Math.Sqrt(1+directionalGrade*directionalGrade);
        }
    }
}

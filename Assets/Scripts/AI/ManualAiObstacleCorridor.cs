using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>AI-only corridor query for an observed stationary obstacle.
    /// Full rotated body dimensions supply clearance. This is an intent query;
    /// it never changes poses or grants contact/course admission.</summary>
    public sealed class ManualAiObstacleCorridor
    {
        private readonly OccupancyDefinition body;
        public ManualAiObstacleCorridor(OccupancyDefinition body)
            =>this.body=body??throw new ArgumentNullException(nameof(body));

        /// <summary>Braking intent for an already forecast moving obstruction.
        /// Match its achieved forward velocity within the actual closing envelope;
        /// the shared force solver still owns the actual acceleration and cost.</summary>
        public double ClosingBrakeFraction(SkiPlanarVelocity velocity,SkiPlanarVelocity otherVelocity,
            double forwardX,double forwardZ,CorneringDefinition corners,ManualAiSteeringDefinition policy,
            double secondsUntilContact=0)
        {
            if(corners==null||policy==null)throw new ArgumentNullException();
            PhysiologyNumbers.RequireRange(secondsUntilContact,0,PhysiologyNumbers.MaximumMagnitude,nameof(secondsUntilContact));
            double closing=(velocity.X-otherVelocity.X)*forwardX+(velocity.Z-otherVelocity.Z)*forwardZ;
            // A long observation horizon is useful for passing, but does not
            // imply that propulsion must stop now. Brake when the closing
            // distance enters the shared deceleration + response envelope.
            // Otherwise a small distant speed difference makes every uphill
            // follower coast and compounds into a nearly stationary tail.
            if(secondsUntilContact>policy.HeadingResponseSeconds+Math.Max(0,closing)/(2*corners.MaximumBraking))return 0;
            return Math.Max(0,Math.Min(1,closing/(corners.MaximumBraking*policy.HeadingResponseSeconds)));
        }

        /// <summary>Constant achieved-velocity forecast for two complete rotated
        /// bodies. This is an AI planning warning, not physical contact admission.
        /// All four separating axes must have overlapping time intervals.</summary>
        public bool TryClosingInterval(ManualSkiHeadingState self,SkiPlanarVelocity velocity,
            ManualSkiHeadingState other,SkiPlanarVelocity otherVelocity,double horizonSeconds,out double seconds)
        {
            PhysiologyNumbers.RequireRange(horizonSeconds,0,PhysiologyNumbers.MaximumMagnitude,nameof(horizonSeconds));
            double dx=other.X-self.X,dz=other.Z-self.Z;
            double vx=otherVelocity.X-velocity.X,vz=otherVelocity.Z-velocity.Z;
            seconds=0;
            if(dx*vx+dz*vz>=0)return false;
            double afx=Math.Sin(self.HeadingRadians),afz=Math.Cos(self.HeadingRadians);
            double bfx=Math.Sin(other.HeadingRadians),bfz=Math.Cos(other.HeadingRadians);
            double enter=0,exit=horizonSeconds;
            if(!Axis(afx,afz)||!Axis(afz,-afx)||!Axis(bfx,bfz)||!Axis(bfz,-bfx))return false;
            seconds=enter;return enter<=exit;

            bool Axis(double x,double z)
            {
                double extent=.5*body.LongitudinalSeparation*(Math.Abs(afx*x+afz*z)+Math.Abs(bfx*x+bfz*z))+
                    .5*body.LateralSeparation*(Math.Abs(afz*x-afx*z)+Math.Abs(bfz*x-bfx*z));
                double position=dx*x+dz*z,change=vx*x+vz*z;
                if(change==0)return Math.Abs(position)<=extent;
                double a=(-extent-position)/change,b=(extent-position)/change;
                enter=Math.Max(enter,Math.Min(a,b));exit=Math.Min(exit,Math.Max(a,b));return enter<=exit;
            }
        }

        public bool Overlaps(ManualSkiHeadingState self,SkiPlanarVelocity velocity,
            double forwardX,double forwardZ,double currentLateral,
            ManualSkiHeadingState other,double candidateLateral,bool anticipate)
        {
            double dx=other.X-self.X,dz=other.Z-self.Z;
            double line=currentLateral+dx*forwardZ-dz*forwardX;
            double ah=self.HeadingRadians,bh=other.HeadingRadians;
            double separation=.5*body.LongitudinalSeparation*
                (Math.Abs(Math.Sin(ah)*forwardZ-Math.Cos(ah)*forwardX)+Math.Abs(Math.Sin(bh)*forwardZ-Math.Cos(bh)*forwardX))+
                .5*body.LateralSeparation*
                (Math.Abs(Math.Cos(ah)*forwardZ+Math.Sin(ah)*forwardX)+Math.Abs(Math.Cos(bh)*forwardZ+Math.Sin(bh)*forwardX));
            double predicted=candidateLateral;
            double forwardSpeed=velocity.X*forwardX+velocity.Z*forwardZ;
            double ahead=dx*forwardX+dz*forwardZ;
            if(anticipate&&forwardSpeed>0&&ahead>0)
                predicted=currentLateral+(velocity.X*forwardZ-velocity.Z*forwardX)*ahead/forwardSpeed;
            double low=Math.Min(candidateLateral,predicted),high=Math.Max(candidateLateral,predicted);
            return line>low-separation&&line<high+separation;
        }
    }
}

using System;
namespace Biathlon.Simulation
{
    /// <summary>Read-only strongest reached-body impulse in one accepted tick.
    /// No force, movement, recovery timer or control authority is stored here.</summary>
    public readonly struct SkiImpactObservation
    {
        public double DeltaX {get;}public double DeltaZ {get;}public double DeltaYaw {get;}
        public double SpeedChange=>Math.Sqrt(DeltaX*DeltaX+DeltaZ*DeltaZ);
        internal SkiImpactObservation(double x,double z,double yaw){DeltaX=x;DeltaZ=z;DeltaYaw=yaw;}
        internal SkiImpactObservation Include(double x,double z,double yaw)
            =>x*x+z*z>DeltaX*DeltaX+DeltaZ*DeltaZ?new SkiImpactObservation(x,z,yaw):this;
    }
}

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

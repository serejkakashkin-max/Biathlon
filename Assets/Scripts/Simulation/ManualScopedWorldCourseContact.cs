using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed partial class ManualScopedWorldRoster
    {
        private readonly ManualCourseContactDefinition courseContact;
        private readonly double[] courseNormalsX,courseNormalsZ,courseLevers;
        private readonly bool[] courseSupports;
        private readonly int[] courseImpactCounts;
        public int CourseEdgeResponses {get;private set;}
        public int GetCourseEdgeResponses(int actor)=>courseImpactCounts[actor];
        /// <summary>Resolve a reached local snow edge as a passive contact.
        /// Position, heading, progress, time and odometer are never corrected.
        /// The normal impulse removes closing motion; the registered reaction
        /// then opposes outward drive through the same paid interval.</summary>
        internal bool TryRespondToCourseEdge(ManualScopedWorldTick tick)
        {
            ValidateTick(tick);
            if(courseContact==null||LastAttempt.Status!=ManualScopedWorldStatus.Course||
                CourseEdgeResponses>=courseContact.MaximumResponses)return false;
            int index=-1;for(int i=0;i<Count;i++)if(workingActors[i].Athlete.Equals(LastAttempt.Athlete)){index=i;break;}
            if(index<0||IsGroundedMat(index))return false;
            var actor=workingActors[index];var pose=actor.Pose;var ribbon=ribbons[index];
            // Respond to the boundary actually threatened by the proposed
            // motion. At a rounded corner the source tangent's normal can
            // differ from the snow edge reached later in this same interval.
            var threatened=ribbon.LastThreatenedPose;
            double h=courseContact.NormalProbe,w=courseContact.AngularProbe;
            double gx=(At(h,0,0)-At(-h,0,0))/(2*h),gz=(At(0,h,0)-At(0,-h,0))/(2*h);
            double gh=(At(0,0,w)-At(0,0,-w))/(2*w),norm=Math.Sqrt(gx*gx+gz*gz);
            // An intersecting rectangle/polygon edge yields a flat conservative
            // clearance. Use the continuous disk cover to obtain its snow normal.
            if(norm<PhysiologyNumbers.MinimumPositive)
            {
                gx=(CoverAt(h,0,0)-CoverAt(-h,0,0))/(2*h);
                gz=(CoverAt(0,h,0)-CoverAt(0,-h,0))/(2*h);
                gh=(CoverAt(0,0,w)-CoverAt(0,0,-w))/(2*w);
                norm=Math.Sqrt(gx*gx+gz*gz);
            }
            if(double.IsNaN(norm)||double.IsInfinity(norm)||norm<PhysiologyNumbers.MinimumPositive||double.IsNaN(gh)||double.IsInfinity(gh))return false;
            gx/=norm;gz/=norm;gh/=norm;
            courseNormalsX[index]=gx;courseNormalsZ[index]=gz;courseLevers[index]=gh;courseSupports[index]=true;
            double inertia=Inertia(index),responseLever=courseContact.ArrestContactSpin?0:gh;
            double inverse=1/masses[index]+responseLever*responseLever/inertia;
            double closing=gx*actor.Velocity.X+gz*actor.Velocity.Z+responseLever*pose.YawRateRadiansPerSecond;
            double impulse=Math.Max(0,courseContact.SeparationBiasSpeed-closing)/inverse;
            var velocity=new SkiPlanarVelocity(actor.Velocity.X+gx*impulse/masses[index],actor.Velocity.Z+gz*impulse/masses[index]);
            // Snow support may arrest rotation into its edge. Rotation that
            // increases clearance remains available to steer back onto snow.
            double spin=courseContact.ArrestContactSpin
                ?(gh*pose.YawRateRadiansPerSecond<0?0:pose.YawRateRadiansPerSecond)
                :pose.YawRateRadiansPerSecond+gh*impulse/inertia;
            var nextPose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians,spin);
            workingActors[index]=new ManualRacePathActorFrame(actor.Athlete,nextPose,velocity,actor.Observation);
            double speed=Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z);
            double grade=speed==0?0:actor.Observation.GeometricGrade*(velocity.X*actor.Observation.ForwardX+velocity.Z*actor.Observation.ForwardZ)/speed;
            workingMotion[index]=new SkiState(workingMotion[index].DistanceNanometers,speed*Math.Sqrt(1+grade*grade));
            ownedFreeValid[index]=false;courseImpactCounts[index]++;CourseEdgeResponses++;return true;
            double At(double x,double z,double yaw)=>ribbon.CurrentRibbonClearance(new ManualSkiHeadingState(threatened.X+x,threatened.Z+z,threatened.HeadingRadians+yaw,threatened.YawRateRadiansPerSecond));
            double CoverAt(double x,double z,double yaw)=>ribbon.CurrentCoverClearance(new ManualSkiHeadingState(threatened.X+x,threatened.Z+z,threatened.HeadingRadians+yaw,threatened.YawRateRadiansPerSecond));
        }
        private bool IsGroundedMat(int actor)
        {
            var cursor=workingCursors[actor];var row=catalogs[actor].Get(cursor.OccurrenceIndex);
            return matCapture!=null&&row.End==ManualRacePathEnd.FiringLine&&cursor.ObservedArcNanometers==row.LengthNanometers;
        }
        private double Inertia(int actor)=>masses[actor]*(occupiedBody.LongitudinalSeparation*occupiedBody.LongitudinalSeparation+
            occupiedBody.LateralSeparation*occupiedBody.LateralSeparation)/12;
        private SkiForceCoupledTrajectory ApplyCourseReaction(int index,SkiForceCoupledTrajectory free)
        {
            if(!courseSupports[index])return free;
            double gx=courseNormalsX[index],gz=courseNormalsZ[index],gh=courseLevers[index],inertia=Inertia(index);
            var a=free.Acceleration;
            if(courseContact.ArrestContactSpin)
            {
                double normalReaction=-Math.Min(0,gx*a.X+gz*a.Z)*masses[index];
                return new SkiForceCoupledTrajectory(free.InitialPose,free.InitialMotion,
                    new SkiContactAcceleration(a.X+gx*normalReaction/masses[index],a.Z+gz*normalReaction/masses[index],
                        gh*a.Yaw<0?0:a.Yaw),free.DurationSeconds);
            }
            double outward=gx*a.X+gz*a.Z+gh*a.Yaw;
            double reaction=-Math.Min(0,outward)/(1/masses[index]+gh*gh/inertia);
            double initialClearance=ribbons[index].CurrentRibbonClearance(free.InitialPose);
            // Preserve the already admitted skin. Trying to eliminate its
            // submillimetric overlap in a vanishing interval creates an
            // unbounded, energy-producing positional spring.
            double target=Math.Min(0,initialClearance);
            SkiForceCoupledTrajectory result=free;
            for(int attempt=0;attempt<courseContact.MaximumResponses;attempt++)
            {
                result=new SkiForceCoupledTrajectory(free.InitialPose,free.InitialMotion,
                    new SkiContactAcceleration(a.X+gx*reaction/masses[index],a.Z+gz*reaction/masses[index],a.Yaw+gh*reaction/inertia),free.DurationSeconds);
                double correction=0;
                for(int k=1;k<=4;k++)
                {
                    double t=free.DurationSeconds*k/4;
                    double gap=ribbons[index].CurrentRibbonClearance(result.Sample((double)k/4));
                    if(gap<target)correction=Math.Max(correction,-2*(gap-target)/(t*t));
                }
                if(correction==0)break;
                reaction+=correction/(1/masses[index]+gh*gh/inertia);
            }
            return result;
        }
    }
}

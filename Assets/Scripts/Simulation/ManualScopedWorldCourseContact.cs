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
        public int CurvedCourseContinuations {get;private set;}
        public double LastCourseNormalX {get;private set;}
        public double LastCourseNormalZ {get;private set;}
        public double LastCourseAngularLever {get;private set;}
        public ManualSkiHeadingState LastCourseThreatenedPose {get;private set;}
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
            LastCourseNormalX=gx;LastCourseNormalZ=gz;LastCourseAngularLever=gh;LastCourseThreatenedPose=threatened;
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
            double At(double x,double z,double yaw)
            {
                var at=new ManualSkiHeadingState(threatened.X+x,threatened.Z+z,threatened.HeadingRadians+yaw,threatened.YawRateRadiansPerSecond);
                return courseContact.NormalSampleColumns>0?ribbon.CurrentNormalSupportClearance(at,courseContact.NormalSampleColumns,courseContact.NormalSampleRows):ribbon.CurrentRibbonClearance(at);
            }
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
                var supported=new SkiForceCoupledTrajectory(free.InitialPose,free.InitialMotion,
                    new SkiContactAcceleration(a.X+gx*normalReaction/masses[index],a.Z+gz*normalReaction/masses[index],
                        gh*a.Yaw<0?0:a.Yaw),free.DurationSeconds);
                if(CourseReactionPolicy==ManualCourseReactionPolicy.LegacyV25)return supported;
                // Preserve the historical force law exactly whenever it already
                // certifies the complete course interval. Curved continuation
                // is needed only for the formerly rejected boundary case.
                var trajectory=new ManualSkiWorldTrajectory(supported);
                bool clear=catalogs[index].ProjectionGeometry.TryGetCollinearCorridor(workingActors[index].Observation.GeometryProjection,out var cell);
                for(int k=0;clear&&k<4;k++)
                {
                    var plane=k==0?cell.Left:k==1?cell.Right:k==2?cell.Entry:cell.Exit;
                    if(courseCells.Preview(trajectory,plane).Status!=SkiEdgeSweepStatus.Clear)clear=false;
                }
                if(clear)return supported;
                var admission=ribbons[index].Preview(trajectory,workingActors[index].Observation.GeometryProjection);
                if(admission.Status==ManualRouteTubeStatus.Clear&&
                    Math.Abs(admission.ClearUntilSeconds-free.DurationSeconds)<=free.DurationSeconds*1e-12)return supported;
                CurvedCourseContinuations++;
                a=new SkiContactAcceleration(a.X,a.Z,gh*a.Yaw<0?0:a.Yaw);
            }
            double responseLever=courseContact.ArrestContactSpin?0:gh;
            double outward=gx*a.X+gz*a.Z+responseLever*a.Yaw;
            double reaction=-Math.Min(0,outward)/(1/masses[index]+responseLever*responseLever/inertia);
            double initialClearance=ribbons[index].CurrentRibbonClearance(free.InitialPose);
            // Preserve the already admitted skin. Trying to eliminate its
            // existing clearance overlap in a vanishing interval creates an
            // unbounded, energy-producing positional spring.
            double target=Math.Min(0,initialClearance);
            double bodyRadius=.5*Math.Sqrt(occupiedBody.LongitudinalSeparation*occupiedBody.LongitudinalSeparation+
                occupiedBody.LateralSeparation*occupiedBody.LateralSeparation);
            double encodingReserve=2*free.PositionErrorBoundMeters+bodyRadius*free.HeadingErrorBoundRadians+
                128*2.2204460492503131e-16*(1+Math.Abs(free.InitialPose.X)+Math.Abs(free.InitialPose.Z));
            double admissionReserve=encodingReserve-(numericalTolerance?.CourseSweepTolerance??0);
            SkiForceCoupledTrajectory result=free;
            for(int attempt=0;attempt<courseContact.MaximumResponses;attempt++)
            {
                double ax=a.X+gx*reaction/masses[index],az=a.Z+gz*reaction/masses[index];
                if(courseContact.ArrestContactSpin)
                {
                    // Support redirects the paid motion; it is not propulsion.
                    // A finite constant-curvature piece otherwise gains kinetic
                    // energy from its normal acceleration, especially on an
                    // already admitted conservative skin near a knot.
                    var initial=free.InitialMotion.Velocity;
                    double initialSquared=initial.X*initial.X+initial.Z*initial.Z;
                    double work=(ax-a.X)*initial.X+(az-a.Z)*initial.Z;
                    if(work>0&&initialSquared>0)
                    {ax-=initial.X*work/initialSquared;az-=initial.Z*work/initialSquared;}
                    double seconds=free.DurationSeconds;
                    double vx=initial.X+ax*seconds,vz=initial.Z+az*seconds;
                    double paidX=initial.X+a.X*seconds,paidZ=initial.Z+a.Z*seconds;
                    double paidSquared=paidX*paidX+paidZ*paidZ,resultSquared=vx*vx+vz*vz;
                    // Factor the velocity-energy difference by duration before
                    // radial projection. Subtracting two nearly equal endpoint
                    // velocities and dividing by a tiny contact prefix turns
                    // ordinary rounding into an unbounded acceleration.
                    double energyRate=2*(initial.X*(ax-a.X)+initial.Z*(az-a.Z))+
                        seconds*((ax-a.X)*(ax+a.X)+(az-a.Z)*(az+a.Z));
                    if(energyRate>0&&resultSquared>0)
                    {
                        double rate=-energyRate/(resultSquared+Math.Sqrt(paidSquared*resultSquared));
                        ax+=vx*rate;az+=vz*rate;
                    }
                }
                result=new SkiForceCoupledTrajectory(free.InitialPose,free.InitialMotion,
                    new SkiContactAcceleration(ax,az,a.Yaw+responseLever*reaction/inertia),free.DurationSeconds);
                double correction=0;
                var velocity=free.InitialMotion.Velocity;double duration=free.DurationSeconds;
                double displacement=duration*Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z)+
                    .5*duration*duration*result.PlanarAccelerationBound+bodyRadius*result.PeakYawRate*duration;
                // A small unrelated contact interval can already be certified
                // from the source clearance and the complete displacement bound.
                // Do not invent an exact-source-gap spring under that safe piece.
                if(courseContact.ArrestContactSpin&&initialClearance>admissionReserve+displacement)break;
                for(int k=1;k<=4;k++)
                {
                    double t=free.DurationSeconds*k/4;
                    double gap=ribbons[index].CurrentRibbonClearance(result.Sample((double)k/4));
                    if(gap<target)correction=Math.Max(correction,-2*(gap-target)/(t*t));
                }
                if(correction==0)break;
                reaction+=correction/(1/masses[index]+responseLever*responseLever/inertia);
            }
            return result;
        }
    }
}

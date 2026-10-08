using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum SkiContactForceStatus { Resolved, Penetrating, UnresolvedGeometry, VelocityResponseRequired, WorkLimit }
    public readonly struct SkiContactForcePreview
    {
        public bool IsValid {get;}
        public SkiContactForceStatus Status {get;}
        public double DurationSeconds {get;}
        public int SupportPoints {get;} public int Passes {get;}
        public double MaximumClosingAcceleration {get;}
        public double MaximumReactionResidualSpeed {get;}
        internal SkiContactForcePreview(SkiContactForceStatus status,double seconds,int points,int passes=0,double closing=0,double residual=0)
        {IsValid=true;Status=status;DurationSeconds=seconds;SupportPoints=points;Passes=passes;MaximumClosingAcceleration=closing;MaximumReactionResidualSpeed=residual;}
    }
    public readonly struct SkiContactForceReaction
    {
        public AthleteId First {get;} public AthleteId Second {get;}
        public double X {get;} public double Z {get;}
        public double NormalNewtons {get;} public double TangentNewtons {get;}
        internal SkiContactForceReaction(AthleteId first,AthleteId second,double x,double z,double normal,double tangent)
        {First=first;Second=second;X=x;Z=z;NormalNewtons=normal;TangentNewtons=tangent;}
    }
    /// <summary>Bounded stable-ID force complementarity at reached contacts.
    /// Uses up to two support endpoints per rectangle pair, exact occupied-body
    /// inertia and the existing friction cone. Restitution applies to impacts
    /// only. A resolved frozen preview still needs whole-body/course admission.
    /// Caller output is written only after every provisional trajectory validates.</summary>
    public sealed class ManualSkiContactForces
    {
        private struct Point
        {
            internal int A,B;
            internal AthleteId First,Second;
            internal double X,Z,Nx,Nz,An,At,Bn,Bt,Knn,Ktt,Knt,Normal,Tangent,Sliding,InitialTangentSpeed;
        }
        private readonly ManualSkiContactSystem geometry;
        private readonly SkiContactGroupDefinition work;
        private readonly double friction,inertiaPerMass;
        private readonly int[] order;
        private readonly SkiContactAcceleration[] accelerations;
        private readonly SkiForceCoupledTrajectory[] staged;
        private readonly Point[] points;
        private int pointCount;
        public string ContextJson {get;}
        public SkiContactForcePreview LastPreview {get;private set;}
        public ManualSkiContactForces(OccupancyDefinition body,SkiContactDefinition contact,SkiContactGroupDefinition work,int capacity)
        {
            if(body==null||contact==null||work==null)throw new ArgumentNullException();
            if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));
            geometry=new ManualSkiContactSystem(body,contact);this.work=work;friction=contact.SlidingFriction;
            inertiaPerMass=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            order=new int[capacity];accelerations=new SkiContactAcceleration[capacity];staged=new SkiForceCoupledTrajectory[capacity];
            points=new Point[checked(capacity*(capacity-1))];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_frozen_contact_forces_v1",geometry.ContextJson,work.ToJson(),
                "two_support_endpoints;stable_id_pgs;unilateral_accumulated_reactions;static_or_sliding_coulomb_cone;no_force_restitution;raw_initial_motion;"+
                "shared_drive_constant_prefix_only;physical_centripetal_point_acceleration;convergence_min_configured_speed_and_64ulp_scale;"+
                "first_frozen_slip_zero_boundary;all_trajectories_validate_before_output;full_sweep_still_required"});
        }
        public SkiContactForceReaction GetReaction(int index)
        {
            if(!LastPreview.IsValid||index<0||index>=pointCount)throw new ArgumentOutOfRangeException(nameof(index));
            var p=points[index];return new SkiContactForceReaction(p.First,p.Second,p.X,p.Z,p.Normal,p.Tangent);
        }
        public SkiContactForcePreview Preview(ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,int count,SkiForceCoupledTrajectory[] output)
        {
            LastPreview=default;pointCount=0;
            if(bodies==null||free==null||output==null||count<1||count>order.Length||count>bodies.Length||count>free.Length||count>output.Length)
                throw new ArgumentException("Complete bodies, drives and output within cold capacity required.");
            double duration=free[0].DurationSeconds,seconds=duration;
            for(int i=0;i<count;i++)
            {
                var b=bodies[i];var f=free[i];
                if(!b.IsValid||!f.IsValid||f.DurationSeconds!=duration||f.InitialPose.X!=b.Pose.X||f.InitialPose.Z!=b.Pose.Z||
                    f.InitialPose.HeadingNanoradians!=b.Pose.HeadingNanoradians||f.InitialPose.YawRateRadiansPerSecond!=b.Pose.YawRateRadiansPerSecond||f.InitialMotion.Velocity.X!=b.Velocity.X||
                    f.InitialMotion.Velocity.Z!=b.Velocity.Z||f.MassKilograms!=b.MassKilograms)
                    throw new ArgumentException("Exact body pose, raw velocity, mass and equal-duration drive source required.");
                for(int j=0;j<i;j++)if(b.Athlete.Equals(bodies[j].Athlete))throw new ArgumentException("Duplicate force actor.");
                seconds=Math.Min(seconds,f.ConstantAccelerationSeconds);
            }
            for(int i=0;i<count;i++)
            {
                int at=i;
                while(at>0&&string.CompareOrdinal(bodies[order[at-1]].Athlete.Value,bodies[i].Athlete.Value)>0){order[at]=order[at-1];at--;}
                order[at]=i;var initial=free[i].InitialMotion;var end=free[i].MotionAt(seconds/duration);
                accelerations[i]=new SkiContactAcceleration((end.Velocity.X-initial.Velocity.X)/seconds,(end.Velocity.Z-initial.Velocity.Z)/seconds,
                    (end.YawRate-initial.YawRate)/seconds);
            }
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
            {
                int ai=order[i],bi=order[j];var manifold=geometry.PreviewManifold(bodies[ai],bodies[bi]);
                if(manifold.Contact.Status==SkiContactPreviewStatus.Separated)continue;
                if(manifold.Contact.Status==SkiContactPreviewStatus.Penetrating)return Result(SkiContactForceStatus.Penetrating,seconds);
                if(!manifold.HasSupport)return Result(SkiContactForceStatus.UnresolvedGeometry,seconds);
                if(!Add(ai,bi,manifold.FirstX,manifold.FirstZ,manifold.Contact.NormalX,manifold.Contact.NormalZ,bodies,free))
                    return Result(SkiContactForceStatus.VelocityResponseRequired,seconds);
                if((manifold.FirstX!=manifold.SecondX||manifold.FirstZ!=manifold.SecondZ)&&
                    !Add(ai,bi,manifold.SecondX,manifold.SecondZ,manifold.Contact.NormalX,manifold.Contact.NormalZ,bodies,free))
                    return Result(SkiContactForceStatus.VelocityResponseRequired,seconds);
            }
            // Resolve to floating-point precision when it is stricter than the
            // configured speed cap. A tolerated residual must not repeatedly
            // manufacture encoded spin and change a resting face into a corner.
            // This is a numerical convergence reserve, not enlarged geometry or
            // post-solve clamping of velocity/yaw.
            double speedScale=1;
            for(int i=0;i<count;i++)
            {
                var m=free[i].InitialMotion;var a=accelerations[i];
                speedScale=Math.Max(speedScale,Math.Abs(m.Velocity.X)+Math.Abs(m.Velocity.Z)+Math.Abs(m.YawRate)+
                    seconds*(Math.Abs(a.X)+Math.Abs(a.Z)+Math.Abs(a.Yaw)));
            }
            double convergence=Math.Min(work.ClosingSpeedTolerance,64*2.2204460492503131e-16*speedScale);
            for(int pass=1;pass<=work.MaximumPasses;pass++)
            {
                for(int k=0;k<pointCount;k++)
                {
                    var p=points[k];Desired(p,bodies,free,out double normal,out double tangent);
                    Apply(p,normal-p.Normal,tangent-p.Tangent,bodies);p.Normal=normal;p.Tangent=tangent;points[k]=p;
                }
                double closing=0,residual=0;
                for(int k=0;k<pointCount;k++)
                {
                    var p=points[k];Relative(p,bodies,free,out double rn,out _);closing=Math.Max(closing,-rn);
                    Desired(p,bodies,free,out double n,out double t);double dn=n-p.Normal,dt=t-p.Tangent;
                    residual=Math.Max(residual,seconds*Math.Max(Math.Abs(p.Knn*dn+p.Knt*dt),Math.Abs(p.Knt*dn+p.Ktt*dt)));
                }
                if(closing*seconds>convergence||residual>convergence)
                {
                    if(pass==work.MaximumPasses)return Result(SkiContactForceStatus.WorkLimit,seconds,pass,closing,residual);
                    continue;
                }
                // A kinetic friction force cannot silently continue through zero
                // slip and start adding energy in the opposite direction. End the
                // common piece at the first frozen contact-point slip boundary.
                double admittedSeconds=seconds;
                for(int k=0;k<pointCount;k++)
                {
                    var p=points[k];if(p.Sliding==0)continue;
                    Relative(p,bodies,free,out _,out double tangentAcceleration);
                    if(p.InitialTangentSpeed*tangentAcceleration<0)
                        admittedSeconds=Math.Min(admittedSeconds,-p.InitialTangentSpeed/tangentAcceleration);
                }
                for(int i=0;i<count;i++)staged[i]=new SkiForceCoupledTrajectory(bodies[i].Pose,free[i].InitialMotion,accelerations[i],admittedSeconds);
                for(int i=0;i<count;i++)output[i]=staged[i];
                return Result(SkiContactForceStatus.Resolved,admittedSeconds,pass,closing,residual);
            }
            throw new InvalidOperationException("Validated force pass bound required.");
        }
        private SkiContactForcePreview Result(SkiContactForceStatus status,double seconds,int passes=0,double closing=0,double residual=0)
        {LastPreview=new SkiContactForcePreview(status,seconds,pointCount,passes,closing,residual);return LastPreview;}
        private bool Add(int ai,int bi,double x,double z,double nx,double nz,ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free)
        {
            var a=bodies[ai];var b=bodies[bi];var am=free[ai].InitialMotion;var bm=free[bi].InitialMotion;
            double arx=x-a.Pose.X,arz=z-a.Pose.Z,brx=x-b.Pose.X,brz=z-b.Pose.Z,tx=-nz,tz=nx;
            double rx=am.Velocity.X+am.YawRate*arz-bm.Velocity.X-bm.YawRate*brz;
            double rz=am.Velocity.Z-am.YawRate*arx-bm.Velocity.Z+bm.YawRate*brx;
            double vn=rx*nx+rz*nz,vt=rx*tx+rz*tz;
            if(vn< -work.ClosingSpeedTolerance)return false;
            // An opening contact carries no adhesive force. Its future recontact
            // remains the scheduler/sweep's responsibility.
            if(vn>work.ClosingSpeedTolerance)return true;
            double an=arz*nx-arx*nz,at=arz*tx-arx*tz,bn=brz*nx-brx*nz,bt=brz*tx-brx*tz;
            double ia=a.MassKilograms*inertiaPerMass,ib=b.MassKilograms*inertiaPerMass,im=1/a.MassKilograms+1/b.MassKilograms;
            points[pointCount++]=new Point{A=ai,B=bi,First=a.Athlete,Second=b.Athlete,X=x,Z=z,Nx=nx,Nz=nz,An=an,At=at,Bn=bn,Bt=bt,
                Knn=im+an*an/ia+bn*bn/ib,Ktt=im+at*at/ia+bt*bt/ib,Knt=an*at/ia+bn*bt/ib,
                Sliding=Math.Abs(vt)<=work.ClosingSpeedTolerance?0:Math.Sign(vt),InitialTangentSpeed=vt};
            return true;
        }
        private void Relative(Point p,ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,out double normal,out double tangent)
        {
            var a=bodies[p.A];var b=bodies[p.B];var aa=accelerations[p.A];var ba=accelerations[p.B];
            double aw=free[p.A].InitialMotion.YawRate,bw=free[p.B].InitialMotion.YawRate;
            double rx=aa.X+aa.Yaw*(p.Z-a.Pose.Z)-aw*aw*(p.X-a.Pose.X)-ba.X-ba.Yaw*(p.Z-b.Pose.Z)+bw*bw*(p.X-b.Pose.X);
            double rz=aa.Z-aa.Yaw*(p.X-a.Pose.X)-aw*aw*(p.Z-a.Pose.Z)-ba.Z+ba.Yaw*(p.X-b.Pose.X)+bw*bw*(p.Z-b.Pose.Z);
            normal=rx*p.Nx+rz*p.Nz;tangent=-rx*p.Nz+rz*p.Nx;
        }
        private void Desired(Point p,ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,out double n,out double t)
        {
            Relative(p,bodies,free,out double rn,out double rt);
            // Remove only this point's accumulated reaction; all other contact
            // forces remain. Re-solving total reactions prevents attractive force.
            rn-=p.Knn*p.Normal+p.Knt*p.Tangent;rt-=p.Knt*p.Normal+p.Ktt*p.Tangent;
            if(rn>=0){n=0;t=0;return;}
            double required=-rn;
            if(p.Sliding!=0)
            {
                double denominator=p.Knn-friction*p.Sliding*p.Knt;
                if(denominator<=0)throw new InvalidOperationException("Unresolved sliding contact effective inertia.");
                n=required/denominator;t=-friction*p.Sliding*n;return;
            }
            double lower=double.NegativeInfinity,upper=double.PositiveInfinity;
            Limit(p.Knt,required,ref lower,ref upper);Limit(p.Knn+friction*p.Knt,friction*required,ref lower,ref upper);
            Limit(-p.Knn+friction*p.Knt,friction*required,ref lower,ref upper);
            t=-(rt-p.Knt*rn/p.Knn)/(p.Ktt-p.Knt*p.Knt/p.Knn);t=Math.Max(lower,Math.Min(upper,t));
            n=Math.Max(0,(required-p.Knt*t)/p.Knn);
        }
        private static void Limit(double coefficient,double bound,ref double lower,ref double upper)
        {if(coefficient>0)upper=Math.Min(upper,bound/coefficient);else if(coefficient<0)lower=Math.Max(lower,bound/coefficient);}
        private void Apply(Point p,double normal,double tangent,ManualSkiContactBody[] bodies)
        {
            var a=accelerations[p.A];var b=accelerations[p.B];double ma=bodies[p.A].MassKilograms,mb=bodies[p.B].MassKilograms;
            double x=p.Nx*normal-p.Nz*tangent,z=p.Nz*normal+p.Nx*tangent;
            accelerations[p.A]=new SkiContactAcceleration(a.X+x/ma,a.Z+z/ma,a.Yaw+(p.An*normal+p.At*tangent)/(ma*inertiaPerMass));
            accelerations[p.B]=new SkiContactAcceleration(b.X-x/mb,b.Z-z/mb,b.Yaw-(p.Bn*normal+p.Bt*tangent)/(mb*inertiaPerMass));
        }
    }
}

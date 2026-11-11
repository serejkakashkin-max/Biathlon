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
        public int IntervalRefinements {get;}
        internal SkiContactForcePreview(SkiContactForceStatus status,double seconds,int points,int passes=0,double closing=0,double residual=0,int refinements=0)
        {IsValid=true;Status=status;DurationSeconds=seconds;SupportPoints=points;Passes=passes;MaximumClosingAcceleration=closing;MaximumReactionResidualSpeed=residual;IntervalRefinements=refinements;}
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
            internal double X,Z,Nx,Nz,An,At,Bn,Bt,Knn,Ktt,Knt,Normal,Tangent,Sliding,InitialTangentSpeed,AxisYaw,NormalBias;
        }
        private readonly ManualSkiContactSystem geometry;
        private readonly SkiContactGroupDefinition work;
        private readonly double friction,inertiaPerMass,halfLength,halfWidth,contactTolerance;
        private double pieceSeconds;
        private readonly int[] order;
        private readonly SkiContactAcceleration[] accelerations;
        private readonly SkiForceCoupledTrajectory[] staged;
        private readonly SkiForceCoupledTrajectory[] freePieces;
        private readonly Point[] points;
        private readonly Point[] opening;
        private int openingCount;
        private int pointCount;
        public string ContextJson {get;}
        public SkiContactForcePreview LastPreview {get;private set;}
        internal double MinimumPieceSeconds=>work.MinimumForcePieceSeconds;
        public ManualSkiContactForces(OccupancyDefinition body,SkiContactDefinition contact,SkiContactGroupDefinition work,int capacity)
        {
            if(body==null||contact==null||work==null)throw new ArgumentNullException();
            if(capacity<1)throw new ArgumentOutOfRangeException(nameof(capacity));
            geometry=new ManualSkiContactSystem(body,contact);this.work=work;friction=contact.SlidingFriction;contactTolerance=contact.ContactTolerance;
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            inertiaPerMass=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            order=new int[capacity];accelerations=new SkiContactAcceleration[capacity];staged=new SkiForceCoupledTrajectory[capacity];
            freePieces=new SkiForceCoupledTrajectory[capacity];
            points=new Point[checked(capacity*(capacity-1))];
            opening=new Point[points.Length];
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_frozen_contact_forces_v8_separate_impact_convergence",geometry.ContextJson,work.ToJson(),
                "two_support_endpoints;stable_id_pgs;unilateral_accumulated_reactions;static_or_sliding_coulomb_cone;no_force_restitution;raw_initial_motion;"+
                "shared_drive_constant_prefix_only;physical_centripetal_point_acceleration;rotating_face_sliding_coriolis;finite_interval_nonclosing_support;configured_closing_speed_convergence;opening_requires_displacement_above_existing_spatial_skin;"+
                "first_frozen_slip_zero_boundary;optional_existing_depth_bounded_half_interval_work_refinement;unchanged_residual_and_pass_bounds;actual_raw_source_and_original_external_acceleration;all_trajectories_validate_before_output;full_sweep_still_required"});
        }
        public SkiContactForceReaction GetReaction(int index)
        {
            if(!LastPreview.IsValid||index<0||index>=pointCount)throw new ArgumentOutOfRangeException(nameof(index));
            var p=points[index];return new SkiContactForceReaction(p.First,p.Second,p.X,p.Z,p.Normal,p.Tangent);
        }
        /// <summary>A difficult finite-interval reaction may need a shorter
        /// physical interval. Retain external acceleration and raw source;
        /// neither convergence tolerance nor the pass limit is increased.
        /// Refinement alone pays no time and is not a movement proof.</summary>
        internal SkiContactForcePreview PreviewRefined(ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,
            int count,SkiForceCoupledTrajectory[] output,int maximumRefinements,bool[] participants=null)
        {
            if(maximumRefinements<0)throw new ArgumentOutOfRangeException(nameof(maximumRefinements));
            var result=Preview(bodies,free,count,output,participants);int refinements=0;double seconds=free[0].DurationSeconds;
            while(result.Status==SkiContactForceStatus.WorkLimit&&refinements<maximumRefinements)
            {
                if(work.MinimumForcePieceSeconds>0&&seconds*.5<work.MinimumForcePieceSeconds)break;
                seconds*=.5;
                for(int i=0;i<count;i++)freePieces[i]=new SkiForceCoupledTrajectory(free[i].InitialPose,free[i].InitialMotion,free[i].Acceleration,seconds);
                result=Preview(bodies,freePieces,count,output,participants);refinements++;
            }
            return LastPreview=new SkiContactForcePreview(result.Status,result.DurationSeconds,result.SupportPoints,result.Passes,
                result.MaximumClosingAcceleration,result.MaximumReactionResidualSpeed,refinements);
        }
        public SkiContactForcePreview Preview(ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,int count,SkiForceCoupledTrajectory[] output)
            =>Preview(bodies,free,count,output,null);
        internal SkiContactForcePreview Preview(ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,int count,SkiForceCoupledTrajectory[] output,bool[] participants)
        {
            LastPreview=default;pointCount=openingCount=0;
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
            // Average the actual free-motion change across a declared short
            // integration interval when its numerical stop phase is smaller.
            // This still starts at the raw pose/velocity and pays real time.
            seconds=Math.Min(duration,Math.Max(seconds,work.MinimumForcePieceSeconds));
            for(int i=0;i<count;i++)
            {
                var initial=free[i].InitialMotion;var end=free[i].MotionAt(seconds/duration);
                var acceleration=new SkiContactAcceleration((end.Velocity.X-initial.Velocity.X)/seconds,
                    (end.Velocity.Z-initial.Velocity.Z)/seconds,(end.YawRate-initial.YawRate)/seconds);
                freePieces[i]=new SkiForceCoupledTrajectory(bodies[i].Pose,initial,acceleration,seconds);
            }
            return Preview(bodies,freePieces,count,output,participants);
        }
        /// <summary>Refresh reached contact reactions over one original frozen
        /// external-force piece, starting from the actual raw current motion.</summary>
        internal SkiContactForcePreview Preview(ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,int count,SkiForceCoupledTrajectory[] output)
            =>Preview(bodies,free,count,output,null);
        private SkiContactForcePreview Preview(ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,int count,SkiForceCoupledTrajectory[] output,bool[] participants)
        {
            LastPreview=default;pointCount=openingCount=0;
            if(bodies==null||free==null||output==null||count<1||count>order.Length||count>bodies.Length||count>free.Length||count>output.Length)
                throw new ArgumentException("Complete bodies, force pieces and output within cold capacity required.");
            if(participants!=null&&participants.Length!=count)throw new ArgumentException("Exact attested force presence roster required.");
            double seconds=free[0].DurationSeconds;
            for(int i=0;i<count;i++)
            {
                var b=bodies[i];var f=free[i];
                if(!b.IsValid||!f.IsValid||f.DurationSeconds!=seconds||f.InitialPose.X!=b.Pose.X||f.InitialPose.Z!=b.Pose.Z||
                    f.InitialPose.HeadingNanoradians!=b.Pose.HeadingNanoradians||
                    f.InitialMotion.Velocity.X!=b.Velocity.X||f.InitialMotion.Velocity.Z!=b.Velocity.Z||
                    f.InitialMotion.YawRate!=b.Pose.YawRateRadiansPerSecond)
                    throw new ArgumentException("Exact body and raw current force-piece source required.");
                for(int j=0;j<i;j++)if(b.Athlete.Equals(bodies[j].Athlete))throw new ArgumentException("Duplicate force actor.");
            }
            pieceSeconds=seconds;
            for(int i=0;i<count;i++)
            {
                int at=i;
                while(at>0&&string.CompareOrdinal(bodies[order[at-1]].Athlete.Value,bodies[i].Athlete.Value)>0){order[at]=order[at-1];at--;}
                order[at]=i;accelerations[i]=free[i].Acceleration;
            }
            for(int i=0;i<count;i++)for(int j=i+1;j<count;j++)
            {
                int ai=order[i],bi=order[j];if(participants!=null&&(!participants[ai]||!participants[bi]))continue;
                var manifold=geometry.PreviewManifold(bodies[ai],bodies[bi]);
                if(manifold.Contact.Status==SkiContactPreviewStatus.Separated)continue;
                if(manifold.Contact.Status==SkiContactPreviewStatus.Penetrating)return Result(SkiContactForceStatus.Penetrating,seconds);
                if(!manifold.HasSupport)return Result(SkiContactForceStatus.UnresolvedGeometry,seconds);
                if(!Add(ai,bi,manifold.FirstX,manifold.FirstZ,manifold.Contact.NormalX,manifold.Contact.NormalZ,bodies,free))
                    return Result(SkiContactForceStatus.VelocityResponseRequired,seconds);
                if((manifold.FirstX!=manifold.SecondX||manifold.FirstZ!=manifold.SecondZ)&&
                    !Add(ai,bi,manifold.SecondX,manifold.SecondZ,manifold.Contact.NormalX,manifold.Contact.NormalZ,bodies,free))
                    return Result(SkiContactForceStatus.VelocityResponseRequired,seconds);
            }
            // The immutable work policy bounds force residual velocity. Impact
            // resolution uses its separate stricter numerical convergence. All
            // force output still requires the independent continuous body proof.
            double convergence=work.ClosingSpeedTolerance;
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
                    var p=points[k];Relative(p,bodies,free,out double rn,out _);closing=Math.Max(closing,p.NormalBias-rn);
                    Desired(p,bodies,free,out double n,out double t);double dn=n-p.Normal,dt=t-p.Tangent;
                    residual=Math.Max(residual,seconds*Math.Max(Math.Abs(p.Knn*dn+p.Knt*dt),Math.Abs(p.Knt*dn+p.Ktt*dt)));
                }
                if(closing*seconds>convergence||residual>convergence)
                {
                    if(pass==work.MaximumPasses)return Result(SkiContactForceStatus.WorkLimit,seconds,pass,closing,residual);
                    continue;
                }
                if(RefreshIntervalBias(bodies,free))
                {
                    if(pass==work.MaximumPasses)return Result(SkiContactForceStatus.WorkLimit,seconds,pass,closing,residual);
                    continue;
                }
                // A kinetic friction force cannot silently continue through zero
                // slip and start adding energy in the opposite direction. End the
                // common piece at the first frozen contact-point slip boundary.
                double admittedSeconds=seconds;
                for(int k=0;k<openingCount;k++)
                {
                    var p=opening[k];
                    // An unresolved reversal inside the selected spatial skin
                    // is a continuing support, not another zero-time impact.
                    // The independent continuous body sweep remains mandatory.
                    if(work.SupportSkinFraction>0&&GapAt(p,bodies,free,admittedSeconds)>=-contactTolerance*work.SupportSkinFraction)continue;
                    if(GapDerivative(p,bodies,free,0)<=0||GapDerivative(p,bodies,free,admittedSeconds)>=0)continue;
                    double low=0,high=admittedSeconds;
                    for(int iteration=0;iteration<64;iteration++)
                    {
                        double mid=(low+high)*.5;if(mid==low||mid==high)break;
                        if(GapDerivative(p,bodies,free,mid)>0)low=mid;else high=mid;
                    }
                    admittedSeconds=high;
                }
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
        private bool Add(int ai,int bi,double x,double z,double nx,double nz,ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free)
        {
            var a=bodies[ai];var b=bodies[bi];var am=free[ai].InitialMotion;var bm=free[bi].InitialMotion;
            double arx=x-a.Pose.X,arz=z-a.Pose.Z,brx=x-b.Pose.X,brz=z-b.Pose.Z,tx=-nz,tz=nx;
            double rx=am.Velocity.X+am.YawRate*arz-bm.Velocity.X-bm.YawRate*brz;
            double rz=am.Velocity.Z-am.YawRate*arx-bm.Velocity.Z+bm.YawRate*brx;
            double vn=rx*nx+rz*nz,vt=rx*tx+rz*tz;
            // Continuing supports use the declared force residual. A stricter
            // impact convergence must not reject a bounded support residual
            // introduced by another paid force/contact piece. The independent
            // continuous body sweep still decides whether motion is admissible.
            if(vn< -work.ClosingSpeedTolerance)return false;
            // An opening contact carries no adhesive force. Its future recontact
            // remains the scheduler/sweep's responsibility.
            double an=arz*nx-arx*nz,at=arz*tx-arx*tz,bn=brz*nx-brx*nz,bt=brz*tx-brx*tz;
            double ia=a.Grounded||work.PreserveSteering?double.PositiveInfinity:a.MassKilograms*inertiaPerMass;
            double ib=b.Grounded||work.PreserveSteering?double.PositiveInfinity:b.MassKilograms*inertiaPerMass;
            double im=a.InverseMass+b.InverseMass;
            double af=Math.Sin(a.Pose.HeadingRadians)*nx+Math.Cos(a.Pose.HeadingRadians)*nz;
            double ar=Math.Cos(a.Pose.HeadingRadians)*nx-Math.Sin(a.Pose.HeadingRadians)*nz;
            bool axisFromA=Math.Max(Math.Abs(af),Math.Abs(ar))>=1-64*2.2204460492503131e-16;
            var point=new Point{A=ai,B=bi,First=a.Athlete,Second=b.Athlete,X=x,Z=z,Nx=nx,Nz=nz,An=an,At=at,Bn=bn,Bt=bt,
                Knn=im+an*an/ia+bn*bn/ib,Ktt=im+at*at/ia+bt*bt/ib,Knt=an*at/ia+bn*bt/ib,
                Sliding=Math.Abs(vt)<=work.ClosingSpeedTolerance?0:Math.Sign(vt),InitialTangentSpeed=vt,AxisYaw=axisFromA?am.YawRate:bm.YawRate};
            // A displacement smaller than the declared contact skin over this
            // force interval is still an owned support. Repeatedly treating its
            // encoded positive speed as a fresh separation creates a Zeno loop
            // at gap-velocity reversals. Resolvable opening remains force-free.
            if(vn>Math.Max(work.ClosingSpeedTolerance,contactTolerance/pieceSeconds))opening[openingCount++]=point;
            else points[pointCount++]=point;
            return true;
        }
        private void Relative(Point p,ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,out double normal,out double tangent)
        {
            var a=bodies[p.A];var b=bodies[p.B];var aa=accelerations[p.A];var ba=accelerations[p.B];
            double aw=free[p.A].InitialMotion.YawRate,bw=free[p.B].InitialMotion.YawRate;
            double rx=aa.X+aa.Yaw*(p.Z-a.Pose.Z)-aw*aw*(p.X-a.Pose.X)-ba.X-ba.Yaw*(p.Z-b.Pose.Z)+bw*bw*(p.X-b.Pose.X);
            double rz=aa.Z-aa.Yaw*(p.X-a.Pose.X)-aw*aw*(p.Z-a.Pose.Z)-ba.Z+ba.Yaw*(p.X-b.Pose.X)+bw*bw*(p.Z-b.Pose.Z);
            normal=rx*p.Nx+rz*p.Nz-2*p.AxisYaw*p.InitialTangentSpeed;tangent=-rx*p.Nz+rz*p.Nx;
        }
        private bool RefreshIntervalBias(ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free)
        {
            bool changed=false;
            // Instantaneous acceleration alone loses the cubic rotation term:
            // repeatedly refreshing a frozen force can consume the entire
            // contact tolerance. Also solve the finite interval's geometric
            // nonclosing condition. Reactions still act at the same physical
            // support point; no pose, clock, dimensions or velocity are clamped.
            for(int i=0;i<pointCount;i++)
            {
                var p=points[i];var a=bodies[p.A];var b=bodies[p.B];
                double initial=GapAt(p,bodies,free,0);
                double reserve=64*2.2204460492503131e-16*(1+Math.Abs(a.Pose.X-b.Pose.X)+Math.Abs(a.Pose.Z-b.Pose.Z)+halfLength+halfWidth);
                double correction=0;
                for(int sample=1;sample<=4;sample++)
                {
                    double t=pieceSeconds*sample/4;
                    double delta=GapAt(p,bodies,free,t)-initial;
                    double gap=initial+delta;
                    // An already admitted contact skin is not a positional
                    // spring. Oppose additional closing, rather than ejecting
                    // two touching skis within a vanishing force interval.
                    // Leave space for both positions and both rotating-body
                    // heading encodings used by the independent joint sweep.
                    double encodingReserve=Math.Max(8*PhysiologyNumbers.MinimumPositive*(1+halfLength+halfWidth),
                        pieceSeconds*work.ClosingSpeedTolerance);
                    double retainedSkin=Math.Min(contactTolerance-encodingReserve,
                        Math.Max(contactTolerance*work.SupportSkinFraction,-initial-work.SupportRecoverySpeed*pieceSeconds));
                    double excess=work.SupportSkinFraction>0?gap+retainedSkin:delta;
                    if(excess< -reserve)correction=Math.Max(correction,-2*excess/(t*t));
                }
                if(correction==0)continue;
                p.NormalBias+=correction;points[i]=p;changed=true;
            }
            return changed;
        }
        private double GapAt(Point p,ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,double seconds)
        {
            var a=bodies[p.A];var b=bodies[p.B];var am=free[p.A].InitialMotion;var bm=free[p.B].InitialMotion;
            var aa=accelerations[p.A];var ba=accelerations[p.B];double t=seconds;
            double dx=a.Pose.X-b.Pose.X+t*(am.Velocity.X-bm.Velocity.X+.5*t*(aa.X-ba.X));
            double dz=a.Pose.Z-b.Pose.Z+t*(am.Velocity.Z-bm.Velocity.Z+.5*t*(aa.Z-ba.Z));
            double ah=a.Pose.HeadingRadians+t*(am.YawRate+.5*t*aa.Yaw),bh=b.Pose.HeadingRadians+t*(bm.YawRate+.5*t*ba.Yaw);
            double ax=Math.Sin(ah),az=Math.Cos(ah),bx=Math.Sin(bh),bz=Math.Cos(bh);
            return Math.Max(Math.Max(Axis(ax,az),Axis(az,-ax)),Math.Max(Axis(bx,bz),Axis(bz,-bx)));
            double Axis(double nx,double nz)=>Math.Abs(dx*nx+dz*nz)-halfLength*(Math.Abs(ax*nx+az*nz)+Math.Abs(bx*nx+bz*nz))-
                halfWidth*(Math.Abs(az*nx-ax*nz)+Math.Abs(bz*nx-bx*nz));
        }
        private double GapDerivative(Point p,ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,double t)
        {
            var a=bodies[p.A];var b=bodies[p.B];var am=free[p.A].InitialMotion;var bm=free[p.B].InitialMotion;
            var aa=accelerations[p.A];var ba=accelerations[p.B];
            double dx=a.Pose.X-b.Pose.X+t*(am.Velocity.X-bm.Velocity.X+.5*t*(aa.X-ba.X));
            double dz=a.Pose.Z-b.Pose.Z+t*(am.Velocity.Z-bm.Velocity.Z+.5*t*(aa.Z-ba.Z));
            double vx=am.Velocity.X-bm.Velocity.X+t*(aa.X-ba.X),vz=am.Velocity.Z-bm.Velocity.Z+t*(aa.Z-ba.Z);
            double ah=a.Pose.HeadingRadians+t*(am.YawRate+.5*t*aa.Yaw),bh=b.Pose.HeadingRadians+t*(bm.YawRate+.5*t*ba.Yaw);
            double aw=am.YawRate+t*aa.Yaw,bw=bm.YawRate+t*ba.Yaw;
            double ax=Math.Sin(ah),az=Math.Cos(ah),bx=Math.Sin(bh),bz=Math.Cos(bh),best=double.NegativeInfinity,result=0;
            Axis(ax,az,aw);Axis(az,-ax,aw);Axis(bx,bz,bw);Axis(bz,-bx,bw);return result;
            void Axis(double nx,double nz,double w)
            {
                double af=ax*nx+az*nz,ar=az*nx-ax*nz,bf=bx*nx+bz*nz,br=bz*nx-bx*nz,d=dx*nx+dz*nz;
                double gap=Math.Abs(d)-halfLength*(Math.Abs(af)+Math.Abs(bf))-halfWidth*(Math.Abs(ar)+Math.Abs(br));
                if(gap<=best)return;best=gap;
                result=Math.Sign(d)*(vx*nx+vz*nz+w*(dx*nz-dz*nx))-
                    halfLength*(Math.Sign(af)*(aw-w)*ar+Math.Sign(bf)*(bw-w)*br)+
                    halfWidth*(Math.Sign(ar)*(aw-w)*af+Math.Sign(br)*(bw-w)*bf);
            }
        }
        private void Desired(Point p,ManualSkiContactBody[] bodies,SkiForceCoupledTrajectory[] free,out double n,out double t)
        {
            Relative(p,bodies,free,out double rn,out double rt);
            rn-=p.NormalBias;
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
            if(!bodies[p.A].Grounded)accelerations[p.A]=new SkiContactAcceleration(a.X+x/ma,a.Z+z/ma,
                work.PreserveSteering ? a.Yaw : a.Yaw+(p.An*normal+p.At*tangent)/(ma*inertiaPerMass));
            if(!bodies[p.B].Grounded)accelerations[p.B]=new SkiContactAcceleration(b.X-x/mb,b.Z-z/mb,
                work.PreserveSteering ? b.Yaw : b.Yaw-(p.Bn*normal+p.Bt*tangent)/(mb*inertiaPerMass));
        }
    }
}

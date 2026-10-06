using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Planar centre velocity may include contact slip. It cannot be encoded
    /// back into speed along heading without separately resolving that slip.</summary>
    public readonly struct SkiPlanarVelocity
    {
        public double X {get;}
        public double Z {get;}
        public SkiPlanarVelocity(double x,double z)
        {
            X=PhysiologyNumbers.RequireRange(x,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(x));
            Z=PhysiologyNumbers.RequireRange(z,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(z));
        }
    }
    public readonly struct ManualSkiContactBody
    {
        public AthleteId Athlete {get;}
        public ManualSkiHeadingState Pose {get;}
        public SkiPlanarVelocity Velocity {get;}
        public double MassKilograms {get;}
        public bool IsValid=>Athlete.IsValid&&Pose.IsValid&&MassKilograms>0;
        public ManualSkiContactBody(AthleteId athlete,ManualSkiHeadingState pose,SkiPlanarVelocity velocity,double massKilograms)
        {
            if(!athlete.IsValid||!pose.IsValid)throw new ArgumentException("Initialized contact body required.");
            MassKilograms=PhysiologyNumbers.RequireRange(massKilograms,.001,PhysiologyNumbers.MaximumMagnitude,nameof(massKilograms));
            Athlete=athlete;Pose=pose;Velocity=velocity;
        }
    }
    public enum SkiContactPreviewStatus { Separated, SeparatingOrResting, Impulse, Penetrating, RequiresAngularResolution }
    public readonly struct SkiLinearContactPreview
    {
        public bool IsValid {get;}
        public bool HasContact {get;}
        public double ContactSeconds {get;}
        public SkiContactPreview Contact {get;}
        internal SkiLinearContactPreview(bool hit,double seconds,SkiContactPreview contact)
        {IsValid=true;HasContact=hit;ContactSeconds=seconds;Contact=contact;}
    }
    public readonly struct SkiContactPreview
    {
        public bool IsValid {get;}
        public SkiContactPreviewStatus Status {get;}
        /// <summary>Separating-axis gap. Negative values mean overlap, not Euclidean distance.</summary>
        public double GapMeters {get;}
        /// <summary>Normal points from the second argument toward the first.</summary>
        public double NormalX {get;}
        public double NormalZ {get;}
        public SkiPlanarVelocity FirstVelocity {get;}
        public SkiPlanarVelocity SecondVelocity {get;}
        public double NormalImpulseNewtonSeconds {get;}
        public double TangentImpulseNewtonSeconds {get;}
        internal SkiContactPreview(SkiContactPreviewStatus status,double gap,double nx,double nz,
            SkiPlanarVelocity first,SkiPlanarVelocity second,double normal=0,double tangent=0)
        {IsValid=true;Status=status;GapMeters=gap;NormalX=nx;NormalZ=nz;FirstVelocity=first;SecondVelocity=second;
            NormalImpulseNewtonSeconds=normal;TangentImpulseNewtonSeconds=tangent;}
    }
    /// <summary>Instantaneous pair response at an already reached contact, not a
    /// whole-step collision detector. No position correction, teleport, route or event commit.</summary>
    public sealed class ManualSkiContactSystem
    {
        private readonly double halfLength,halfWidth;
        private readonly SkiContactDefinition definition;
        public string ContextJson {get;}
        public ManualSkiContactSystem(OccupancyDefinition body,SkiContactDefinition definition)
        {
            if(body==null||definition==null)throw new ArgumentNullException();
            this.definition=definition;
            // Preserve the current complete occupied envelope, including clearance.
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_oriented_pair_contact_v1",body.ToJson(),definition.ToJson()});
        }
        /// <summary>Exact separating-axis contact time for constant velocities and
        /// fixed orientations. It rejects rotating bodies instead of pretending their
        /// endpoint boxes cover the sweep. No course-edge or other-body admission.</summary>
        public SkiLinearContactPreview PreviewLinearContact(ManualSkiContactBody first,ManualSkiContactBody second,double seconds)
        {
            var initial=Preview(first,second);
            PhysiologyNumbers.RequireRange(seconds,0,PhysiologyNumbers.MaximumMagnitude,nameof(seconds));
            if(first.Pose.YawRateRadiansPerSecond!=0||second.Pose.YawRateRadiansPerSecond!=0)
                throw new ArgumentException("Linear contact requires fixed body orientations.");
            if(initial.Status!=SkiContactPreviewStatus.Separated)return new SkiLinearContactPreview(true,0,initial);
            double ax=Math.Sin(first.Pose.HeadingRadians),az=Math.Cos(first.Pose.HeadingRadians);
            double bx=Math.Sin(second.Pose.HeadingRadians),bz=Math.Cos(second.Pose.HeadingRadians);
            double dx=first.Pose.X-second.Pose.X,dz=first.Pose.Z-second.Pose.Z;
            double vx=first.Velocity.X-second.Velocity.X,vz=first.Velocity.Z-second.Velocity.Z;
            double enter=0,exit=seconds;
            bool hit=Axis(ax,az)&&Axis(az,-ax)&&Axis(bx,bz)&&Axis(bz,-bx);
            if(!hit)return new SkiLinearContactPreview(false,seconds,default);
            // Pose encoding can move an endpoint by a nanometre. The numerical
            // contact tolerance covers it; no penetration correction is performed.
            var a=At(first,enter);var b=At(second,enter);
            var contact=Preview(a,b);
            return new SkiLinearContactPreview(true,enter,contact);
            bool Axis(double x,double z)
            {
                double radius=halfLength*(Math.Abs(ax*x+az*z)+Math.Abs(bx*x+bz*z))
                    +halfWidth*(Math.Abs(az*x-ax*z)+Math.Abs(bz*x-bx*z));
                double offset=dx*x+dz*z,speed=vx*x+vz*z;
                if(speed==0)return Math.Abs(offset)<=radius;
                double a=(-radius-offset)/speed,b=(radius-offset)/speed;
                if(a>b){double swap=a;a=b;b=swap;}
                enter=Math.Max(enter,a);exit=Math.Min(exit,b);
                return enter<=exit;
            }
            ManualSkiContactBody At(ManualSkiContactBody body,double time)=>new ManualSkiContactBody(body.Athlete,
                new ManualSkiHeadingState(body.Pose.X+body.Velocity.X*time,body.Pose.Z+body.Velocity.Z*time,body.Pose.HeadingRadians),
                body.Velocity,body.MassKilograms);
        }
        public SkiContactPreview Preview(ManualSkiContactBody first,ManualSkiContactBody second)
        {
            if(!first.IsValid||!second.IsValid||first.Athlete.Equals(second.Athlete))throw new ArgumentException("Two distinct initialized bodies required.");
            // Stable actor order makes degenerate axes and ties independent of caller order.
            bool swap=string.CompareOrdinal(first.Athlete.Value,second.Athlete.Value)>0;
            var a=swap?second:first;var b=swap?first:second;
            double ax=Math.Sin(a.Pose.HeadingRadians),az=Math.Cos(a.Pose.HeadingRadians);
            double bx=Math.Sin(b.Pose.HeadingRadians),bz=Math.Cos(b.Pose.HeadingRadians);
            double dx=a.Pose.X-b.Pose.X,dz=a.Pose.Z-b.Pose.Z;
            double gap=double.NegativeInfinity,nx=0,nz=0;
            Axis(ax,az);Axis(az,-ax);Axis(bx,bz);Axis(bz,-bx);
            void Axis(double x,double z)
            {
                double offset=dx*x+dz*z;
                double ra=halfLength*Math.Abs(ax*x+az*z)+halfWidth*Math.Abs(az*x-ax*z);
                double rb=halfLength*Math.Abs(bx*x+bz*z)+halfWidth*Math.Abs(bz*x-bx*z);
                double candidate=Math.Abs(offset)-ra-rb;
                if(candidate<=gap)return;
                double sign=offset<0?-1:1;gap=candidate;nx=x*sign;nz=z*sign;
            }
            var va=a.Velocity;var vb=b.Velocity;double impulse=0,tangent=0;
            SkiContactPreviewStatus status;
            if(gap>definition.ContactTolerance)status=SkiContactPreviewStatus.Separated;
            else if(gap< -definition.ContactTolerance)status=SkiContactPreviewStatus.Penetrating;
            else if(a.Pose.YawRateRadiansPerSecond!=0||b.Pose.YawRateRadiansPerSecond!=0)
                // The centre-only impulse below is not a rigid-body angular impulse.
                // Rotating skis can close at their corners with zero centre speed.
                // Keep that unresolved case explicit rather than granting a false rest.
                status=SkiContactPreviewStatus.RequiresAngularResolution;
            else
            {
                double rx=va.X-vb.X,rz=va.Z-vb.Z,closing=rx*nx+rz*nz;
                if(closing>=0)status=SkiContactPreviewStatus.SeparatingOrResting;
                else
                {
                    status=SkiContactPreviewStatus.Impulse;
                    double inverseMass=1/a.MassKilograms+1/b.MassKilograms;
                    impulse=-(1+definition.Restitution)*closing/inverseMass;
                    double tx=-nz,tz=nx,sliding=rx*tx+rz*tz;
                    double bound=definition.SlidingFriction*impulse;
                    tangent=Math.Max(-bound,Math.Min(bound,-sliding/inverseMass));
                    double ix=nx*impulse+tx*tangent,iz=nz*impulse+tz*tangent;
                    va=new SkiPlanarVelocity(va.X+ix/a.MassKilograms,va.Z+iz/a.MassKilograms);
                    vb=new SkiPlanarVelocity(vb.X-ix/b.MassKilograms,vb.Z-iz/b.MassKilograms);
                }
            }
            return swap?new SkiContactPreview(status,gap,-nx,-nz,vb,va,impulse,tangent):
                new SkiContactPreview(status,gap,nx,nz,va,vb,impulse,tangent);
        }
    }
}

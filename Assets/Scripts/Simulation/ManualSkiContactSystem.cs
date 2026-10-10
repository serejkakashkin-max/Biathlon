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
        public bool Grounded {get;}
        internal double InverseMass=>Grounded?0:1/MassKilograms;
        public bool IsValid=>Athlete.IsValid&&Pose.IsValid&&MassKilograms>0;
        public ManualSkiContactBody(AthleteId athlete,ManualSkiHeadingState pose,SkiPlanarVelocity velocity,double massKilograms,bool grounded=false)
        {
            if(!athlete.IsValid||!pose.IsValid)throw new ArgumentException("Initialized contact body required.");
            MassKilograms=PhysiologyNumbers.RequireRange(massKilograms,.001,PhysiologyNumbers.MaximumMagnitude,nameof(massKilograms));
            Athlete=athlete;Pose=pose;Velocity=velocity;Grounded=grounded;
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
    public readonly struct SkiAngularContactPreview
    {
        public bool IsValid {get;}
        public SkiContactPreview Contact {get;}
        public double FirstYawRate {get;}
        public double SecondYawRate {get;}
        public double ContactX {get;}
        public double ContactZ {get;}
        internal SkiAngularContactPreview(SkiContactPreview contact,double firstYaw,double secondYaw,double x,double z)
        {IsValid=true;Contact=contact;FirstYawRate=firstYaw;SecondYawRate=secondYaw;ContactX=x;ContactZ=z;}
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
    /// <summary>Frozen support interval of an actual reached rectangle contact.
    /// Both endpoints matter: a face contact cannot be held by one centre point
    /// when another force would rotate its corner into the other body.</summary>
    public readonly struct SkiContactManifoldPreview
    {
        public SkiContactPreview Contact {get;}
        public bool HasSupport {get;}
        public double FirstX {get;} public double FirstZ {get;}
        public double SecondX {get;} public double SecondZ {get;}
        public double MiddleX {get;} public double MiddleZ {get;}
        internal SkiContactManifoldPreview(SkiContactPreview contact,bool support=false,double ax=0,double az=0,double bx=0,double bz=0,double mx=0,double mz=0)
        {Contact=contact;HasSupport=support;FirstX=ax;FirstZ=az;SecondX=bx;SecondZ=bz;MiddleX=mx;MiddleZ=mz;}
    }
    /// <summary>Instantaneous pair response at an already reached contact, not a
    /// whole-step collision detector. No position correction, teleport, route or event commit.</summary>
    public sealed class ManualSkiContactSystem
    {
        private readonly double halfLength,halfWidth;
        private readonly SkiContactDefinition definition;
        public string ContextJson {get;}
        public string AngularContextJson {get;}
        public ManualSkiContactSystem(OccupancyDefinition body,SkiContactDefinition definition)
        {
            if(body==null||definition==null)throw new ArgumentNullException();
            this.definition=definition;
            // Preserve the current complete occupied envelope, including clearance.
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_oriented_pair_contact_v1",body.ToJson(),definition.ToJson()});
            AngularContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_oriented_angular_contact_v1",body.ToJson(),definition.ToJson()});
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
                body.Velocity,body.MassKilograms,body.Grounded);
        }
        /// <summary>One contact-point impulse including angular velocity and rectangular
        /// planar inertia. Requires the caller to have reached contact continuously.
        /// Does not sweep a rotating trajectory or advance bodies after the impulse.</summary>
        public SkiAngularContactPreview PreviewAngularContact(ManualSkiContactBody first,ManualSkiContactBody second)
            => PreviewAngularContact(first,first.Pose.YawRateRadiansPerSecond,second,second.Pose.YawRateRadiansPerSecond);

        /// <summary>Retains unquantized physical spin between simultaneous contact
        /// impulses. Encoding belongs to the future joint motion commit, not each pair.</summary>
        public SkiAngularContactPreview PreviewAngularContact(ManualSkiContactBody first,double firstYaw,
            ManualSkiContactBody second,double secondYaw)
        {
            PhysiologyNumbers.RequireRange(firstYaw,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(firstYaw));
            PhysiologyNumbers.RequireRange(secondYaw,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(secondYaw));
            // Canonical order also preserves exact swap symmetry of the angular solve.
            if(!first.IsValid||!second.IsValid||first.Athlete.Equals(second.Athlete))throw new ArgumentException("Two distinct initialized bodies required.");
            if(string.CompareOrdinal(first.Athlete.Value,second.Athlete.Value)>0)
            {
                var reverse=Angular(second,first,secondYaw,firstYaw);var c=reverse.Contact;
                return new SkiAngularContactPreview(new SkiContactPreview(c.Status,c.GapMeters,-c.NormalX,-c.NormalZ,
                    c.SecondVelocity,c.FirstVelocity,c.NormalImpulseNewtonSeconds,c.TangentImpulseNewtonSeconds),
                    reverse.SecondYawRate,reverse.FirstYawRate,reverse.ContactX,reverse.ContactZ);
            }
            return Angular(first,second,firstYaw,secondYaw);
        }
        private SkiAngularContactPreview Angular(ManualSkiContactBody a,ManualSkiContactBody b,double wa,double wb)
        {
            var manifold=PreviewManifold(a,b);var initial=manifold.Contact;
            if(initial.Status==SkiContactPreviewStatus.Separated||initial.Status==SkiContactPreviewStatus.Penetrating)
                return new SkiAngularContactPreview(initial,wa,wb,0,0);
            double nx=initial.NormalX,nz=initial.NormalZ,tx=-nz,tz=nx;
            if(!manifold.HasSupport)
                return new SkiAngularContactPreview(new SkiContactPreview(SkiContactPreviewStatus.RequiresAngularResolution,
                    initial.GapMeters,nx,nz,a.Velocity,b.Velocity),wa,wb,0,0);
            return AngularAt(a,b,wa,wb,initial,manifold.MiddleX,manifold.MiddleZ);
        }
        internal SkiAngularContactPreview PreviewSupportContact(ManualSkiContactBody a,double wa,ManualSkiContactBody b,double wb,double x,double z)
        {
            var contact=Preview(a,b);
            if(contact.Status==SkiContactPreviewStatus.Separated||contact.Status==SkiContactPreviewStatus.Penetrating)
                return new SkiAngularContactPreview(contact,wa,wb,x,z);
            return AngularAt(a,b,wa,wb,contact,x,z);
        }
        private SkiAngularContactPreview AngularAt(ManualSkiContactBody a,ManualSkiContactBody b,double wa,double wb,SkiContactPreview initial,double px,double pz)
        {
            double nx=initial.NormalX,nz=initial.NormalZ,tx=-nz,tz=nx;
            double arx=px-a.Pose.X,arz=pz-a.Pose.Z,brx=px-b.Pose.X,brz=pz-b.Pose.Z;
            double an=arz*nx-arx*nz,at=arz*tx-arx*tz,bn=brz*nx-brx*nz,bt=brz*tx-brx*tz;
            double ia=a.Grounded?double.PositiveInfinity:a.MassKilograms*(halfLength*halfLength+halfWidth*halfWidth)/3;
            double ib=b.Grounded?double.PositiveInfinity:b.MassKilograms*(halfLength*halfLength+halfWidth*halfWidth)/3;
            double rx=a.Velocity.X+wa*arz-b.Velocity.X-wb*brz;
            double rz=a.Velocity.Z-wa*arx-b.Velocity.Z+wb*brx;
            double vn=rx*nx+rz*nz,vt=rx*tx+rz*tz;
            if(vn>=0)return new SkiAngularContactPreview(new SkiContactPreview(SkiContactPreviewStatus.SeparatingOrResting,
                initial.GapMeters,nx,nz,a.Velocity,b.Velocity),wa,wb,px,pz);
            double inverseMass=a.InverseMass+b.InverseMass;
            double knn=inverseMass+an*an/ia+bn*bn/ib,ktt=inverseMass+at*at/ia+bt*bt/ib,knt=an*at/ia+bn*bt/ib;
            double required=-(1+definition.Restitution)*vn;
            // Minimize kinetic energy subject to the normal restitution constraint
            // and Coulomb impulse cone. Coupled normal/tangent inertia must not be
            // solved independently: that can reintroduce normal closing velocity.
            double min=double.NegativeInfinity,max=double.PositiveInfinity,mu=definition.SlidingFriction;
            Limit(knt,required);Limit(knn+mu*knt,mu*required);Limit(-knn+mu*knt,mu*required);
            double jt=-(vt-knt*vn/knn)/(ktt-knt*knt/knn);
            jt=Math.Max(min,Math.Min(max,jt));double jn=Math.Max(0,(required-knt*jt)/knn);
            double ix=nx*jn+tx*jt,iz=nz*jn+tz*jt;
            var va=new SkiPlanarVelocity(a.Velocity.X+ix*a.InverseMass,a.Velocity.Z+iz*a.InverseMass);
            var vb=new SkiPlanarVelocity(b.Velocity.X-ix*b.InverseMass,b.Velocity.Z-iz*b.InverseMass);
            double nextWa=wa+(an*jn+at*jt)/ia,nextWb=wb-(bn*jn+bt*jt)/ib;
            PhysiologyNumbers.RequireRange(nextWa,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,"contact yaw");
            PhysiologyNumbers.RequireRange(nextWb,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,"contact yaw");
            return new SkiAngularContactPreview(new SkiContactPreview(SkiContactPreviewStatus.Impulse,initial.GapMeters,nx,nz,va,vb,jn,jt),nextWa,nextWb,px,pz);

            void Limit(double coefficient,double bound)
            {if(coefficient>0)max=Math.Min(max,bound/coefficient);else if(coefficient<0)min=Math.Max(min,bound/coefficient);}
        }
        public SkiContactManifoldPreview PreviewManifold(ManualSkiContactBody a,ManualSkiContactBody b)
        {
            // Preserve canonical point order, independent of caller enumeration.
            if(!a.IsValid||!b.IsValid||a.Athlete.Equals(b.Athlete))throw new ArgumentException("Distinct initialized bodies required.");
            if(string.CompareOrdinal(a.Athlete.Value,b.Athlete.Value)>0)
            {
                var reverse=PreviewManifold(b,a);var c=reverse.Contact;
                return new SkiContactManifoldPreview(new SkiContactPreview(c.Status,c.GapMeters,-c.NormalX,-c.NormalZ,
                    c.SecondVelocity,c.FirstVelocity,c.NormalImpulseNewtonSeconds,c.TangentImpulseNewtonSeconds),
                    reverse.HasSupport,reverse.FirstX,reverse.FirstZ,reverse.SecondX,reverse.SecondZ,reverse.MiddleX,reverse.MiddleZ);
            }
            var contact=Preview(a,b);
            if(contact.Status==SkiContactPreviewStatus.Separated||contact.Status==SkiContactPreviewStatus.Penetrating)
                return new SkiContactManifoldPreview(contact);
            double nx=contact.NormalX,nz=contact.NormalZ,tx=-nz,tz=nx;
            Feature(a,-1,out double alow,out double ahigh,out double adepth);
            Feature(b,1,out double blow,out double bhigh,out double bdepth);
            double low=Math.Max(alow,blow),high=Math.Min(ahigh,bhigh);
            if(low>high+definition.ContactTolerance)return new SkiContactManifoldPreview(contact);
            double depth=(adepth+bdepth)*.5,tangent=(low+high)*.5;
            return new SkiContactManifoldPreview(contact,true,nx*depth+tx*low,nz*depth+tz*low,
                nx*depth+tx*high,nz*depth+tz*high,nx*depth+tx*tangent,nz*depth+tz*tangent);
            void Feature(ManualSkiContactBody body,int sign,out double lower,out double upper,out double extreme)
            {
                double fx=Math.Sin(body.Pose.HeadingRadians),fz=Math.Cos(body.Pose.HeadingRadians);
                double extent=halfLength*Math.Abs(fx*nx+fz*nz)+halfWidth*Math.Abs(fz*nx-fx*nz);
                extreme=body.Pose.X*nx+body.Pose.Z*nz+sign*extent;
                lower=double.PositiveInfinity;upper=double.NegativeInfinity;
                // Four corners, with coordinate-roundoff reserve in addition to the
                // declared contact tolerance. Face support keeps both edge endpoints.
                double reserve=definition.ContactTolerance+32*2.2204460492503131e-16*(Math.Abs(body.Pose.X)+Math.Abs(body.Pose.Z)+halfLength+halfWidth+1);
                for(int longitudinal=-1;longitudinal<=1;longitudinal+=2)for(int lateral=-1;lateral<=1;lateral+=2)
                {
                    double x=body.Pose.X+fx*halfLength*longitudinal+fz*halfWidth*lateral;
                    double z=body.Pose.Z+fz*halfLength*longitudinal-fx*halfWidth*lateral;
                    if(Math.Abs(x*nx+z*nz-extreme)>reserve)continue;
                    double projected=x*tx+z*tz;lower=Math.Min(lower,projected);upper=Math.Max(upper,projected);
                }
            }
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
                    double inverseMass=a.InverseMass+b.InverseMass;
                    impulse=-(1+definition.Restitution)*closing/inverseMass;
                    double tx=-nz,tz=nx,sliding=rx*tx+rz*tz;
                    double bound=definition.SlidingFriction*impulse;
                    tangent=Math.Max(-bound,Math.Min(bound,-sliding/inverseMass));
                    double ix=nx*impulse+tx*tangent,iz=nz*impulse+tz*tangent;
                    va=new SkiPlanarVelocity(va.X+ix*a.InverseMass,va.Z+iz*a.InverseMass);
                    vb=new SkiPlanarVelocity(vb.X-ix*b.InverseMass,vb.Z-iz*b.InverseMass);
                }
            }
            return swap?new SkiContactPreview(status,gap,-nx,-nz,vb,va,impulse,tangent):
                new SkiContactPreview(status,gap,nx,nz,va,vb,impulse,tangent);
        }
    }
}

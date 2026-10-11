using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum SkiContactSweepStatus { Clear, InitialProximity, PotentialContact, WorkLimit, UnreachedArrival }

    public enum SkiFirstContactStatus { Clear, InitialProximity, Reached, WorkLimit }
    /// <summary>Actual reached proximity within the existing spatial tolerance,
    /// with a continuously admitted prefix. Not an exact zero-gap time, impulse,
    /// course proof or permission to ignore other actors.</summary>
    public readonly struct SkiFirstContactPreview
    {
        public bool IsValid {get;}
        public SkiFirstContactStatus Status {get;}
        public double ContactSeconds {get;}
        public int IntervalChecks {get;}
        public double GapMeters {get;}
        public SkiContactSweepPreview Admission {get;}
        internal SkiFirstContactPreview(SkiFirstContactStatus status,double seconds,int checks,double gap,SkiContactSweepPreview admission=default)
        {IsValid=true;Status=status;ContactSeconds=seconds;IntervalChecks=checks;GapMeters=gap;Admission=admission;}
    }

    /// <summary>A clear prefix and the first interval not proved clear. PotentialContact
    /// is deliberately not an exact hit time or permission to apply an impulse.</summary>
    public readonly struct SkiContactSweepPreview
    {
        public bool IsValid {get;}
        public SkiContactSweepStatus Status {get;}
        public double ClearUntilSeconds {get;}
        public double IntervalEndSeconds {get;}
        public int IntervalChecks {get;}
        public double GeometryReserveMeters {get;}
        public bool IsContactContinuation {get;}
        public bool IsContactArrival {get;}
        internal SkiContactSweepPreview(SkiContactSweepStatus status,double from,double to,int checks,double reserve,bool continuation=false,bool arrival=false)
        {IsValid=true;Status=status;ClearUntilSeconds=from;IntervalEndSeconds=to;IntervalChecks=checks;GeometryReserveMeters=reserve;IsContactContinuation=continuation;IsContactArrival=arrival;}
    }

    /// <summary>Continuous conservative pair exclusion for force-weighted manual turns.
    /// Uses actual oriented boxes. No actor position, race state or event is committed.</summary>
    public sealed class ManualSkiContactSweep
    {
        private readonly ManualSkiContactSystem contact;
        private readonly SkiContactSweepDefinition definition;
        private readonly double radius,tolerance,halfLength,halfWidth;
        public string ContextJson {get;}
        public string ContinuationContextJson {get;}
        public string SpatialContextJson {get;}
        public string ArrivalContextJson {get;}
        public string FirstContactContextJson {get;}
        public ManualSkiContactSweep(OccupancyDefinition body,SkiContactDefinition contactDefinition,SkiContactSweepDefinition definition)
        {
            if(body==null||contactDefinition==null||definition==null)throw new ArgumentNullException();
            contact=new ManualSkiContactSystem(body,contactDefinition);this.definition=definition;
            radius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation);
            tolerance=contactDefinition.ContactTolerance;
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_force_turn_sweep_v1",contact.ContextJson,definition.ToJson()});
            ContinuationContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_existing_contact_sweep_v1",ContextJson});
            ArrivalContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_reached_terminal_contact_sweep_v1",ContextJson,
                "endpoint_actual_proximity;continuous_existing_numerical_depth_only;response_before_next_motion"});
            SpatialContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_mixed_world_trajectory_sweep_v3_rotating_axis",ContinuationContextJson,ArrivalContextJson});
            FirstContactContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_first_reached_proximity_v2_endpoint_skin",SpatialContextJson,
                "fixed_current_axis_conservative_advancement;existing_tolerance_half_band_target;actual_endpoint_within_configured_skin;full_prefix_admission;shared_configured_work_bound;no_unproved_clear"});
        }

        public SkiContactSweepPreview Preview(AthleteId first,ManualSkiHeadingFrame a,AthleteId second,ManualSkiHeadingFrame b)
            => PreviewCore(first,new ManualSkiWorldTrajectory(a),second,new ManualSkiWorldTrajectory(b),false);

        public SkiContactSweepPreview Preview(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b)
            => PreviewCore(first,a,second,b,false);

        // The scheduler owns endpoint proximity registration. Its unregistered
        // pairs may enter the positive response skin while remaining physically
        // separated throughout the interval. This never admits zero/negative
        // gap or exempts an already registered contact from its own proof.
        internal SkiContactSweepPreview PreviewSeparatedResponseBand(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b)
        {
            if(!first.IsValid||!second.IsValid||first.Equals(second)||!a.IsValid||!b.IsValid||a.DurationSeconds!=b.DurationSeconds)
                throw new ArgumentException("Distinct actors and equal-duration trajectories required.");
            double reserve=Reserve(a,b);
            if(FarApart(a,b,reserve))return new SkiContactSweepPreview(SkiContactSweepStatus.Clear,a.DurationSeconds,a.DurationSeconds,0,reserve);
            var start=Geometry(first,a,second,b,0);
            if(start.GapMeters<=tolerance)return new SkiContactSweepPreview(SkiContactSweepStatus.InitialProximity,0,0,0,reserve);
            int checks=0;return Search(first,a,second,b,0,a.DurationSeconds,0,reserve,0,false,false,ref checks);
        }

        /// <summary>For an explicitly registered, already reached contact pair only.
        /// Allows numerical contact depth while proving the whole continuation does
        /// not exceed it. Does not exempt other pairs, edges or force admission.</summary>
        public SkiContactSweepPreview PreviewExistingContact(AthleteId first,ManualSkiHeadingFrame a,AthleteId second,ManualSkiHeadingFrame b)
            => PreviewCore(first,new ManualSkiWorldTrajectory(a),second,new ManualSkiWorldTrajectory(b),true);

        public SkiContactSweepPreview PreviewExistingContact(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b)
            => PreviewCore(first,a,second,b,true);

        /// <summary>For an explicitly identified contact reached at the end of
        /// this interval. Actual endpoint proximity and continuous exclusion of
        /// penetration beyond the existing numerical tolerance are both required.
        /// A possible-contact bracket alone cannot authorize this mode.</summary>
        public SkiContactSweepPreview PreviewArrivalContact(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b)
            =>PreviewCore(first,a,second,b,false,true);

        /// <summary>Advance toward the first reached body proximity using a fixed
        /// separating axis for each conservative interval. The bound accounts for
        /// translation, acceleration, rotation and existing encoding reserves.
        /// A potential-contact bracket never becomes a guessed impulse time.
        /// Initial contacts need their own response/continuation policy.</summary>
        public SkiFirstContactPreview PreviewFirstContact(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b)
        {
            if(!first.IsValid||!second.IsValid||first.Equals(second)||!a.IsValid||!b.IsValid||a.DurationSeconds!=b.DurationSeconds)
                throw new ArgumentException("Distinct actors and equal-duration world trajectories required.");
            if(string.CompareOrdinal(first.Value,second.Value)>0)return PreviewFirstContact(second,b,first,a);
            double reserve=Reserve(a,b),seconds=0,target=tolerance*.5;
            if(FarApart(a,b,reserve))return new SkiFirstContactPreview(SkiFirstContactStatus.Clear,a.DurationSeconds,0,double.PositiveInfinity);
            var geometry=Geometry(first,a,second,b,0);
            if(geometry.GapMeters<=tolerance)
                return new SkiFirstContactPreview(SkiFirstContactStatus.InitialProximity,0,0,geometry.GapMeters);
            int checks=0;
            while(checks<definition.MaximumIntervalChecks)
            {
                checks++;
                if(geometry.GapMeters<=tolerance&&geometry.GapMeters> -tolerance+reserve&&seconds>0)
                {
                    var proof=Search(first,a,second,b,0,seconds,0,reserve,-tolerance,false,true,ref checks);
                    return new SkiFirstContactPreview(proof.Status==SkiContactSweepStatus.Clear?SkiFirstContactStatus.Reached:SkiFirstContactStatus.WorkLimit,
                        seconds,checks,geometry.GapMeters,proof);
                }
                double remaining=a.DurationSeconds-seconds;
                if(remaining==0)
                {
                    var proof=Search(first,a,second,b,0,a.DurationSeconds,0,reserve,tolerance,false,false,ref checks);
                    return new SkiFirstContactPreview(proof.Status==SkiContactSweepStatus.Clear?SkiFirstContactStatus.Clear:SkiFirstContactStatus.WorkLimit,
                        seconds,checks,geometry.GapMeters,proof);
                }
                double allowance=geometry.GapMeters-target-reserve;
                if(allowance<=0)break; // Numerical uncertainty cannot grant contact or clearance.
                double fraction=seconds/a.DurationSeconds;var va=a.VelocityAt(fraction);var vb=b.VelocityAt(fraction);
                double speed=Math.Abs((va.X-vb.X)*geometry.NormalX+(va.Z-vb.Z)*geometry.NormalZ)
                    +radius*(a.PeakYawRate+b.PeakYawRate);
                double acceleration=a.PlanarAccelerationBound+b.PlanarAccelerationBound;
                // Stable positive root of speed*t + acceleration*t*t/2 = allowance.
                double denominator=speed+Math.Sqrt(speed*speed+2*acceleration*allowance);
                double step=denominator==0?remaining:Math.Min(remaining,2*allowance/denominator);
                double next=step==remaining?a.DurationSeconds:seconds+step;
                if(next<=seconds)break;
                seconds=next;geometry=Geometry(first,a,second,b,seconds/a.DurationSeconds);
            }
            return new SkiFirstContactPreview(SkiFirstContactStatus.WorkLimit,seconds,checks,geometry.GapMeters);
        }

        private SkiContactSweepPreview PreviewCore(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b,bool continuation,bool arrival=false)
        {
            if(!first.IsValid||!second.IsValid||first.Equals(second)||!a.IsValid||!b.IsValid||a.DurationSeconds!=b.DurationSeconds)
                throw new ArgumentException("Distinct actors and equal-duration force-weighted frames required.");
            if(string.CompareOrdinal(first.Value,second.Value)>0)return PreviewCore(second,b,first,a,continuation,arrival);
            // Cover errors at both the midpoint used for exclusion and any encoded
            // sample subsequently committed from the admitted prefix.
            double reserve=Reserve(a,b);
            if(!continuation&&!arrival&&FarApart(a,b,reserve))
                return new SkiContactSweepPreview(SkiContactSweepStatus.Clear,a.DurationSeconds,a.DurationSeconds,0,reserve);
            var start=Geometry(first,a,second,b,0);
            if(arrival)
            {
                var end=Geometry(first,a,second,b,1);
                if(end.GapMeters>tolerance||end.GapMeters<=-tolerance+reserve)
                    return new SkiContactSweepPreview(SkiContactSweepStatus.UnreachedArrival,0,a.DurationSeconds,0,reserve,arrival:true);
            }
            if(continuation&&start.GapMeters>tolerance+reserve)
                throw new ArgumentException("Existing-contact continuation must begin at contact proximity.");
            double requiredGap=continuation||arrival?-tolerance:tolerance;
            if(start.GapMeters<=requiredGap+reserve)
                return new SkiContactSweepPreview(SkiContactSweepStatus.InitialProximity,0,0,0,reserve,continuation,arrival);
            int checks=0;
            return Search(first,a,second,b,0,a.DurationSeconds,0,reserve,requiredGap,continuation,arrival,ref checks);
        }

        private double Reserve(ManualSkiWorldTrajectory a,ManualSkiWorldTrajectory b)
            =>2*(a.PositionErrorBoundMeters+b.PositionErrorBoundMeters+radius*(a.HeadingErrorBoundRadians+b.HeadingErrorBoundRadians));

        // Relative translation retains cancellation for a fast co-moving pack.
        // Acceleration still uses both full bounds; circumscribed bodies cover yaw.
        private bool FarApart(ManualSkiWorldTrajectory a,ManualSkiWorldTrajectory b,double reserve)
        {
            var pa=a.Sample(0);var pb=b.Sample(0);double dx=pa.X-pb.X,dz=pa.Z-pb.Z;
            double dt=a.DurationSeconds;var va=a.VelocityAt(0);var vb=b.VelocityAt(0);
            double vx=va.X-vb.X,vz=va.Z-vb.Z;
            double reach=2*radius+tolerance+reserve+dt*Math.Sqrt(vx*vx+vz*vz)+
                .5*(a.PlanarAccelerationBound+b.PlanarAccelerationBound)*dt*dt;
            return dx*dx+dz*dz>reach*reach;
        }

        private SkiContactSweepPreview Search(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b,
            double from,double to,int depth,double reserve,double requiredGap,bool continuation,bool arrival,ref int checks)
        {
            if(checks>=definition.MaximumIntervalChecks)
                return new SkiContactSweepPreview(SkiContactSweepStatus.WorkLimit,from,to,checks,reserve,continuation,arrival);
            checks++;
            double mid=from+(to-from)*.5,h=(to-from)*.5,fraction=mid/a.DurationSeconds;
            var geometry=Geometry(first,a,second,b,fraction);
            var va=a.VelocityAt(fraction);var vb=b.VelocityAt(fraction);
            // Keep the midpoint separating axis fixed over the entire interval.
            // Translation uses relative projected velocity plus acceleration; rotation
            // only inflates this interval bound, never the permanent occupied body.
            double speed=Math.Abs((va.X-vb.X)*geometry.NormalX+(va.Z-vb.Z)*geometry.NormalZ);
            double translation=speed*h+.5*(a.PlanarAccelerationBound+b.PlanarAccelerationBound)*h*h;
            double rotation=radius*(Math.Min(2,a.PeakYawRate*h)+Math.Min(2,b.PeakYawRate*h));
            if(geometry.GapMeters>requiredGap+reserve+translation+rotation)
                return new SkiContactSweepPreview(SkiContactSweepStatus.Clear,to,to,checks,reserve,continuation,arrival);
            if(RotatingAxisClear(a,b,fraction,h,requiredGap+reserve))
                return new SkiContactSweepPreview(SkiContactSweepStatus.Clear,to,to,checks,reserve,continuation,arrival);
            if(to-from<=definition.TimeToleranceSeconds)
                return new SkiContactSweepPreview(SkiContactSweepStatus.PotentialContact,from,to,checks,reserve,continuation,arrival);
            if(depth>=definition.MaximumDepth||mid<=from||mid>=to)
                return new SkiContactSweepPreview(SkiContactSweepStatus.WorkLimit,from,to,checks,reserve,continuation,arrival);
            var left=Search(first,a,second,b,from,mid,depth+1,reserve,requiredGap,continuation,arrival,ref checks);
            if(left.Status!=SkiContactSweepStatus.Clear)return left;
            return Search(first,a,second,b,mid,to,depth+1,reserve,requiredGap,continuation,arrival,ref checks);
        }

        // A fixed axis loses all cancellation when touching skis rotate together.
        // Each body's continuously rotating SAT axis is also a valid separator.
        // Use a Taylor enclosure only for an actual constant angular law and
        // sign-stable support terms; cusp intervals retain the original bound.
        private bool RotatingAxisClear(ManualSkiWorldTrajectory a,ManualSkiWorldTrajectory b,double fraction,double halfSeconds,double required)
        {
            if(!a.TryGetConstantAngularAcceleration(out double aa)||!b.TryGetConstantAngularAcceleration(out double ba))return false;
            return Axis(a,b,aa,ba,true)||Axis(a,b,aa,ba,false)||Axis(b,a,ba,aa,true)||Axis(b,a,ba,aa,false);
            bool Axis(ManualSkiWorldTrajectory first,ManualSkiWorldTrajectory second,double alpha,double otherAlpha,bool forward)
            {
                var p=first.Sample(fraction);var q=second.Sample(fraction);var v=first.VelocityAt(fraction);var u=second.VelocityAt(fraction);
                double nx=forward?Math.Sin(p.HeadingRadians):Math.Cos(p.HeadingRadians);
                double nz=forward?Math.Cos(p.HeadingRadians):-Math.Sin(p.HeadingRadians);
                double dx=p.X-q.X,dz=p.Z-q.Z,offset=dx*nx+dz*nz;
                if(offset<0){nx=-nx;nz=-nz;offset=-offset;}
                double fx=Math.Sin(q.HeadingRadians),fz=Math.Cos(q.HeadingRadians);
                double along=fx*nx+fz*nz,side=fz*nx-fx*nz;
                double w=p.YawRateRadiansPerSecond,r=q.YawRateRadiansPerSecond-w,ar=otherAlpha-alpha,h=halfSeconds;
                double wr=Math.Abs(r)+Math.Abs(ar)*h,angle=wr*h;
                if((Math.Abs(along)<=angle||Math.Abs(side)<=angle)&&(r!=0||ar!=0))return false;
                double gap=offset-(forward?halfLength:halfWidth)-halfLength*Math.Abs(along)-halfWidth*Math.Abs(side);
                double vx=v.X-u.X,vz=v.Z-u.Z;
                double derivative=vx*nx+vz*nz+w*(dx*nz-dz*nx)-
                    r*(halfLength*Math.Sign(along)*side-halfWidth*Math.Sign(side)*along);
                double acceleration=first.PlanarAccelerationBound+second.PlanarAccelerationBound;
                double relativeSpeed=Math.Sqrt(vx*vx+vz*vz)+acceleration*h;
                double distance=Math.Sqrt(dx*dx+dz*dz)+relativeSpeed*h+.5*acceleration*h*h;
                double peak=Math.Abs(w)+Math.Abs(alpha)*h;
                double secondDerivative=acceleration+2*relativeSpeed*peak+distance*(Math.Abs(alpha)+peak*peak)+
                    (halfLength+halfWidth)*(Math.Abs(ar)+wr*wr);
                return gap>required+Math.Abs(derivative)*h+.5*secondDerivative*h*h;
            }
        }

        private SkiContactPreview Geometry(AthleteId first,ManualSkiWorldTrajectory a,AthleteId second,ManualSkiWorldTrajectory b,double fraction)
        {
            // Zero velocities avoid invoking response while querying geometry. Mass is
            // an unused unit placeholder, not an athlete physics parameter.
            return contact.Preview(new ManualSkiContactBody(first,a.Sample(fraction),default,1),
                new ManualSkiContactBody(second,b.Sample(fraction),default,1));
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum SkiContactSweepStatus { Clear, InitialProximity, PotentialContact, WorkLimit }

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
        internal SkiContactSweepPreview(SkiContactSweepStatus status,double from,double to,int checks,double reserve)
        {IsValid=true;Status=status;ClearUntilSeconds=from;IntervalEndSeconds=to;IntervalChecks=checks;GeometryReserveMeters=reserve;}
    }

    /// <summary>Continuous conservative pair exclusion for force-weighted manual turns.
    /// Uses actual oriented boxes. No actor position, race state or event is committed.</summary>
    public sealed class ManualSkiContactSweep
    {
        private readonly ManualSkiContactSystem contact;
        private readonly SkiContactSweepDefinition definition;
        private readonly double radius,tolerance;
        public string ContextJson {get;}
        public ManualSkiContactSweep(OccupancyDefinition body,SkiContactDefinition contactDefinition,SkiContactSweepDefinition definition)
        {
            if(body==null||contactDefinition==null||definition==null)throw new ArgumentNullException();
            contact=new ManualSkiContactSystem(body,contactDefinition);this.definition=definition;
            radius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation);
            tolerance=contactDefinition.ContactTolerance;
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_force_turn_sweep_v1",contact.ContextJson,definition.ToJson()});
        }

        public SkiContactSweepPreview Preview(AthleteId first,ManualSkiHeadingFrame a,AthleteId second,ManualSkiHeadingFrame b)
        {
            if(!first.IsValid||!second.IsValid||first.Equals(second)||!a.HasForceTrajectory||!b.HasForceTrajectory||a.DurationSeconds!=b.DurationSeconds)
                throw new ArgumentException("Distinct actors and equal-duration force-weighted frames required.");
            if(string.CompareOrdinal(first.Value,second.Value)>0)return Preview(second,b,first,a);
            // Cover errors at both the midpoint used for exclusion and any encoded
            // sample subsequently committed from the admitted prefix.
            double reserve=2*(a.PositionErrorBoundMeters+b.PositionErrorBoundMeters
                +radius*(a.HeadingErrorBoundRadians+b.HeadingErrorBoundRadians));
            var start=Geometry(first,a,second,b,0);
            if(start.GapMeters<=tolerance+reserve)
                return new SkiContactSweepPreview(SkiContactSweepStatus.InitialProximity,0,0,0,reserve);
            int checks=0;
            return Search(first,a,second,b,0,a.DurationSeconds,0,reserve,ref checks);
        }

        private SkiContactSweepPreview Search(AthleteId first,ManualSkiHeadingFrame a,AthleteId second,ManualSkiHeadingFrame b,
            double from,double to,int depth,double reserve,ref int checks)
        {
            if(checks>=definition.MaximumIntervalChecks)
                return new SkiContactSweepPreview(SkiContactSweepStatus.WorkLimit,from,to,checks,reserve);
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
            if(geometry.GapMeters>tolerance+reserve+translation+rotation)
                return new SkiContactSweepPreview(SkiContactSweepStatus.Clear,to,to,checks,reserve);
            if(to-from<=definition.TimeToleranceSeconds)
                return new SkiContactSweepPreview(SkiContactSweepStatus.PotentialContact,from,to,checks,reserve);
            if(depth>=definition.MaximumDepth||mid<=from||mid>=to)
                return new SkiContactSweepPreview(SkiContactSweepStatus.WorkLimit,from,to,checks,reserve);
            var left=Search(first,a,second,b,from,mid,depth+1,reserve,ref checks);
            if(left.Status!=SkiContactSweepStatus.Clear)return left;
            return Search(first,a,second,b,mid,to,depth+1,reserve,ref checks);
        }

        private SkiContactPreview Geometry(AthleteId first,ManualSkiHeadingFrame a,AthleteId second,ManualSkiHeadingFrame b,double fraction)
        {
            // Zero velocities avoid invoking response while querying geometry. Mass is
            // an unused unit placeholder, not an athlete physics parameter.
            return contact.Preview(new ManualSkiContactBody(first,a.Sample(fraction),default,1),
                new ManualSkiContactBody(second,b.Sample(fraction),default,1));
        }
    }
}

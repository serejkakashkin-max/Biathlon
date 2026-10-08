using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Accepted observation and high water on one explicitly owned
    /// physical occurrence. Neither arc is the skier's physical odometer.</summary>
    public readonly struct ManualRacePathCursor
    {
        internal ManualRacePathCatalog Owner {get;}
        public bool IsValid=>Owner!=null&&Athlete.IsValid;
        public AthleteId Athlete {get;}
        public int OccurrenceIndex {get;}
        public long ObservedArcNanometers {get;}
        public long CreditedArcNanometers {get;}
        public long CreditedRouteNanometers {get;}
        internal ManualRacePathCursor(ManualRacePathCatalog owner,AthleteId athlete,int occurrence,
            long observed,long credited,long route)
        {Owner=owner;Athlete=athlete;OccurrenceIndex=occurrence;ObservedArcNanometers=observed;
            CreditedArcNanometers=credited;CreditedRouteNanometers=route;}
    }

    /// <summary>Observed world endpoint, not a body-clearance or race-commit proof.</summary>
    public readonly struct ManualRacePathActorFrame
    {
        public bool IsValid=>Athlete.IsValid&&Pose.IsValid&&Observation.IsValid;
        public AthleteId Athlete {get;}
        public ManualSkiHeadingState Pose {get;}
        public SkiPlanarVelocity Velocity {get;}
        public ManualRacePathObservation Observation {get;}
        public ManualRacePathActorFrame(AthleteId athlete,ManualSkiHeadingState pose,
            SkiPlanarVelocity velocity,ManualRacePathObservation observation)
        {
            if(!athlete.IsValid||!pose.IsValid||!observation.IsValid)
                throw new ArgumentException("Initialized athlete, world pose and scoped observation required.");
            Athlete=athlete;Pose=pose;Velocity=velocity;Observation=observation;
        }
    }

    /// <summary>Event-free high-water proposal. The owning race must commit it
    /// together with physical movement, physiology, clocks and lifecycle.</summary>
    public readonly struct ManualRacePathProgressFrame
    {
        public bool IsValid=>Cursor.IsValid;
        public ManualRacePathCursor Cursor {get;}
        public long NewlyCreditedArcNanometers {get;}
        public long NewlyCreditedRouteNanometers {get;}
        internal ManualRacePathProgressFrame(ManualRacePathCursor cursor,long arc,long route)
        {Cursor=cursor;NewlyCreditedArcNanometers=arc;NewlyCreditedRouteNanometers=route;}
    }

    /// <summary>Scoped high-water observation and ordinary zero-credit seam
    /// handoff. Shooting, penalty and finish gates stay owned by race lifecycle.
    /// This does not admit bodies or solve a complete world tick.</summary>
    public sealed class ManualRacePathProgression
    {
        private readonly ManualRacePathCatalog catalog;
        public string ContextJson {get;}
        public string HandoffContextJson {get;}
        public ManualRacePathProgression(ManualRacePathCatalog catalog)
        {
            this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_scoped_physical_arc_high_water_v1",
                catalog.ContextJson,"start_penalty_arc_not_base_distance;backward_observation_retains_high_water"});
            HandoffContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_scoped_ordinary_zero_credit_handoff_v1",
                ContextJson,"accepted_exact_end;same_world_pose;ordered_base_flow_only;race_phase_gates_closed"});
        }

        /// <summary>Restore only an already accepted race-phase snapshot. This
        /// constructor does not authorize entering the selected occurrence.</summary>
        public ManualRacePathCursor Reconstitute(AthleteId athlete,int occurrenceIndex,long observedArcNanometers,
            long creditedArcNanometers,long creditedRouteNanometers)
        {
            if(!athlete.IsValid||occurrenceIndex<0||occurrenceIndex>=catalog.Count)
                throw new ArgumentException("Valid athlete and selected physical occurrence required.");
            var row=catalog.Get(occurrenceIndex);
            if(observedArcNanometers<0||observedArcNanometers>row.LengthNanometers||
                creditedArcNanometers<observedArcNanometers||creditedArcNanometers>row.LengthNanometers||
                creditedRouteNanometers!=Mapped(row,creditedArcNanometers))
                throw new ArgumentException("Observed/credited arc and base credit must match this exact occurrence.");
            return new ManualRacePathCursor(catalog,athlete,occurrenceIndex,observedArcNanometers,
                creditedArcNanometers,creditedRouteNanometers);
        }
        private static long Mapped(ManualRacePathOccurrence row,long arc)
            =>checked(row.RouteStartNanometers+(row.CountsTowardsRoute?arc:0));

        /// <summary>Observe one already safe endpoint without mutating accepted
        /// state. The race owner supplies the maximum admitted physical arc.</summary>
        public bool TryPreview(ManualRacePathActorFrame safe,ManualRacePathCursor accepted,
            long maximumArcNanometers,out ManualRacePathProgressFrame proposal)
        {
            proposal=default;
            if(!safe.IsValid||!accepted.IsValid||!ReferenceEquals(accepted.Owner,catalog)||
                !ReferenceEquals(safe.Observation.Owner,catalog)||!safe.Athlete.Equals(accepted.Athlete)||
                safe.Observation.OccurrenceIndex!=accepted.OccurrenceIndex)return false;
            var row=catalog.Get(accepted.OccurrenceIndex);long observed=safe.Observation.PhysicalArcNanometers;
            if(maximumArcNanometers<accepted.CreditedArcNanometers||maximumArcNanometers>row.LengthNanometers||
                observed<0||observed>maximumArcNanometers||safe.Observation.LoopId!=row.LoopId||
                safe.Observation.MappedRouteNanometers!=Mapped(row,observed)||
                !catalog.TryProject(safe.Pose,row.Index,observed,0,out var actual)||
                actual.PhysicalArcNanometers!=observed||actual.LateralMeters!=safe.Observation.LateralMeters||
                actual.HeightMeters!=safe.Observation.HeightMeters)return false;
            long credit=Math.Max(accepted.CreditedArcNanometers,observed),route=Mapped(row,credit);
            proposal=new ManualRacePathProgressFrame(new ManualRacePathCursor(catalog,accepted.Athlete,row.Index,
                observed,credit,route),credit-accepted.CreditedArcNanometers,route-accepted.CreditedRouteNanometers);
            return true;
        }

        /// <summary>Validate the whole roster before changing any proposal slot.
        /// All actors here use this exact catalog; heterogeneous lanes require
        /// the owning world roster to combine their separate scoped proposals.</summary>
        public bool TryPreviewGroup(ManualRacePathActorFrame[] safe,ManualRacePathCursor[] accepted,
            long[] maximumArcsNanometers,int count,ManualRacePathProgressFrame[] proposals)
        {
            if(safe==null||accepted==null||maximumArcsNanometers==null||proposals==null||
                count<1||count>safe.Length||count>accepted.Length||count>maximumArcsNanometers.Length||count>proposals.Length)
                throw new ArgumentException("Full observed roster, accepted cursors, caps and proposal slots required.");
            for(int i=0;i<count;i++)
            {
                for(int j=0;j<i;j++)if(accepted[i].Athlete.Equals(accepted[j].Athlete))
                    throw new ArgumentException("Duplicate athlete in scoped progress roster.");
                if(!TryPreview(safe[i],accepted[i],maximumArcsNanometers[i],out _))return false;
            }
            for(int i=0;i<count;i++)
            {TryPreview(safe[i],accepted[i],maximumArcsNanometers[i],out var proposed);proposals[i]=proposed;}
            return true;
        }

        /// <summary>Propose an ordinary adjacent occurrence only when the actual
        /// accepted body centre observes both exact endpoints. It preserves pose,
        /// heading, velocity, odometer, clocks and base credit. Junction/body
        /// clearance and every lifecycle gate still need separate admission.</summary>
        public bool TryPreviewOrdinaryHandoff(ManualRacePathCursor accepted,ManualRacePathActorFrame safeAtEnd,
            out ManualRacePathCursor next)
        {
            next=default;
            if(!accepted.IsValid||!ReferenceEquals(accepted.Owner,catalog))return false;
            if(!TryPreview(safeAtEnd,accepted,catalog.Get(accepted.OccurrenceIndex).LengthNanometers,
                out var end))return false;
            var row=catalog.Get(accepted.OccurrenceIndex);
            if(row.End!=ManualRacePathEnd.Continue||accepted.ObservedArcNanometers!=row.LengthNanometers||
                accepted.CreditedArcNanometers!=row.LengthNanometers||
                end.Cursor.ObservedArcNanometers!=row.LengthNanometers||end.NewlyCreditedArcNanometers!=0)return false;
            int index=catalog.GetBaseFlowSuccessor(row.Index);if(index<0)return false;
            var successor=catalog.Get(index);
            if(successor.RouteStartNanometers!=accepted.CreditedRouteNanometers||
                !catalog.TryProject(safeAtEnd.Pose,index,0,0,out var projected)||projected.PhysicalArcNanometers!=0)return false;
            next=new ManualRacePathCursor(catalog,accepted.Athlete,index,0,0,accepted.CreditedRouteNanometers);
            return true;
        }
    }
}

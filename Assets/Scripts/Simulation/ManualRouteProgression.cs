using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>The last world observation and the furthest earned station on one
    /// selected route occurrence. Neither value is a physical ski odometer.</summary>
    public readonly struct ManualRouteProgressCursor
    {
        public bool IsValid => Athlete.IsValid;
        public AthleteId Athlete { get; }
        public int SectionIndex { get; }
        public long ObservedStationNanometers { get; }
        public long CreditedStationNanometers { get; }
        internal ManualRouteProgressCursor(AthleteId athlete,int section,long observed,long credited)
        { Athlete=athlete;SectionIndex=section;ObservedStationNanometers=observed;CreditedStationNanometers=credited; }
    }

    /// <summary>Event-free route credit proposed from an already safe world frame.
    /// A later race transaction must commit this and all other actor state together.</summary>
    public readonly struct ManualRouteProgressFrame
    {
        public bool IsValid => Cursor.IsValid;
        public ManualRouteProgressCursor Cursor { get; }
        public long NewlyCreditedNanometers { get; }
        internal ManualRouteProgressFrame(ManualRouteProgressCursor cursor,long newlyCredited)
        { Cursor=cursor;NewlyCreditedNanometers=newlyCredited; }
    }

    /// <summary>Conservative base-route credit for manual world motion. It cannot
    /// authorize a section seam, start approach, private range path or penalty lap.</summary>
    public sealed class ManualRouteProgression
    {
        private readonly RaceRoute route;
        public string ContextJson { get; }
        public ManualRouteProgression(RaceRoute route)
        {
            this.route=route??throw new ArgumentNullException(nameof(route));
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_same_section_high_water_credit_v1",
                route.TrackSnapshotJson,route.PlanSnapshotJson,route.RequirementsSnapshotJson});
        }

        /// <summary>Initialize only from an accepted race snapshot. A new race
        /// starts at section zero/station zero; later sections need explicit
        /// topology admission before this cursor is reconstructed.</summary>
        public ManualRouteProgressCursor Reconstitute(AthleteId athlete,int section,long observed,long credited)
        {
            if(!athlete.IsValid||section<0||section>=route.SectionCount)
                throw new ArgumentException("Valid athlete and selected route section required.");
            var selected=route.GetSection(section);
            long start=checked(selected.Start.Millimeters*SkiState.NanometersPerMillimeter);
            long end=checked((selected.Start.Millimeters+selected.Length.Millimeters)*SkiState.NanometersPerMillimeter);
            if(observed<start||observed>end||credited<observed||credited>end)
                throw new ArgumentException("Observed and credited stations must belong to the selected occurrence.");
            return new ManualRouteProgressCursor(athlete,section,observed,credited);
        }

        /// <summary>Preview all safe actor frames atomically. Each maximum station
        /// is supplied by the owning race phase (range entry, firing line, finish,
        /// or current section end). Crossing any unadmitted boundary rejects the
        /// entire proposal and leaves all caller-owned output slots untouched.</summary>
        public bool TryPreviewGroup(ManualRouteActorFrame[] safeFrames,ManualRouteProgressCursor[] accepted,
            long[] maximumStationsNanometers,int count,ManualRouteProgressFrame[] proposals)
        {
            if(safeFrames==null||accepted==null||maximumStationsNanometers==null||proposals==null||
                count<1||safeFrames.Length<count||accepted.Length<count||
                maximumStationsNanometers.Length<count||proposals.Length<count)
                throw new ArgumentException("Complete roster, limits and proposal slots required.");
            for(int i=0;i<count;i++)
            {
                for(int j=0;j<i;j++)if(accepted[i].Athlete.Equals(accepted[j].Athlete))
                    throw new ArgumentException("Duplicate accepted athlete cursor.");
                if(!TryPreviewOne(safeFrames[i],accepted[i],maximumStationsNanometers[i],out _))return false;
            }
            for(int i=0;i<count;i++)
            {
                TryPreviewOne(safeFrames[i],accepted[i],maximumStationsNanometers[i],out var proposal);
                proposals[i]=proposal;
            }
            return true;
        }

        private bool TryPreviewOne(ManualRouteActorFrame safe,ManualRouteProgressCursor accepted,
            long maximumStation,out ManualRouteProgressFrame proposal)
        {
            proposal=default;
            if(!safe.IsValid||!accepted.IsValid||!safe.Athlete.Equals(accepted.Athlete)||
                accepted.SectionIndex<0||accepted.SectionIndex>=route.SectionCount)return false;
            var section=route.GetSection(accepted.SectionIndex);
            long start=checked(section.Start.Millimeters*SkiState.NanometersPerMillimeter);
            long end=checked((section.Start.Millimeters+section.Length.Millimeters)*SkiState.NanometersPerMillimeter);
            long observed=safe.Projection.RouteNanometers;
            if(accepted.ObservedStationNanometers<start||accepted.ObservedStationNanometers>end||
                accepted.CreditedStationNanometers<accepted.ObservedStationNanometers||
                accepted.CreditedStationNanometers>end||maximumStation<accepted.CreditedStationNanometers||
                maximumStation>end||safe.Projection.SectionIndex!=accepted.SectionIndex||
                safe.Projection.LoopId!=section.LoopId||observed<start||observed>maximumStation)return false;
            long credit=Math.Max(accepted.CreditedStationNanometers,observed);
            proposal=new ManualRouteProgressFrame(new ManualRouteProgressCursor(accepted.Athlete,
                accepted.SectionIndex,observed,credit),credit-accepted.CreditedStationNanometers);
            return true;
        }
    }
}

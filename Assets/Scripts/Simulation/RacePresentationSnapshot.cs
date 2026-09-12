using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Detached completed-tick observations. No live session or mutable map escapes.</summary>
    public readonly struct AthletePresentationState
    {
        public string AthleteId { get; }
        public string LoopId { get; }
        public long LocalNanometers { get; }
        public double LateralMeters { get; }
        public double SpeedMetersPerSecond { get; }
        public PhysiologyState Physiology { get; }
        public EffortOutput Effort { get; }
        public DraftOutput Draft { get; }
        public CompetitionPhase Phase { get; }
        public int CompletedLaps { get; }
        public int CompletedBouts { get; }
        public int PendingPenaltyLoops { get; }
        public int TotalPenaltyLoops { get; }
        public bool OnCourse { get; }
        public int RangeLane { get; }
        public bool ShootingActive { get; }
        public int ClosedTargetsMask { get; }
        public int ShotsFired { get; }
        internal AthletePresentationState(PackSprintFieldSession field,int index)
        {
            var a=field.GetAthlete(index);var point=field.GetPhysicalPoint(index);var shooting=a.ActiveShooting;
            AthleteId=a.AthleteId.Value;LoopId=point.LoopId;LocalNanometers=point.LocalNanometers;LateralMeters=field.GetLateral(index);
            SpeedMetersPerSecond=a.Motion.SpeedMetersPerSecond;Physiology=a.PhysiologyState;Phase=a.Lifecycle.Phase;
            Effort=a.LastFrame.Physiology.Effort;Draft=field.GetDraft(index);
            CompletedLaps=a.Lifecycle.CompletedLaps;CompletedBouts=a.Lifecycle.CompletedBouts;PendingPenaltyLoops=a.Lifecycle.PendingPenaltyLoops;TotalPenaltyLoops=a.TotalPenaltyLoops;
            OnCourse=field.IsOnCourse(index);RangeLane=field.GetAssignedRangeLane(index);ShootingActive=shooting!=null;
            ClosedTargetsMask=shooting?.ClosedMask??0;ShotsFired=shooting?.ShotsFired??0;
        }
    }
    public sealed class RacePresentationSnapshot
    {
        private readonly AthletePresentationState[] athletes;
        public long Tick { get; }
        public int TickFrequency { get; }
        public string TrackId { get; }
        public string ProfileId { get; }
        public bool Complete { get; }
        public int Count=>athletes.Length;
        public AthletePresentationState Get(int index)=>athletes[index];
        private RacePresentationSnapshot(PackSprintFieldSession field)
        {
            Tick=field.Tick.Value;TickFrequency=field.TickFrequency;TrackId=field.Track.Id;ProfileId=field.Profile.Id;Complete=field.IsComplete;
            athletes=new AthletePresentationState[field.Count];for(int i=0;i<athletes.Length;i++)athletes[i]=new AthletePresentationState(field,i);
        }
        // Call on the simulation owner thread after Step, never during a prepared step.
        public static RacePresentationSnapshot Capture(PackSprintFieldSession field)
        {if(field==null)throw new ArgumentNullException(nameof(field));return new RacePresentationSnapshot(field);}
    }
    public readonly struct PresentationPosition
    {
        public string LoopId { get; }
        public double LocalMeters { get; }
        public double LateralMeters { get; }
        public bool Interpolated { get; }
        internal PresentationPosition(string loop,double local,double lateral,bool interpolated){LoopId=loop;LocalMeters=local;LateralMeters=lateral;Interpolated=interpolated;}
    }
    public static class RacePresentationSampling
    {
        public static PresentationPosition Position(RacePresentationSnapshot previous,RacePresentationSnapshot current,int index,double alpha)
        {
            if(current==null)throw new ArgumentNullException(nameof(current));if(double.IsNaN(alpha)||double.IsInfinity(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
            var b=current.Get(index);
            // Snap at route/phase seams, skipped ticks and changed rosters. Do not invent a
            // world-space shortcut between different physical branches or wrap backward.
            if(previous!=null&&previous.Tick+1==current.Tick&&previous.TickFrequency==current.TickFrequency&&previous.TrackId==current.TrackId&&previous.ProfileId==current.ProfileId&&index<previous.Count)
            {
                var a=previous.Get(index);
                if(a.AthleteId==b.AthleteId&&a.LoopId==b.LoopId&&a.Phase==b.Phase&&a.OnCourse&&b.OnCourse&&a.LocalNanometers<=b.LocalNanometers)
                    return new PresentationPosition(b.LoopId,(a.LocalNanometers+(b.LocalNanometers-a.LocalNanometers)*alpha)/SkiState.NanometersPerMeter,a.LateralMeters+(b.LateralMeters-a.LateralMeters)*alpha,true);
            }
            return new PresentationPosition(b.LoopId,b.LocalNanometers/(double)SkiState.NanometersPerMeter,b.LateralMeters,false);
        }
    }
}

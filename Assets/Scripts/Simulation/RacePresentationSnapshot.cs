using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Detached completed-tick observations. No live session or mutable map escapes.</summary>
    public readonly struct AthletePresentationState
    {
        public string AthleteId { get; }
        public RaceHudState Hud { get; }
        public string LoopId { get; }
        public long LocalNanometers { get; }
        public double LateralMeters { get; }
        public double SpeedMetersPerSecond { get; }
        public ManualSkiHeadingState WorldPose {get;}
        public SkiPlanarVelocity WorldVelocity {get;}
        public bool TuckActive { get; }
        public bool FinalSprintActive { get; }
        /// <summary>Completed-position course slope (rise/run) and signed turning curvature (1/m).</summary>
        public double CourseGrade { get; }
        public double CourseCurvaturePerMeter { get; }
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
        public ShootingPhase ShootingPhase { get; }
        public int ShootingPhaseDurationTicks { get; }
        public int ShootingPhaseRemainingTicks { get; }
        public int ShootingPhaseElapsedTicks { get; }
        public ShootingSwayFrame ShootingSway { get; }
        public bool BreathHolding { get; }
        public double BreathingPhaseCycles { get; }
        public int ClosedTargetsMask { get; }
        public int ShotsFired { get; }
        public WeaponAimState Sight { get; }
        public ShootingStance Stance { get; }
        public ShotOutcome ShotOutcome { get; }
        public TargetIntersection ShotImpact { get; }
        internal AthletePresentationState(PackSprintFieldSession field,int index,TrackSampler penaltySampler)
        {
            Hud=new RaceHudState(field,index);
            var a=field.GetAthlete(index);var point=field.GetPhysicalPoint(index);var shooting=a.ActiveShooting;
            AthleteId=a.AthleteId.Value;LoopId=point.LoopId;LocalNanometers=point.LocalNanometers;LateralMeters=field.GetLateral(index);
            SpeedMetersPerSecond=a.Motion.SpeedMetersPerSecond;Physiology=a.PhysiologyState;Phase=a.Lifecycle.Phase;
            var world=field.HasManualWorldState?field.GetManualWorldActor(index):default;
            WorldPose=world.Pose;WorldVelocity=world.Velocity;
            TuckActive=a.TuckActive;FinalSprintActive=a.FinalSprintActive;
            var range=a.CurrentRangeMotion;
            var course=a.RemainingStartApproachNanometers>0 ? field.StartApproach.Sample(a.RemainingStartApproachNanometers) :
                range.HasValue&&range.Value.Phase==RangeTraversalPhase.PenaltyLoop ?
                    penaltySampler.Sample(new TrackDistance(range.Value.PenaltyNanometers/SkiState.NanometersPerMillimeter)) :
                    a.RangePath==null ? field.Route.Sample(new TrackDistance(a.RouteNanometers/SkiState.NanometersPerMillimeter)).Track :
                    a.RangePath.SampleRoute(a.RouteNanometers);
            CourseGrade=course.Grade;CourseCurvaturePerMeter=course.CurvaturePerMeter;
            Effort=a.LastFrame.Physiology.Effort;Draft=field.GetDraft(index);
            CompletedLaps=a.Lifecycle.CompletedLaps;CompletedBouts=a.Lifecycle.CompletedBouts;PendingPenaltyLoops=a.Lifecycle.PendingPenaltyLoops;TotalPenaltyLoops=a.TotalPenaltyLoops;
            OnCourse=field.IsOnCourse(index);RangeLane=field.GetAssignedRangeLane(index);ShootingActive=shooting!=null;
            ShootingPhase=shooting?.Phase??ShootingPhase.Outside;
            ShootingPhaseDurationTicks=shooting?.PhaseDurationTicks??0;
            ShootingPhaseRemainingTicks=shooting?.PhaseRemainingTicks??0;
            ShootingPhaseElapsedTicks=shooting?.PhaseElapsedTicks??0;
            ShootingSway=shooting?.LastFrame.Sway??default;
            BreathHolding=shooting?.LastFrame.BreathControl.Holding??false;
            BreathingPhaseCycles=shooting?.LastFrame.Breathing.PhaseCycles??0;
            ClosedTargetsMask=shooting?.ClosedMask??0;ShotsFired=shooting?.ShotsFired??0;
            Sight=shooting?.Sight??default;Stance=field.Profile.GetBout(Math.Min(a.Lifecycle.CompletedBouts,field.Profile.BoutCount-1)).Stance;
            ShotOutcome=shooting?.LastFrame.Shot.Outcome??ShotOutcome.NoShot;ShotImpact=shooting?.LastFrame.Shot.Impact??default;
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
            var penaltySampler=new TrackSampler(field.Track.GetLoop(field.Track.Penalty.LoopId));
            athletes=new AthletePresentationState[field.Count];for(int i=0;i<athletes.Length;i++)athletes[i]=new AthletePresentationState(field,i,penaltySampler);
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

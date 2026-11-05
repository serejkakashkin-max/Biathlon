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
        public double ElapsedSeconds { get; }
        public double RouteMeters { get; }
        public int LastCompletedBoutClosedMask { get; }
        public double StartCountdownSeconds { get; }
        public ManualSkiHeadingState WorldPose {get;}
        public SkiPlanarVelocity WorldVelocity {get;}
        public SkiImpactObservation Impact {get;}
        public bool RangeTransitYielding {get;}
        public bool TuckActive { get; }
        public bool FinalSprintActive { get; }
        /// <summary>Completed-position slope (rise/run) and signed turning curvature (1/m).
        /// Manual world mode observes actual heading/yaw; legacy mode uses course metadata.</summary>
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
        public bool StartReleaseHeld {get;}
        public int RangeLane { get; }
        public bool ShootingActive { get; }
        public ShootingPhase ShootingPhase { get; }
        public int ShootingPhaseDurationTicks { get; }
        public int ShootingPhaseRemainingTicks { get; }
        public int ShootingPhaseElapsedTicks { get; }
        public ShootingSwayFrame ShootingSway { get; }
        public bool BreathHolding { get; }
        public double BreathingPhaseCycles { get; }
        /// <summary>Read-only audio/HUD rhythm from the same heartbeat that drives shooting sway.</summary>
        public long CompletedHeartbeats {get;}
        public int ClosedTargetsMask { get; }
        public int ShotsFired { get; }
        public WeaponAimState Sight { get; }
        /// <summary>Latest accepted control aim, before procedural sway/handling.</summary>
        public WeaponAimState ControlledAim {get;}
        /// <summary>Exact pre-ballistic sight used for the latest accepted shot.</summary>
        public WeaponAimState ShotSight {get;}
        public WindVector ShootingWind {get;}
        public bool FineAim {get;}
        public bool NeedsBoltCycle {get;} public bool BreathBlocked {get;} public double BreathDebt {get;} public double BreathSwayMultiplier {get;}
        public ShootingStance Stance { get; }
        public ShotOutcome ShotOutcome { get; }
        public TargetIntersection ShotImpact { get; }
        internal AthletePresentationState(PackSprintFieldSession field,int index,TrackSampler penaltySampler)
        {
            Hud=new RaceHudState(field,index);
            var a=field.GetAthlete(index);var point=field.GetPhysicalPoint(index);var shooting=a.ActiveShooting;
            AthleteId=a.AthleteId.Value;LoopId=point.LoopId;LocalNanometers=point.LocalNanometers;LateralMeters=field.GetLateral(index);
            SpeedMetersPerSecond=a.Motion.SpeedMetersPerSecond;Physiology=a.PhysiologyState;Phase=a.Lifecycle.Phase;
            ElapsedSeconds=a.Lifecycle.LastFrame.ElapsedSeconds;RouteMeters=a.RouteNanometers/(double)SkiState.NanometersPerMeter;
            LastCompletedBoutClosedMask=a.Lifecycle.CompletedBouts>0?a.GetBoutClosedMask(a.Lifecycle.CompletedBouts-1):0;
            StartCountdownSeconds=Math.Max(0,(a.ScheduledStart.Value-field.Tick.Value)/(double)field.TickFrequency);
            var world=field.HasManualWorldState&&!field.HasManualScopedWorldState?field.GetManualWorldActor(index):default;
            var scoped=field.HasManualScopedWorldState?field.GetManualScopedWorldActor(index):default;
            WorldPose=scoped.IsValid?scoped.Pose:world.Pose;WorldVelocity=scoped.IsValid?scoped.Velocity:world.Velocity;
            Impact=field.GetManualScopedImpact(index);
            RangeTransitYielding=field.IsRangeTransitYielding(index);
            TuckActive=a.TuckActive;FinalSprintActive=a.FinalSprintActive;
            var range=a.CurrentRangeMotion;
            var course=a.RemainingStartApproachNanometers>0 ? field.StartApproach.Sample(a.RemainingStartApproachNanometers) :
                range.HasValue&&range.Value.Phase==RangeTraversalPhase.PenaltyLoop ?
                    penaltySampler.Sample(new TrackDistance(range.Value.PenaltyNanometers/SkiState.NanometersPerMillimeter)) :
                    a.RangePath==null ? field.Route.Sample(new TrackDistance(a.RouteNanometers/SkiState.NanometersPerMillimeter)).Track :
                    a.RangePath.SampleRoute(a.RouteNanometers);
            CourseGrade=course.Grade;CourseCurvaturePerMeter=course.CurvaturePerMeter;
            if(world.IsValid)
            {
                double planar=Math.Sqrt(world.Velocity.X*world.Velocity.X+world.Velocity.Z*world.Velocity.Z);
                CourseGrade=world.Projection.GradeAlong(world.Pose);
                CourseCurvaturePerMeter=planar==0?0:world.Pose.YawRateRadiansPerSecond/planar;
            }
            if(scoped.IsValid)
            {
                double planar=Math.Sqrt(scoped.Velocity.X*scoped.Velocity.X+scoped.Velocity.Z*scoped.Velocity.Z);
                CourseGrade=scoped.Observation.GradeAlong(scoped.Pose);
                CourseCurvaturePerMeter=planar==0?0:scoped.Pose.YawRateRadiansPerSecond/planar;
            }
            Effort=a.LastFrame.Physiology.Effort;Draft=field.GetDraft(index);
            CompletedLaps=a.Lifecycle.CompletedLaps;CompletedBouts=a.Lifecycle.CompletedBouts;PendingPenaltyLoops=a.Lifecycle.PendingPenaltyLoops;TotalPenaltyLoops=a.TotalPenaltyLoops;
            OnCourse=field.IsOnCourse(index)&&field.HasEnteredManualStart(index);StartReleaseHeld=field.IsStartReleaseHeld(index);RangeLane=field.GetAssignedRangeLane(index);ShootingActive=shooting!=null;
            ShootingPhase=shooting?.Phase??ShootingPhase.Outside;
            ShootingPhaseDurationTicks=shooting?.PhaseDurationTicks??0;
            ShootingPhaseRemainingTicks=shooting?.PhaseRemainingTicks??0;
            ShootingPhaseElapsedTicks=shooting?.PhaseElapsedTicks??0;
            ShootingSway=shooting?.LastFrame.Sway??default;
            BreathHolding=shooting?.LastFrame.BreathControl.Holding??false;
            NeedsBoltCycle=shooting?.NeedsBoltCycle??false;BreathBlocked=shooting?.LastFrame.BreathControl.Blocked??false;
            BreathDebt=shooting?.LastFrame.BreathControl.Debt??0;BreathSwayMultiplier=shooting?.LastFrame.BreathControl.BreathingMultiplier??1;
            BreathingPhaseCycles=shooting?.LastFrame.Breathing.PhaseCycles??0;
            CompletedHeartbeats=shooting?.LastFrame.Heartbeat.CompletedBeats??0;
            ClosedTargetsMask=shooting?.ClosedMask??0;ShotsFired=shooting?.ShotsFired??0;
            Sight=shooting?.Sight??default;ControlledAim=shooting?.LastFrame.Aim??default;ShotSight=shooting?.LastFrame.ShotAim??default;
            ShootingWind=shooting?.LastFrame.Input.Wind??default;FineAim=shooting?.LastFrame.Input.Aim.FineControl??false;Stance=field.Profile.GetBout(Math.Min(a.Lifecycle.CompletedBouts,field.Profile.BoutCount-1)).Stance;
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
        /// <summary>Interpolate actual accepted world poses, including across
        /// path seams. This never derives a heading from route tangent knots.</summary>
        public static ManualSkiHeadingState WorldPose(RacePresentationSnapshot previous,RacePresentationSnapshot current,int index,double alpha)
        {
            if(current==null)throw new ArgumentNullException(nameof(current));
            PhysiologyNumbers.RequireRange(alpha,0,1,nameof(alpha));
            var b=current.Get(index);if(!b.WorldPose.IsValid)return default;
            if(previous!=null&&previous.Tick+1==current.Tick&&previous.TrackId==current.TrackId&&previous.ProfileId==current.ProfileId&&index<previous.Count)
            {
                var a=previous.Get(index);
                if(a.AthleteId==b.AthleteId&&a.WorldPose.IsValid)
                {
                    double delta=b.WorldPose.HeadingRadians-a.WorldPose.HeadingRadians;
                    delta=Math.Atan2(Math.Sin(delta),Math.Cos(delta));
                    return new ManualSkiHeadingState(a.WorldPose.X+(b.WorldPose.X-a.WorldPose.X)*alpha,
                        a.WorldPose.Z+(b.WorldPose.Z-a.WorldPose.Z)*alpha,a.WorldPose.HeadingRadians+delta*alpha,
                        a.WorldPose.YawRateRadiansPerSecond+(b.WorldPose.YawRateRadiansPerSecond-a.WorldPose.YawRateRadiansPerSecond)*alpha);
                }
            }
            return b.WorldPose;
        }
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

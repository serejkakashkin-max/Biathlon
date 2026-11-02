using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedSessionTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private static readonly SkiReachedContactPair[] NoPairs=Array.Empty<SkiReachedContactPair>();
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        internal sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source;
            internal readonly SprintAthleteSession Session;
            internal readonly RaceEventLog Events=new RaceEventLog(10000);
            internal readonly SkiModelDefinitions Model=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            internal readonly ManualScopedWorldRoster World;
            internal readonly int Occurrence;
            internal Fixture(string phase,bool backward=false,bool waiting=false)
            {
                Source=new ManualRacePathCatalogTests.Fixture(phase.StartsWith("start",StringComparison.Ordinal)?"mass_start":"sprint");
                var ski=TimeTrialTests.Athlete();var shooter=ShootingSessionTests.Athlete();
                shooter=ShootingAthleteDefinition.FromJson(shooter.ToJson().Replace(shooter.Id,ski.Id));
                Session=new SprintAthleteSession(new SimulationConfig(1,"scoped_session_test",20,123),Source.Profile,
                    Model,ShootingSessionTests.Model(),ski,shooter,Source.Venue.Track,Source.Route,
                    new CheckpointLayout(Source.Venue.Track,Source.Route),SnowConditionTests.Conditions(),"ski_neutral",
                    new SimulationTick(waiting?100:1),new ShootingLaneAssignment(Source.Venue.Track.Range.Id,Source.Path.LaneId,Source.Path.LaneOrdinal),
                    Events,Source.Start,phase.StartsWith("start",StringComparison.Ordinal)?20*N:0,Source.Path);
                if(!waiting)
                    while(!AtPhase(phase)&&Session.Tick.Value<30000)
                    {
                        var point=Session.CurrentRangeMotion;
                        bool clip=phase=="penalty_entry"&&point?.Phase==RangeTraversalPhase.PenaltyApproach||
                            phase=="penalty_repeat"&&point?.Phase==RangeTraversalPhase.PenaltyLoop&&Session.CurrentRangeRemainingPenaltyLoops>1||
                            phase=="penalty_return"&&point?.Phase==RangeTraversalPhase.PenaltyLoop&&Session.CurrentRangeRemainingPenaltyLoops==1;
                        if(clip)
                        {
                            var prepared=Session.PrepareStep(SprintAthleteSessionTests.Script(Session));
                            long remaining=point.Value.DistanceToBoundaryNanometers;
                            if(remaining>0&&prepared.Movement.Motion.DistanceNanometers-Session.Motion.DistanceNanometers>=remaining)
                            {Session.CommitStep(Session.ConstrainPreparedTravel(prepared,remaining));break;}
                            Session.CommitStep(prepared);
                        }
                        else Session.Step(SprintAthleteSessionTests.Script(Session));
                    }
                Assert.That(waiting||AtPhase(phase),Is.True,$"Could not reach {phase}: {Session.CurrentRangeMotion?.Phase}, tick {Session.Tick.Value}");
                long high=0;
                // A fixed tick may skip the first bout's tiny before-gate window.
                // Bind the captured penalty to the actual unfinished bout.
                int bout=0;
                if(phase.EndsWith("_before",StringComparison.Ordinal))
                    while(bout+1<Source.Profile.BoutCount&&Session.GetBoutRangeFrame(bout).Phase==RangeTraversalPhase.Complete)bout++;
                if(phase.StartsWith("start",StringComparison.Ordinal))
                {Occurrence=Source.Catalog.StartApproachIndex;high=Source.Start.LengthNanometers-Session.RemainingStartApproachNanometers;}
                else if(phase=="penalty_entry")
                {Occurrence=Source.Catalog.GetPenaltyEntryOccurrence(0);high=Source.Catalog.Get(Occurrence).LengthNanometers;}
                else if(phase=="penalty_repeat"||phase=="penalty_return")
                {
                    Occurrence=Source.Catalog.GetPenaltyOccurrence(0,Session.GetBoutRangeFrame(0).CompletedLoops);
                    high=Source.Catalog.Get(Occurrence).LengthNanometers;
                }
                else if(phase=="penalty"||phase=="penalty_repeat_before"||phase=="penalty_return_before")
                {Occurrence=Source.Catalog.GetPenaltyOccurrence(bout,Session.CurrentRangeRequiredPenaltyLoops-Session.CurrentRangeRemainingPenaltyLoops+1);high=Session.CurrentRangeMotion.Value.PenaltyNanometers;}
                else if(phase=="shooting")
                {Occurrence=Source.Catalog.GetFiringOccurrence(0);high=Source.Catalog.Get(Occurrence).LengthNanometers;}
                else
                {
                    Occurrence=-1;
                    for(int i=0;i<Source.Catalog.BaseFlowCount;i++)
                    {
                        var r=Source.Catalog.Get(i);
                        if(r.CountsTowardsRoute&&Session.RouteNanometers>=r.RouteStartNanometers&&Session.RouteNanometers<r.RouteEndNanometers)
                        {Occurrence=i;high=Session.RouteNanometers-r.RouteStartNanometers;break;}
                    }
                    Assert.That(Occurrence,Is.GreaterThanOrEqualTo(0));
                }
                long observed=high-(backward?N/10:0);
                var pose=Source.At(Occurrence,observed,heading:backward?Math.PI:(double?)null);
                if(phase=="penalty_entry_before"||phase=="penalty_repeat_before"||phase=="penalty_return_before")
                {
                    var row=Source.Catalog.Get(Occurrence);
                    Assert.That(Source.Catalog.TrySample(Occurrence,row.LengthNanometers,out var end),Is.True);
                    int target=phase=="penalty_entry_before"?Source.Catalog.GetPenaltyOccurrence(row.BoutIndex,1):
                        phase=="penalty_repeat_before"?Source.Catalog.GetPenaltyOccurrence(row.BoutIndex,row.PenaltyOrdinal+1):Source.Catalog.GetPenaltyReturnOccurrence(row.BoutIndex);
                    Assert.That(Source.Catalog.TrySample(target,0,out var to),Is.True);
                    double vx=end.ForwardX+to.ForwardX,vz=end.ForwardZ+to.ForwardZ,norm=Math.Sqrt(vx*vx+vz*vz);
                    vx/=norm;vz/=norm;
                    double gap=(end.X-pose.X)*end.ForwardX+(end.Z-pose.Z)*end.ForwardZ;
                    double side=-gap*(vx*end.ForwardZ-vz*end.ForwardX)/(vx*end.ForwardX+vz*end.ForwardZ);
                    pose=Source.At(Occurrence,observed,side,Math.Atan2(vx,vz));
                }
                if(backward)
                {
                    var tangent=Source.At(Occurrence,observed);
                    pose=new ManualSkiHeadingState(pose.X,pose.Z,tangent.HeadingRadians+Math.PI);
                }
                // The synthetic offset above belongs to authored geometry, not
                // the oblique portal's station chart. Restore the actual local
                // observation while retaining the already earned high-water
                // credit. No pose or phase endpoint is moved to fit a hint.
                bool movingPhase=phase=="penalty_entry_before"||phase=="penalty_repeat_before"||phase=="penalty_return_before";
                Assert.That(Source.Catalog.TryProject(pose,Occurrence,observed,movingPhase?.001:0,out var at),Is.True);
                if(movingPhase)
                {
                    Assert.That(at.PhysicalArcNanometers,Is.LessThanOrEqualTo(high));
                    observed=at.PhysicalArcNanometers;
                }
                double planar=Session.Motion.SpeedMetersPerSecond/Math.Sqrt(1+Math.Pow(at.GradeAlong(pose),2));
                var velocity=new SkiPlanarVelocity(planar*Math.Sin(pose.HeadingRadians),planar*Math.Cos(pose.HeadingRadians));
                var cursor=new ManualRacePathProgression(Source.Catalog).Reconstitute(Session.AthleteId,Occurrence,observed,high,Session.RouteNanometers);
                World=new ManualScopedWorldRoster(new[]{Source.Catalog},Source.Venue.Occupancy,
                    SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_session_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"),
                    SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_session_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}"),
                    new[]{cursor},new[]{pose},new[]{velocity},new[]{Session.Motion},20,Session.Tick);
            }
            private bool AtPhase(string phase)
            {
                var p=Session.CurrentRangeMotion;
                switch(phase)
                {
                    case "start":return Session.Tick.Value>3&&Session.RemainingStartApproachNanometers>0;
                    case "start_seam":return Session.Motion.SpeedMetersPerSecond>0&&Session.RemainingStartApproachNanometers>0&&
                        Session.RemainingStartApproachNanometers/(double)N/Session.Motion.SpeedMetersPerSecond<.049;
                    case "base_seam":
                        for(int i=0;i<Source.Catalog.BaseFlowCount-1;i++)
                        {
                            var r=Source.Catalog.Get(i);
                            if(r.Kind==ManualRacePathKind.BaseRoute&&r.End==ManualRacePathEnd.Continue&&
                                Source.Catalog.Get(i+1).Kind==ManualRacePathKind.RangeApproach&&Session.RouteNanometers<r.RouteEndNanometers&&
                                Session.RouteNanometers>=r.RouteStartNanometers&&Session.Motion.SpeedMetersPerSecond>0&&
                                (r.RouteEndNanometers-Session.RouteNanometers)/(double)N/Session.Motion.SpeedMetersPerSecond<.049)return true;
                        }
                        return false;
                    case "base":return Session.RouteNanometers>10*N;
                    case "approach":return p?.Phase==RangeTraversalPhase.Approach&&Session.RouteNanometers>Source.Venue.Track.Range.Entry.Distance.Millimeters*SkiState.NanometersPerMillimeter+3*N;
                    case "exit":return p?.Phase==RangeTraversalPhase.RangeExit&&Session.RouteNanometers>Source.Path.FiringRouteNanometers(0)+3*N;
                    case "penalty":return p?.Phase==RangeTraversalPhase.PenaltyLoop&&p.Value.PenaltyNanometers>10*N;
                    case "penalty_entry_before":return p?.Phase==RangeTraversalPhase.PenaltyApproach&&
                        p.Value.DistanceToBoundaryNanometers>0&&Session.Motion.SpeedMetersPerSecond>0&&
                        p.Value.DistanceToBoundaryNanometers/(double)N/Session.Motion.SpeedMetersPerSecond<.035;
                    case "penalty_repeat_before":case "penalty_return_before":return p?.Phase==RangeTraversalPhase.PenaltyLoop&&
                        (phase=="penalty_repeat_before"?Session.CurrentRangeRemainingPenaltyLoops>1:Session.CurrentRangeRemainingPenaltyLoops==1)&&
                        p.Value.DistanceToBoundaryNanometers>0&&Session.Motion.SpeedMetersPerSecond>0&&
                        p.Value.DistanceToBoundaryNanometers/(double)N/Session.Motion.SpeedMetersPerSecond<.035;
                    case "penalty_entry":return p?.Phase==RangeTraversalPhase.PenaltyLoop&&p.Value.PenaltyNanometers==0&&Session.CurrentRangeRemainingPenaltyLoops>0;
                    case "penalty_repeat":return p?.Phase==RangeTraversalPhase.PenaltyLoop&&p.Value.PenaltyNanometers==0&&
                        Session.CurrentRangeRemainingPenaltyLoops<Session.CurrentRangeRequiredPenaltyLoops;
                    case "penalty_return":
                        var bout=Session.GetBoutRangeFrame(0);
                        return (bout.Phase==RangeTraversalPhase.Rejoin||bout.Phase==RangeTraversalPhase.Complete)&&bout.RemainingLoops==0;
                    case "shooting":return p?.Phase==RangeTraversalPhase.Shooting;
                    default:throw new ArgumentException();
                }
            }
            internal SprintAthleteInput Command(bool tuck=false)=>new SprintAthleteInput(
                new TimeTrialInput(new SimulationInput(Session.Tick.Next(),Session.AthleteId,800),1,1,0,tuckRequested:tuck),
                new WeaponAimCommand(Session.Tick.Next(),Session.AthleteId,0,0),true);
            internal SprintAthleteSession WaitingSibling(RaceEventLog events)
            {
                var original=TimeTrialTests.Athlete();var ski=TimeTrialAthlete.FromJson(original.ToJson().Replace(original.Id,"scoped_sibling"));
                var shooter=ShootingSessionTests.Athlete();shooter=ShootingAthleteDefinition.FromJson(shooter.ToJson().Replace(shooter.Id,ski.Id));
                return new SprintAthleteSession(new SimulationConfig(1,"scoped_session_test",20,123),Source.Profile,
                    Model,ShootingSessionTests.Model(),ski,shooter,Source.Venue.Track,Source.Route,
                    new CheckpointLayout(Source.Venue.Track,Source.Route),SnowConditionTests.Conditions(),"ski_neutral",new SimulationTick(100),
                    new ShootingLaneAssignment(Source.Venue.Track.Range.Id,Source.Path.LaneId,Source.Path.LaneOrdinal),events,Source.Start,20*N,Source.Path);
            }
            internal bool Propose(SprintStepPreview prepared,out ManualScopedWorldPreview proof,int steering=0)
            {
                var actor=World.GetActor(0);var at=actor.Observation;
                ManualSkiWorldTrajectory trajectory;
                if(prepared.MovementActive)
                {
                    var input=Session.GetPreparedManualMotionInput(prepared,World,0,steering);
                    var solver=new ManualSkiMotionSystem(Model.Physics,
                        ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/manual_ski_steering_v2.json")),Model.Cornering,20);
                    trajectory=new ManualSkiWorldTrajectory(solver.PreviewAcceptedVelocity(Session.Motion,actor.Pose,actor.Velocity,
                        at,Source.Catalog.SampleSurface(at),input).Heading);
                }
                else
                {
                    var snow=new ManualSkiSnowContactSystem(Source.Venue.Occupancy,1);
                    trajectory=new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75),
                        new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,.05));
                }
                var c=World.GetCursor(0);
                return World.TryPrepare(new[]{new SkiWorldMotionCandidate(actor.Athlete,trajectory)},
                    new[]{new ManualScopedWorldWindow(actor.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers)},
                    NoPairs,0,NoEdges,0,new[]{at.GradeAlong(actor.Pose)},out proof);
            }
        }

        [TestCase("start")][TestCase("base")][TestCase("approach")][TestCase("exit")][TestCase("penalty")]
        public void PhysicalOccurrenceInputAndWholeTickCommitUseActualSurfaceAndAcceptedRacePhase(string phase)
        {
            var f=new Fixture(phase);var actor=f.World.GetActor(0);var before=f.World.GetCursor(0);
            var state=f.Session.PhysiologyState;var motion=f.Session.Motion;var tick=f.Session.Tick;int events=f.Events.Count;
            var p=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            Assert.That(p.ManualWorldRequired,Is.True);Assert.That(p.Movement,Is.EqualTo(default(RacePathMotionFrame)));
            var surface=f.Source.Catalog.SampleSurface(actor.Observation);
            var effort=new EffortSystem(f.Session.PhysiologyDefinition).Evaluate(800,state,1);
            var fatigue=new FatigueSystem(f.Session.PhysiologyDefinition,20).Step(state.MuscularFatigue,effort.DeliveredRelativeEffort,surface.TerrainMuscleFactor);
            Assert.That(p.Physiology.State.MuscularFatigue,Is.EqualTo(fatigue.Value));
            var input=f.Session.GetPreparedManualMotionInput(p,f.World,0,450);
            Assert.That(input.SteeringInput,Is.EqualTo(450));
            Assert.Throws<InvalidOperationException>(()=>f.Session.CommitStep(p));
            Assert.Throws<InvalidOperationException>(()=>f.Session.ConstrainPreparedTravel(p,0));
            Assert.That(f.Session.Motion,Is.EqualTo(motion));Assert.That(f.Session.PhysiologyState,Is.EqualTo(state));Assert.That(f.Events.Count,Is.EqualTo(events));
            Assert.That(f.Propose(p,out var proof,450),Is.True,f.World.LastAttempt.Status.ToString());
            var expectedMotion=f.World.GetProposedPhysicalMotion(proof,0);var after=f.World.GetProposedCursor(proof,0);
            new ManualScopedRosterSessionCommit(new[]{f.Session},f.World).Commit(new[]{p},proof);
            Assert.That(f.Session.Tick.Value,Is.EqualTo(tick.Value+1));Assert.That(f.World.Tick,Is.EqualTo(f.Session.Tick));
            Assert.That(f.Session.Motion,Is.EqualTo(expectedMotion));Assert.That(f.Session.RouteNanometers,Is.EqualTo(after.CreditedRouteNanometers));
            Assert.That(f.Session.PhysiologyState,Is.EqualTo(p.Physiology.State));Assert.That(f.Session.Checkpoints.Tick,Is.EqualTo(f.Session.Tick));
            Assert.That(f.World.GetActor(0).Pose.HeadingNanoradians,Is.Not.EqualTo(actor.Pose.HeadingNanoradians));
            if(phase=="penalty")
            {Assert.That(f.Session.RouteNanometers,Is.EqualTo(before.CreditedRouteNanometers));Assert.That(f.Session.CurrentRangeMotion.Value.PenaltyNanometers,Is.EqualTo(after.CreditedArcNanometers));}
            if(phase=="start")Assert.That(f.Session.RemainingStartApproachNanometers,Is.EqualTo(f.Source.Start.LengthNanometers-after.CreditedArcNanometers));
        }
        [Test] public void ReverseStartTravelCannotSpendRemainingApproachOrGrantRaceCredit()
        {
            var f=new Fixture("start",backward:true);long remaining=f.Session.RemainingStartApproachNanometers,odo=f.Session.Motion.DistanceNanometers;
            long observed=f.World.GetCursor(0).ObservedArcNanometers;var p=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            Assert.That(f.Propose(p,out var proof),Is.True,f.World.LastAttempt.Status.ToString());
            new ManualScopedRosterSessionCommit(new[]{f.Session},f.World).Commit(new[]{p},proof);
            Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(odo));Assert.That(f.World.GetCursor(0).ObservedArcNanometers,Is.LessThan(observed));
            Assert.That(f.Session.RemainingStartApproachNanometers,Is.EqualTo(remaining));Assert.That(f.Session.RouteNanometers,Is.Zero);
            var next=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            Assert.That(f.Session.GetPreparedManualMotionInput(next,f.World,0,0).SteeringInput,Is.Zero);
            Assert.Throws<InvalidOperationException>(()=>f.Session.PrepareStep(f.Command()),"A bound manual session cannot fall back to automatic route movement.");
        }
        [TestCase("approach")][TestCase("exit")][TestCase("penalty")]
        public void ReversedPhysicalRangeMovementRetainsAcceptedPhaseHighWater(string phase)
        {
            var f=new Fixture(phase,backward:true);var before=f.World.GetCursor(0);long odo=f.Session.Motion.DistanceNanometers;
            var range=f.Session.CurrentRangeMotion.Value;var p=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            Assert.That(f.Propose(p,out var proof),Is.True,f.World.LastAttempt.Status.ToString());
            new ManualScopedRosterSessionCommit(new[]{f.Session},f.World).Commit(new[]{p},proof);
            Assert.That(f.World.GetCursor(0).ObservedArcNanometers,Is.LessThan(before.ObservedArcNanometers));
            Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(odo));Assert.That(f.Session.RouteNanometers,Is.EqualTo(before.CreditedRouteNanometers));
            Assert.That(f.Session.CurrentRangeMotion.Value,Is.EqualTo(range));
            Assert.That(f.Session.PrepareManualScopedStep(f.Command(),f.World,0).ManualWorldRequired,Is.True);
        }
        [Test] public void WarmActualOccurrencePreparationAndSharedInputQueriesAllocateNothing()
        {
            var f=new Fixture("approach");var command=f.Command();
            for(int i=0;i<128;i++){var p=f.Session.PrepareManualScopedStep(command,f.World,0);f.Session.GetPreparedManualMotionInput(p,f.World,0,400);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){var p=f.Session.PrepareManualScopedStep(command,f.World,0);f.Session.GetPreparedManualMotionInput(p,f.World,0,400);}
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(used,Is.Zero);
        }
        [TestCase(false)][TestCase(true)]
        public void StationaryShootingAndScheduledStartStillRequireAnUnchangedFullWorldBody(bool waiting)
        {
            var f=new Fixture(waiting?"start":"shooting",waiting:waiting);var source=f.World.GetActor(0);var motion=f.Session.Motion;
            var p=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);Assert.That(p.MovementActive,Is.False);Assert.That(p.ShootingActive,Is.EqualTo(!waiting));
            Assert.Throws<ArgumentException>(()=>f.Session.GetPreparedManualMotionInput(p,f.World,0,0));
            Assert.That(f.Propose(p,out var proof),Is.True,f.World.LastAttempt.Status.ToString());
            new ManualScopedRosterSessionCommit(new[]{f.Session},f.World).Commit(new[]{p},proof);
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(source.Pose));Assert.That(f.Session.Motion,Is.EqualTo(motion));
            Assert.That(f.Session.Tick,Is.EqualTo(f.World.Tick));
            if(!waiting)Assert.That(f.Session.ActiveShooting.Phase,Is.Not.EqualTo(ShootingPhase.Outside));
        }
        [Test] public void ForeignPathOrClockCannotReplaceTheOwnedPreparedSource()
        {
            var f=new Fixture("base");var other=new Fixture("base");var command=f.Command();
            var p=f.Session.PrepareManualScopedStep(command,f.World,0);
            Assert.Throws<ArgumentException>(()=>f.Session.GetPreparedManualMotionInput(p,other.World,0,0));
            Assert.That(f.Propose(p,out var proof),Is.True);
            Assert.Throws<ArgumentException>(()=>f.Session.PrepareManualScopedStep(command,other.World,0));
            Assert.Throws<ArgumentException>(()=>f.Session.PrepareManualScopedCommit(p,proof,0));
            Assert.That(f.Session.Tick,Is.EqualTo(f.World.Tick));
            Assert.That(f.Session.RouteNanometers,Is.EqualTo(f.World.GetCursor(0).CreditedRouteNanometers));
        }
        [Test] public void StaleLastSessionCannotPublishTheEarlierRosterPhysiologyWorldOrEvents()
        {
            var f=new Fixture("start",waiting:true);var siblingEvents=new RaceEventLog(100);var second=f.WaitingSibling(siblingEvents);
            var first=f.World.GetActor(0);var c=f.World.GetCursor(0);
            var poses=new[]{first.Pose,f.Source.At(f.Occurrence,c.ObservedArcNanometers,side:f.Source.Venue.Occupancy.LateralSeparation+.1)};
            var ids=new[]{f.Session.AthleteId,second.AthleteId};var cursors=new ManualRacePathCursor[2];
            for(int i=0;i<2;i++)cursors[i]=new ManualRacePathProgression(f.Source.Catalog).Reconstitute(ids[i],c.OccurrenceIndex,c.ObservedArcNanometers,c.CreditedArcNanometers,c.CreditedRouteNanometers);
            var world=new ManualScopedWorldRoster(new[]{f.Source.Catalog,f.Source.Catalog},f.Source.Venue.Occupancy,
                SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_session_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"),
                SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_session_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}"),
                cursors,poses,new[]{default(SkiPlanarVelocity),default(SkiPlanarVelocity)},new[]{f.Session.Motion,second.Motion},20);
            var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session,second},world);
            var pending=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0),second.PrepareManualScopedStep(SprintAthleteSessionTests.Script(second),world,1)};
            var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);var candidates=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];
            for(int i=0;i<2;i++)
            {
                var actor=world.GetActor(i);var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                candidates[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(body,new SkiContactMotion(actor.Velocity,0),0,0,.05)));
                windows[i]=new ManualScopedWorldWindow(ids[i],c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers);
            }
            Assert.That(world.TryPrepare(candidates,windows,NoPairs,0,NoEdges,0,new[]{0.0,0.0},out var proof),Is.True,world.LastAttempt.Status+" / "+world.LastAttempt.Course.Status);
            second.Step(SprintAthleteSessionTests.Script(second)); // The final prepared source is now stale.
            var state=f.Session.PhysiologyState;var motion=f.Session.Motion;int events=f.Events.Count;
            Assert.Throws<ArgumentException>(()=>transaction.Commit(pending,proof));
            Assert.That(f.Session.Tick.Value,Is.Zero);Assert.That(world.Tick.Value,Is.Zero);Assert.That(f.Session.Motion,Is.EqualTo(motion));
            Assert.That(f.Session.PhysiologyState,Is.EqualTo(state));Assert.That(f.Events.Count,Is.EqualTo(events));
            Assert.That(world.GetActor(0).Pose,Is.EqualTo(first.Pose));Assert.That(second.Tick.Value,Is.EqualTo(1));
        }
    }
}

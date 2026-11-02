using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedFieldPublicationTests
    {
        private const long N=SkiState.NanometersPerMeter;
        internal sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source;
            internal readonly SprintFieldSession Independent;
            internal readonly PackSprintFieldSession Pack;
            internal ISprintFieldView View=>Pack==null?(ISprintFieldView)Independent:Pack;
            internal readonly RaceEventLog Events=new RaceEventLog(20000);
            internal readonly ManualRacePathCatalog[] Catalogs;
            internal readonly ManualScopedWorldRoster World;
            internal readonly ManualScopedRosterSessionCommit Commit;
            internal readonly SprintAthleteInput[] Inputs;
            internal readonly SprintStepPreview[] Pending;
            internal readonly SkiWorldMotionCandidate[] Candidates;
            internal readonly ManualScopedWorldWindow[] Windows;
            internal readonly double[] Grades;
            internal readonly DraftOutput[] Drafts;
            private readonly CheckpointLayout checkpoints;
            private readonly ManualSkiSnowContactSystem snow;
            internal Fixture(string phase="base",bool pack=true,bool smallerBody=false,bool reusable=false)
            {
                Source=new ManualRacePathCatalogTests.Fixture(phase=="start"?"mass_start":"sprint");
                snow=new ManualSkiSnowContactSystem(Source.Venue.Occupancy,1);
                checkpoints=new CheckpointLayout(Source.Venue.Track,Source.Route);
                int count=phase=="start"?4:1;var ids=new AthleteId[count];var entrants=new SprintEntrant[count];
                for(int i=0;i<count;i++)
                {
                    var ski=TimeTrialTests.Athlete();var shot=ShootingSessionTests.Athlete();string id="scoped_field_"+i;
                    ski=TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id));shot=ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,id));
                    entrants[i]=new SprintEntrant(ski,shot,"ski_neutral");ids[i]=entrants[i].AthleteId;
                }
                var model=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);var layout=new PhysicalRangeFieldLayout(
                    new PhysicalTrackMap(Source.Venue.Track,Source.Route),Source.Venue.Track,Source.Route,Source.Venue.Paths);
                var config=new SimulationConfig(1,"scoped_field",20,123);var timing=ResultTimingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("result_timing_v1.json"));
                MassStartGrid grid=null;
                if(Source.Start!=null)
                {
                    var loop=Source.Venue.Track.GetLoop(Source.Start.LoopId);double width=double.MaxValue;
                    for(int i=0;i<loop.SegmentCount;i++)width=Math.Min(width,loop.GetSegment(i).WidthMeters);
                    grid=new MassStartGrid(Source.Profile,MassStartGridDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_grid_provisional_v1.json")),
                        Source.Venue.Occupancy,ids,width,loop.Length.Meters);
                }
                if(pack)Pack=new PackSprintFieldSession(config,Source.Profile,model,ShootingSessionTests.Model(),Source.Venue.Track,Source.Route,
                    SnowConditionTests.Conditions(),timing,entrants,new SimulationTick(1),"scoped_field_result",Source.Venue.Occupancy,
                    DraftDefinition.FromJson(PhysiologyContractTests.DefinitionJson("draft_provisional_v1.json")),
                    OvertakeDefinition.FromJson(PhysiologyContractTests.DefinitionJson("overtake_provisional_v1.json")),
                    Source.Venue.Junctions,Source.Venue.Connections,Events,massGrid:grid,startApproach:Source.Start,
                    rangeLayout:layout,authoredStartApproach:Source.Start!=null,startContinuation:Source.Start==null?null:Source.Venue.StartContinuation,
                    reusableRangeMats:reusable,routeBoundJunctions:true);
                else Independent=new SprintFieldSession(config,Source.Profile,model,ShootingSessionTests.Model(),Source.Venue.Track,Source.Route,
                    SnowConditionTests.Conditions(),timing,entrants,new SimulationTick(1),"scoped_field_result",Events,rangeLayout:layout);
                Inputs=new SprintAthleteInput[count];Pending=new SprintStepPreview[count];Candidates=new SkiWorldMotionCandidate[count];
                Windows=new ManualScopedWorldWindow[count];Grades=new double[count];Drafts=new DraftOutput[count];
                var lines=new double[count];
                while(!AtPhase(phase)&&View.Tick.Value<30000)
                {
                    for(int i=0;i<count;i++)
                    {
                        Inputs[i]=SprintAthleteSessionTests.Script(View.GetAthlete(i));lines[i]=Pack?.GetLateral(i)??0;
                        // Paid legacy fixture setup moves the outer start rows
                        // inward by0.2 m, retaining separation. The initial full
                        // grid and body-straddling seam still need the later
                        // composite course qualification; this is publication.
                        if(phase=="start")lines[i]=Math.Sign(lines[i])*1.8;
                    }
                    if(Pack!=null)Pack.Step(Inputs,lines);else Independent.Step(Inputs);
                }
                Assert.That(AtPhase(phase),Is.True,phase+" / "+View.GetAthlete(count-1).CurrentRangeMotion?.Phase);
                Catalogs=new ManualRacePathCatalog[count];var cursors=new ManualRacePathCursor[count];var poses=new ManualSkiHeadingState[count];
                var velocities=new SkiPlanarVelocity[count];var motions=new SkiState[count];var sessions=new SprintAthleteSession[count];
                for(int i=0;i<count;i++)
                {
                    var a=View.GetAthlete(i);sessions[i]=a;var c=Catalogs[i]=new ManualRacePathCatalog(Source.Venue.Track,Source.Route,
                        Source.Profile,Source.Venue.Visual,a.RangePath,Source.Start);int occurrence=-1;long arc=0;
                    if(a.RemainingStartApproachNanometers>0){occurrence=c.StartApproachIndex;arc=c.Get(occurrence).LengthNanometers-a.RemainingStartApproachNanometers;}
                    else
                    {
                        int b=0;while(b<Source.Profile.BoutCount&&a.GetBoutRangeFrame(b).Phase==RangeTraversalPhase.Complete)b++;
                        if(b<Source.Profile.BoutCount&&a.GetBoutRangeFrame(b).Phase==RangeTraversalPhase.PenaltyLoop)
                        {occurrence=c.GetPenaltyOccurrence(b,a.GetBoutRangeFrame(b).CompletedLoops+1);arc=a.GetBoutRangeFrame(b).PenaltyNanometers;}
                        else if(b<Source.Profile.BoutCount&&a.GetBoutRangeFrame(b).Phase==RangeTraversalPhase.Shooting)
                        {occurrence=c.GetFiringOccurrence(b);arc=c.Get(occurrence).LengthNanometers;}
                        else for(int j=0;j<c.BaseFlowCount;j++)
                        {
                            var r=c.Get(j);if(r.CountsTowardsRoute&&a.RouteNanometers>=r.RouteStartNanometers&&a.RouteNanometers<r.RouteEndNanometers)
                            {occurrence=j;arc=a.RouteNanometers-r.RouteStartNanometers;break;}
                        }
                    }
                    Assert.That(occurrence,Is.GreaterThanOrEqualTo(0));Assert.That(c.TrySample(occurrence,arc,out var p),Is.True);
                    double side=Pack?.GetLateral(i)??0;poses[i]=new ManualSkiHeadingState(p.X+p.ForwardZ*side,p.Z-p.ForwardX*side,Math.Atan2(p.ForwardX,p.ForwardZ));
                    Assert.That(c.TryProject(poses[i],occurrence,arc,0,out var at),Is.True);double speed=a.Motion.SpeedMetersPerSecond/Math.Sqrt(1+Math.Pow(at.GradeAlong(poses[i]),2));
                    velocities[i]=new SkiPlanarVelocity(speed*Math.Sin(poses[i].HeadingRadians),speed*Math.Cos(poses[i].HeadingRadians));motions[i]=a.Motion;
                    cursors[i]=new ManualRacePathProgression(c).Reconstitute(a.AthleteId,occurrence,arc,arc,a.RouteNanometers);
                }
                var body=smallerBody?OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Source.Venue.Occupancy.ToJson(),"body_width_m","0.3")):Source.Venue.Occupancy;
                World=new ManualScopedWorldRoster(Catalogs,body,SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_field_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"),
                    SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_field_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}"),cursors,poses,velocities,motions,20,View.Tick);
                Commit=new ManualScopedRosterSessionCommit(sessions,World);
            }
            private bool AtPhase(string phase)
            {
                var a=View.GetAthlete(View.Count-1);var r=a.CurrentRangeMotion;
                switch(phase)
                {
                    case "start":return View.GetAthlete(0).RouteNanometers>1.2*N&&a.RemainingStartApproachNanometers>0;
                    case "base":return a.RouteNanometers>10*N;
                    case "checkpoint":return a.Checkpoints.Count<checkpoints.Count&&a.Motion.SpeedMetersPerSecond>0&&
                        checkpoints.Get(a.Checkpoints.Count).RouteNanometers>a.RouteNanometers&&
                        (checkpoints.Get(a.Checkpoints.Count).RouteNanometers-a.RouteNanometers)/(double)N/a.Motion.SpeedMetersPerSecond<.049;
                    case "approach":return r?.Phase==RangeTraversalPhase.Approach&&a.RouteNanometers>Source.Venue.Track.Range.Entry.Distance.Millimeters*SkiState.NanometersPerMillimeter+3*N;
                    case "shooting":return r?.Phase==RangeTraversalPhase.Shooting;
                    case "exit":return r?.Phase==RangeTraversalPhase.RangeExit&&a.RouteNanometers>a.RangePath.FiringRouteNanometers(0)+3*N;
                    case "penalty":return r?.Phase==RangeTraversalPhase.PenaltyLoop&&r.Value.PenaltyNanometers>10*N;
                    default:throw new ArgumentException();
                }
            }
            internal ManualScopedWorldPreview Prepare()
            {
                bool success=TryPrepare(out var proof);
                Assert.That(success,Is.True,World.LastAttempt.Status+" / "+World.LastAttempt.Course.Status+" / "+World.LastAttempt.Athlete.Value);
                return proof;
            }
            internal bool TryPrepare(out ManualScopedWorldPreview proof)
            {
                for(int i=0;i<View.Count;i++)
                {
                    var a=View.GetAthlete(i);Inputs[i]=SprintAthleteSessionTests.Script(a);Pending[i]=a.PrepareManualScopedStep(Inputs[i],World,i);
                    var body=World.GetActor(i);var c=World.GetCursor(i);
                    Candidates[i]=new SkiWorldMotionCandidate(a.AthleteId,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                        new ManualSkiContactBody(a.AthleteId,body.Pose,body.Velocity,75),new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond),0,0,.05)));
                    Windows[i]=new ManualScopedWorldWindow(a.AthleteId,c.OccurrenceIndex,c.ObservedArcNanometers,2,Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
                    Grades[i]=body.Observation.GradeAlong(body.Pose);
                }
                return World.TryPrepare(Candidates,Windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,Grades,out proof);
            }
            internal void Publish(ManualScopedWorldPreview proof)
            {if(Pack!=null)Commit.CommitPackField(Pack,Inputs,Pending,proof,Drafts);else Commit.CommitField(Independent,Inputs,Pending,proof);}
        }
        [TestCase("start")][TestCase("base")][TestCase("approach")][TestCase("shooting")][TestCase("exit")][TestCase("penalty")]
        public void ActivePhaseWorldReceiptPublishesPackClockPositionAndDetachedPresentation(string phase)
        {
            var f=new Fixture(phase);var proof=f.Prepare();long tick=f.View.Tick.Value;string old=f.Pack.ContextJson;
            f.Publish(proof);Assert.That(f.View.Tick.Value,Is.EqualTo(tick+1));Assert.That(f.World.Tick,Is.EqualTo(f.View.Tick));
            Assert.That(f.Pack.HasManualScopedWorldState,Is.True);var snapshot=RacePresentationSnapshot.Capture(f.Pack);
            for(int i=0;i<f.View.Count;i++)
            {
                Assert.That(f.View.GetAthlete(i).Motion,Is.EqualTo(f.World.GetPhysicalMotion(i)));
                Assert.That(f.Pack.GetManualScopedWorldActor(i).Pose,Is.EqualTo(f.World.GetActor(i).Pose));
                Assert.That(snapshot.Get(i).WorldPose,Is.EqualTo(f.World.GetActor(i).Pose));Assert.That(snapshot.Get(i).WorldVelocity,Is.EqualTo(f.World.GetActor(i).Velocity));
                Assert.That(snapshot.Get(i).CourseGrade,Is.EqualTo(f.World.GetActor(i).Observation.GradeAlong(f.World.GetActor(i).Pose)));
                Assert.That(f.Pack.GetPhysicalPoint(i).LoopId,Is.EqualTo(f.World.GetActor(i).Observation.LoopId));
            }
            Assert.That(f.Pack.ContextJson,Is.Not.EqualTo(old));string context=f.Pack.ContextJson;f.Publish(f.Prepare());
            Assert.That(f.Pack.ContextJson,Is.EqualTo(context));Assert.That(snapshot.Tick,Is.EqualTo(tick+1));
            Assert.Throws<InvalidOperationException>(()=>f.Pack.Step(f.Inputs,new double[f.View.Count]));
        }
        [Test] public void IndependentFieldPublishesScopedClockAndRejectsLegacyBeforeSchedulerMutation()
        {
            var f=new Fixture(pack:false);var proof=f.Prepare();long tick=f.View.Tick.Value;f.Publish(proof);
            Assert.That(f.View.Tick.Value,Is.EqualTo(tick+1));Assert.That(f.World.Tick,Is.EqualTo(f.View.Tick));
            Assert.That(f.Independent.ContextJson,Does.Contain("manual_scoped_field_clock_publication_v2_context_reference"));
            Assert.That(f.Independent.ContextJson,Does.Contain("world_context_sha256:"+ManualContextIdentity.Sha256(f.World.ContextJson)));
            int events=f.Events.Count;Assert.Throws<InvalidOperationException>(()=>f.Independent.Step(f.Inputs));Assert.That(f.Events.Count,Is.EqualTo(events));
            Assert.That(f.View.Tick.Value,Is.EqualTo(tick+1));
        }
        [TestCase(true,false,false)][TestCase(false,true,false)][TestCase(false,false,true)]
        public void StalePreviewForeignBodyOrReusableMatOwnershipCannotPartiallyPublish(bool stale,bool body,bool reusable)
        {
            var f=new Fixture(smallerBody:body,reusable:reusable);var proof=f.Prepare();long tick=f.View.Tick.Value;var motion=f.View.GetAthlete(0).Motion;
            var point=f.Pack.GetPhysicalPoint(0);int events=f.Events.Count;string context=f.Pack.ContextJson;
            if(stale)f.View.GetAthlete(0).PrepareManualScopedStep(f.Inputs[0],f.World,0);
            Assert.Catch(()=>f.Publish(proof));Assert.That(f.View.Tick.Value,Is.EqualTo(tick));Assert.That(f.World.Tick.Value,Is.EqualTo(tick));
            Assert.That(f.Pack.GetPhysicalPoint(0),Is.EqualTo(point));Assert.That(f.View.GetAthlete(0).Motion,Is.EqualTo(motion));
            Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(f.Pack.ContextJson,Is.EqualTo(context));Assert.That(f.Pack.HasManualScopedWorldState,Is.False);
        }
        [Test] public void AStaleLastMassStartRowCannotPublishTheEarlierRowsOrPackSideState()
        {
            var f=new Fixture("start");var proof=f.Prepare();long tick=f.View.Tick.Value;int events=f.Events.Count;
            var first=f.View.GetAthlete(0).Motion;var point=f.Pack.GetPhysicalPoint(0);var last=f.View.GetAthlete(f.View.Count-1);
            last.PrepareManualScopedStep(f.Inputs[f.View.Count-1],f.World,f.View.Count-1);
            Assert.Throws<ArgumentException>(()=>f.Publish(proof));Assert.That(f.View.Tick.Value,Is.EqualTo(tick));
            Assert.That(f.World.Tick.Value,Is.EqualTo(tick));Assert.That(f.View.GetAthlete(0).Motion,Is.EqualTo(first));
            Assert.That(f.Pack.GetPhysicalPoint(0),Is.EqualTo(point));Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(f.Pack.HasManualScopedWorldState,Is.False);
        }
        [Test] public void AScopedCrossingPublishesTheSameExactSplitToTheFieldTimingBoard()
        {
            var f=new Fixture("checkpoint");int previous=f.View.GetAthlete(0).Checkpoints.Count;var proof=f.Prepare();f.Publish(proof);
            var a=f.View.GetAthlete(0);Assert.That(a.Checkpoints.Count,Is.EqualTo(previous+1));var split=a.Checkpoints.Get(previous);
            Assert.That(f.View.Timing.Get(a.AthleteId,previous).GlobalTime,Is.EqualTo(split.GlobalTime));
            Assert.That(split.GlobalTime.WholeTicks,Is.EqualTo(f.View.Tick.Value-1));
        }
        [TestCase(false)][TestCase(true)] public void WarmScopedFieldPublicationHasNoPerTickManagedAllocation(bool pack)
        {
            var f=new Fixture(pack:pack);bool success=true;
            for(int i=0;i<32;i++)
            {bool ok=f.TryPrepare(out var proof);success&=ok;if(ok)f.Publish(proof);}
            Assert.That(success,Is.True);long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<128;i++)
            {bool ok=f.TryPrepare(out var proof);success&=ok;if(ok)f.Publish(proof);}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(success,Is.True);Assert.That(allocated,Is.Zero);
            Assert.That(f.World.Tick,Is.EqualTo(f.View.Tick));
        }
    }
}

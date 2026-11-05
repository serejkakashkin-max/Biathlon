using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using Biathlon.Presentation;
using System.IO;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class ManualStartReleaseGateTests
    {
        internal static PursuitQualificationSnapshot Qualification(int count)
        {
            var sprint=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/Venues/woodland_basin_v1/sprint_profile"));
            var pursuit=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/Venues/woodland_basin_v1/pursuit_profile"));
            var ids=new AthleteId[count];for(int i=0;i<count;i++)ids[i]=new AthleteId("start_gate_"+i);
            var book=new SprintResultBook(sprint,SprintResultsTests.Timing(),ids,20,new RaceEventLog());
            for(int i=0;i<count;i++)
            {
                var life=new CompetitionLifecycle(sprint,ids[i],20,new RaceEventLog());
                life.Step(new SimulationTick(1),CompetitionAction.Start);life.Step(new SimulationTick(2),CompetitionAction.EnterRange);
                life.Step(new SimulationTick(3),CompetitionAction.CompleteBout,31,5);life.Step(new SimulationTick(4),CompetitionAction.ReachLapBoundary);
                life.Step(new SimulationTick(5),CompetitionAction.EnterRange);life.Step(new SimulationTick(6),CompetitionAction.CompleteBout,31,5);
                life.Step(new SimulationTick(7),CompetitionAction.ReachLapBoundary);
                long finish=201+i*6;for(long t=8;t<finish;t++)life.Step(new SimulationTick(t));
                life.Step(new SimulationTick(finish),CompetitionAction.Finish,finish:new FinishTime(finish,0,20));book.Record(life);
            }
            return PursuitQualificationSnapshot.Create("held_start_qualifiers",book.Complete("controlled_sprint_start_source"),pursuit);
        }
        internal static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) Create(int version=3,string format="pursuit",int count=8)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_profile"));var events=new RaceEventLog(100000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,events,new AiDecisionCounts(),
                format=="pursuit"?Qualification(count):null,count:count,startContinuationVersion:version);
            var movement=ManualProductionFieldComposition.Create(runner.Field,venue,PhysiologyContractTests.DefinitionJson);
            runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v4.json")),ManualScopedFieldStepperTests.Steering(),completeSnowSearch:true);
            return(runner,movement,events);
        }
        internal static void Player(PackAiRaceRunner runner,int effort)
        {
            var a=runner.Field.GetAthlete(0);var tick=runner.Field.Tick.Next();
            runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,effort),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0)),0,true);
        }
        [Test] public void LaterPursuitEntrantWaitsWhenReleasedLineIsStillOccupied()
        {
            var f=Create();Assert.That(f.runner.Field.GetLateral(0),Is.EqualTo(f.runner.Field.GetLateral(7)));
            for(int i=0;i<120;i++)
            {
                Player(f.runner,0);
                if(f.runner.Field.Tick.Value==43)Assert.That(RacePresentationSnapshot.Capture(f.runner.Field).Get(7).OnCourse,Is.False,"The due start event must not render a body before physical admission.");
            }
            Assert.That(f.runner.Field.Tick.Value,Is.EqualTo(120));
            Assert.That(f.stepper.World.IsStartReleaseHeld(7),Is.True);
            Assert.That(f.runner.Field.IsOnCourse(7),Is.False);
            Assert.That(f.runner.Field.GetAthlete(7).Motion.DistanceNanometers,Is.Zero);
            Assert.That(f.runner.Field.GetAthlete(7).ScheduledStart.Value,Is.EqualTo(43));
            Assert.That(f.runner.Field.GetAthlete(7).Lifecycle.LastFrame.ElapsedSeconds,Is.EqualTo((120-43)/20d).Within(1e-10));
            var observed=RacePresentationSnapshot.Capture(f.runner.Field).Get(7);
            Assert.That(observed.StartReleaseHeld,Is.True);Assert.That(observed.OnCourse,Is.False);
            var text=UiTextCatalog.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/front_end_text_v1.json"));
            Assert.That(new GameplayHudModel(observed,text,"ru").Phase,Is.EqualTo("Ожидаем свободную стартовую дорожку"));
            Assert.That(new GameplayHudModel(observed,text,"en").Phase,Is.EqualTo("Waiting for a clear start lane"));
            for(int i=0;i<240;i++)Player(f.runner,1000);
            Assert.That(f.stepper.World.IsStartReleaseHeld(7),Is.False);
            Assert.That(f.stepper.World.GetStartReleaseTick(7),Is.GreaterThan(120).And.LessThan(360));
            Assert.That(f.runner.Field.GetAthlete(7).Motion.DistanceNanometers,Is.GreaterThan(SkiState.NanometersPerMeter));
            Assert.That(f.stepper.World.GetStartReleaseWaitTicks(7),Is.EqualTo(f.stepper.World.GetStartReleaseTick(7)-44));
            int held=0,released=0;for(int i=0;i<f.events.Count;i++)
            {var e=f.events[i];if(!e.AthleteId.Equals(f.runner.Field.GetAthlete(7).AthleteId))continue;if(e.EventType==RaceEventType.StartReleaseHeld)held++;if(e.EventType==RaceEventType.StartReleased){released++;Assert.That(e.Payload,Is.GreaterThan(0));}}
            Assert.That((held,released),Is.EqualTo((1,1)));
        }
        [TestCase(false)][TestCase(true)] public void LegacyReleaseFailureIsIndependentOfContactQueryReuse(bool disableCache)
        {
            var f=Create(2);var cache=typeof(ManualScopedWorldRoster).GetField("ownedContactCacheValid",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            for(int i=0;i<43;i++){if(disableCache)cache.SetValue(f.stepper.World,false);Player(f.runner,0);}
            if(disableCache)cache.SetValue(f.stepper.World,false);
            Assert.Throws<InvalidOperationException>(()=>Player(f.runner,0));
            Assert.That(f.stepper.LastStep.WorldAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactGeometry));
            Assert.That(f.stepper.LastStep.Tick.Value,Is.EqualTo(44));
        }
        [Test] public void IntervalSprintWaitsForTheSameOccupiedStartLine()
        {
            var f=Create(3,"sprint",2);
            for(int i=0;i<700;i++)Player(f.runner,0);
            Assert.That(f.stepper.World.IsStartReleaseHeld(1),Is.True);
            Assert.That(f.runner.Field.GetAthlete(1).ScheduledStart.Value,Is.EqualTo(601));
            Assert.That(f.runner.Field.GetAthlete(1).Motion.DistanceNanometers,Is.Zero);
            for(int i=0;i<200;i++)Player(f.runner,1000);
            Assert.That(f.stepper.World.IsStartReleaseHeld(1),Is.False);
            Assert.That(f.runner.Field.GetAthlete(1).Motion.DistanceNanometers,Is.GreaterThan(SkiState.NanometersPerMeter));
        }
        [Test] public void StartReleasePreviewCannotEscapeAnAbortedWorldTick()
        {
            var f=Create();Player(f.runner,0);var world=f.stepper.World;
            Assert.That(world.GetStartReleaseTick(0),Is.EqualTo(-1));
            var tick=world.BeginTick();Assert.That(world.IsPhysicalParticipant(0,tick.Tick),Is.True);
            Assert.That(world.GetStartReleaseTick(0),Is.EqualTo(-1));world.AbortTick(tick);
            Assert.That(world.GetStartReleaseTick(0),Is.EqualTo(-1));Assert.That(world.Tick.Value,Is.EqualTo(1));
            Player(f.runner,0);Assert.That(world.GetStartReleaseTick(0),Is.EqualTo(2));
        }
        [Test] public void DelayedStartReleaseAndWaitEventsReexecuteFromRecordedCommands()
        {
            var source=Create();byte[] bytes;
            using(var output=new MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(output,source.stepper,SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json"))))
                {
                    source.stepper.CommandObserver=writer;
                    for(int i=0;i<360;i++)Player(source.runner,i<120?0:1000);
                    writer.Complete();Assert.That(writer.Failure,Is.Null);
                }
                bytes=output.ToArray();
            }
            var replay=Create();using(var input=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(input,replay.stepper,360))while(reader.HasNext)reader.Step();
            for(int i=0;i<8;i++)
            {
                Assert.That(replay.stepper.World.GetActor(i).Pose,Is.EqualTo(source.stepper.World.GetActor(i).Pose));
                Assert.That(replay.stepper.World.GetActor(i).Velocity,Is.EqualTo(source.stepper.World.GetActor(i).Velocity));
                Assert.That(replay.stepper.World.GetStartReleaseTick(i),Is.EqualTo(source.stepper.World.GetStartReleaseTick(i)));
                Assert.That(replay.runner.Field.GetAthlete(i).PhysiologyState,Is.EqualTo(source.runner.Field.GetAthlete(i).PhysiologyState));
            }
            var old=Create(2);using(var input=new MemoryStream(bytes))Assert.Catch(()=>new ManualRaceJournalReader(input,old.stepper,360),"The recorded new start-admission policy must not be replayed with the old solver context.");
        }
    }
}

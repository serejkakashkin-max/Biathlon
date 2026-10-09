using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualReusableRangeTests
    {
        internal static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) Initial(string format,int count)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_profile"));
            PursuitQualificationSnapshot qualification=null;
            if(format=="pursuit")
            {
                // Completed lifecycle contract rows supply a deliberate ten-second
                // starting gap. They are not claimed as a simulated Sprint race.
                var sprint=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+"sprint_profile"));
                var ids=new AthleteId[count];for(int i=0;i<count;i++)ids[i]=new AthleteId("manual_reuse_"+i);
                var book=new SprintResultBook(sprint,SprintResultsTests.Timing(),ids,20,new RaceEventLog());
                for(int i=0;i<count;i++)
                {
                    var life=new CompetitionLifecycle(sprint,ids[i],20,new RaceEventLog());
                    life.Step(new SimulationTick(1),CompetitionAction.Start);
                    life.Step(new SimulationTick(2),CompetitionAction.EnterRange);
                    life.Step(new SimulationTick(3),CompetitionAction.CompleteBout,31,5);
                    life.Step(new SimulationTick(4),CompetitionAction.ReachLapBoundary);
                    life.Step(new SimulationTick(5),CompetitionAction.EnterRange);
                    life.Step(new SimulationTick(6),CompetitionAction.CompleteBout,31,5);
                    life.Step(new SimulationTick(7),CompetitionAction.ReachLapBoundary);
                    long finish=201+i*200;
                    for(long t=8;t<finish;t++)life.Step(new SimulationTick(t));
                    life.Step(new SimulationTick(finish),CompetitionAction.Finish,finish:new FinishTime(finish,0,20));book.Record(life);
                }
                qualification=PursuitQualificationSnapshot.Create("manual_reuse_qualification",book.Complete("manual_reuse_source"),profile);
            }
            var events=new RaceEventLog(10000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),qualification:qualification,count:count);
            var models=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var world=ManualScopedFieldStepper.CreateInitialWorld(runner.Field,venue.Track,venue.Visual,venue.Occupancy,
                ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),ManualScopedFieldStepperTests.Group(),models.Physics,
                ManualFinishRunoutDefinition.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/manual_finish_runout_v1")));
            var stepper=new ManualScopedFieldStepper(runner.Field,world,models,venue.Occupancy,ManualScopedFieldStepperTests.Steering(),
                ManualScopedFieldStepperTests.Work(),ManualMatBrakingDefinition.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/manual_mat_braking_v1")));
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/manual_ai_steering_v2")),ManualScopedFieldStepperTests.Steering());
            return(runner,stepper,events);
        }

        [TestCase("sprint",1)][TestCase("sprint",3)][TestCase("pursuit",3)]
        public void RealManualFormatsAcquireAndReleaseEveryMatOnTheFieldClock(string format,int count)
        {
            var f=Initial(format,count);var field=f.runner.Field;var world=f.stepper.World;var book=field.RangeReservations;
            Assert.That(world.PathChoiceCount,Is.EqualTo(book.LaneCount));
            bool changed=false;var assignments=new int[count];var current=new ManualRacePathCatalog[count];
            for(int i=0;i<count;i++)current[i]=world.GetCatalog(i);
            while(!field.IsComplete&&field.Tick.Value<30000)
            {
                try{f.runner.Step();}
                catch(Exception e){Assert.Fail("tick="+field.Tick.Next().Value+"; "+ManualScopedFieldStepperTests.Explain(f.stepper)+"; "+e);}
                Assert.That(world.Tick,Is.EqualTo(field.Tick));Assert.That(book.Tick,Is.EqualTo(field.Tick));
                for(int i=0;i<count;i++)
                {
                    var a=field.GetAthlete(i);Assert.That(field.GetManualScopedWorldActor(i).Pose,Is.EqualTo(world.GetActor(i).Pose));
                    Assert.That(world.GetPhysicalMotion(i),Is.EqualTo(a.Motion));
                    if(!ReferenceEquals(current[i],world.GetCatalog(i)))
                    {
                        changed=true;current[i]=world.GetCatalog(i);assignments[i]++;
                        Assert.That(current[i].Get(world.GetCursor(i).OccurrenceIndex).Kind,Is.EqualTo(ManualRacePathKind.RangeApproach));
                        Assert.That(book.GetOwnedLane(i),Is.EqualTo(a.RangePath.LaneOrdinal));
                    }
                }
            }
            Assert.That(field.IsComplete,Is.True);
            int acquired=0,released=0,assigned=0;
            for(int e=0;e<f.events.Count;e++)
            {
                var type=f.events[e].EventType;
                if(type==RaceEventType.PhysicalRangeReservationAcquired)acquired++;
                if(type==RaceEventType.PhysicalRangeReservationReleased)released++;
                if(type==RaceEventType.PhysicalRangePathAssigned)assigned++;
            }
            int expected=count*book.BoutCount;
            Assert.That(acquired,Is.EqualTo(expected));Assert.That(released,Is.EqualTo(expected));Assert.That(assigned,Is.EqualTo(expected));
            for(int i=0;i<count;i++)
            {
                Assert.That(book.GetOwnedLane(i),Is.Zero);
                for(int b=0;b<book.BoutCount;b++)
                {
                    var record=book.GetRecord(i,b);Assert.That(record.IsValid&&record.IsReleased,Is.True);
                    Assert.That(record.Arrival.Tick.Value,Is.LessThan(record.Release.Tick.Value));
                    Assert.That(record.Release.Fraction,Is.EqualTo(JunctionAdmissionSession.FractionParts));
                }
            }
            if(format=="pursuit"&&count>1)Assert.That(changed,Is.True,"Overlapping arrivals must exercise selected catalogs beyond lane01.");
            TestContext.WriteLine(format+" actual complete actors="+count+" tick="+field.Tick.Value+" changed="+changed+" acquisitions="+acquired+" releases="+released);
        }

        [Test] public void ReusableRaceAndSelectedPathEventsRepeatExactly()
        {
            var a=Initial("pursuit",3);var b=Initial("pursuit",3);
            while(!a.runner.Field.IsComplete&&a.runner.Field.Tick.Value<30000){a.runner.Step();b.runner.Step();}
            Assert.That(a.runner.Field.IsComplete&&b.runner.Field.IsComplete,Is.True);
            for(int i=0;i<3;i++)
            {
                Assert.That(a.stepper.World.GetActor(i).Pose,Is.EqualTo(b.stepper.World.GetActor(i).Pose));
                Assert.That(a.stepper.World.GetActor(i).Velocity,Is.EqualTo(b.stepper.World.GetActor(i).Velocity));
                Assert.That(a.runner.Field.GetAthlete(i).PhysiologyState,Is.EqualTo(b.runner.Field.GetAthlete(i).PhysiologyState));
                Assert.That(a.runner.Field.GetAthlete(i).Lifecycle.GlobalFinishTime,Is.EqualTo(b.runner.Field.GetAthlete(i).Lifecycle.GlobalFinishTime));
                for(int bout=0;bout<4;bout++)Assert.That(a.runner.Field.RangeReservations.GetRecord(i,bout),Is.EqualTo(b.runner.Field.RangeReservations.GetRecord(i,bout)));
            }
            Assert.That(a.events.Count,Is.EqualTo(b.events.Count));
            for(int e=0;e<a.events.Count;e++)Assert.That(a.events[e],Is.EqualTo(b.events[e]));
        }

        [Test] public void ForeignStaleAndPrematurePathChoicesCannotMoveOrReserveTheField()
        {
            var f=Initial("sprint",1);var world=f.stepper.World;var actor=world.GetActor(0);var cursor=world.GetCursor(0);
            var hold=new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,1).PreviewTrajectory(
                new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75),new SkiContactMotion(actor.Velocity,0),0,0,.05);
            var candidates=new[]{new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(hold))};
            var windows=new[]{new ManualScopedWorldWindow(actor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,0,world.GetCatalog(0).Get(cursor.OccurrenceIndex).LengthNanometers)};
            Assert.That(world.TryPrepare(candidates,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{0d},out var proof),Is.True);
            var other=Initial("sprint",1);
            Action<ManualScopedWorldPreview,int,int,int> Choice(ManualScopedWorldRoster target)
                =>(Action<ManualScopedWorldPreview,int,int,int>)typeof(ManualScopedWorldRoster).GetMethod("StagePaidPathChoice",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .CreateDelegate(typeof(Action<ManualScopedWorldPreview,int,int,int>),target);
            var stage=Choice(world);
            Assert.Throws<ArgumentException>(()=>Choice(other.stepper.World)(proof,0,1,0));
            Assert.Throws<ArgumentException>(()=>stage(proof,0,1,0),"The initial base occurrence has no paid range arrival.");
            world.DiscardPrepared(proof);
            Assert.Throws<ArgumentException>(()=>stage(proof,0,1,0));
            Assert.That(world.Tick.Value,Is.Zero);Assert.That(world.GetActor(0).Pose,Is.EqualTo(actor.Pose));
            Assert.That(world.GetCursor(0),Is.EqualTo(cursor));Assert.That(f.runner.Field.RangeReservations.Tick.Value,Is.Zero);
            Assert.That(f.runner.Field.RangeReservations.GetOwnedLane(0),Is.Zero);Assert.That(f.events.Count,Is.Zero);
        }

        [Test] public void InvalidLastRowCannotPartiallyPublishAnyReusableResourceOrBody()
        {
            var f=Initial("sprint",3);var field=f.runner.Field;var world=f.stepper.World;
            var inputs=new SprintAthleteInput[3];var steering=new int[3];
            for(int i=0;i<3;i++)
            {
                var a=field.GetAthlete(i);var tick=field.Tick.Next();
                inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,500),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0));
            }
            steering[2]=ManualSkiCommand.SteeringUnits+1;
            var before=world.GetActor(0);string context=field.ContextJson;int events=f.events.Count;
            Assert.Throws<ArgumentException>(()=>f.stepper.TryStep(inputs,steering));
            Assert.That(field.Tick.Value,Is.Zero);Assert.That(world.Tick.Value,Is.Zero);Assert.That(field.RangeReservations.Tick.Value,Is.Zero);
            Assert.That(world.GetActor(0).Pose,Is.EqualTo(before.Pose));Assert.That(field.ContextJson,Is.EqualTo(context));
            Assert.That(f.events.Count,Is.EqualTo(events));for(int i=0;i<3;i++)Assert.That(field.RangeReservations.GetOwnedLane(i),Is.Zero);
        }

        [Test] public void WarmReusableFieldClockAndForcePresenceHaveNoManagedTickAllocation()
        {
            var f=Initial("sprint",3);
            for(int i=0;i<64;i++)f.runner.Step();
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<128;i++)f.runner.Step();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
            Assert.That(f.runner.Field.RangeReservations.Tick,Is.EqualTo(f.runner.Field.Tick));
            Assert.That(f.runner.Field.GetAthlete(2).Motion.DistanceNanometers,Is.Zero,"An unstarted roster row cannot receive hidden contact-force movement.");
        }
    }
}

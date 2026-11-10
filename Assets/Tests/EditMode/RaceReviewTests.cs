using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Replay;
using Biathlon.AI;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class RaceReviewTests
    {
        [Test] public void SplitTableUsesSameLineAndElapsedClockForIntervalStarters()
        {
            var layout=CheckpointTimingTests.Layout();
            CheckpointSplit Split(string id,long start,long travel)
            {var t=new CheckpointTracker(layout,new AthleteId(id),20,new SimulationTick(start),new RaceEventLog(20));t.Observe(new SimulationTick(start+1),0,travel);return t.Get(1);}
            var early=Split("early",0,1000000000000);var fast=Split("fast",600,1600000000000);var tie=Split("tie",1200,1600000000000);
            var elapsed=new CheckpointStandings(new[]{early,fast,tie,default},CheckpointClock.Elapsed);
            Assert.That(elapsed.Count,Is.EqualTo(3));Assert.That(elapsed.Get(0).AthleteId,Is.EqualTo("fast"));
            Assert.That(elapsed.Get(1).Rank,Is.EqualTo(1));Assert.That(elapsed.Get(2).Rank,Is.EqualTo(3));Assert.That(elapsed.Get(2).GapSeconds,Is.GreaterThan(0));
            var global=new CheckpointStandings(new[]{fast,early,tie},CheckpointClock.Global);Assert.That(global.Get(0).AthleteId,Is.EqualTo("early"));
            var other=new CheckpointTracker(layout,new AthleteId("other"),20,default,new RaceEventLog(20));other.Observe(new SimulationTick(1),0,1600000000000);
            Assert.Throws<ArgumentException>(()=>new CheckpointStandings(new[]{early,other.Get(2)},CheckpointClock.Elapsed));
        }
        [Test]
        public void BroadcastPagesCoverNineteenResultsAndKeepThreeVisibleUntilPlayerFinishesTwentieth()
        {
            var layout = CheckpointTimingTests.Layout();
            long line = layout.Get(1).RouteNanometers;
            var arrivals = new CheckpointSplit[20];
            for (int i = 0; i < arrivals.Length; i++)
            {
                var tracker = new CheckpointTracker(layout, new AthleteId($"bib_{i + 1:00}"), 20, default, new RaceEventLog(20));
                tracker.Observe(new SimulationTick(1), 0, line * (40 - i));
                arrivals[i] = tracker.Get(1);
            }

            var approach = new CheckpointStandings(arrivals.AsSpan(0, 19), CheckpointClock.Global);
            int[] expectedStarts = { 0, 3, 6, 9, 12, 15, 16 };
            var displayedRanks = new System.Collections.Generic.HashSet<int>();
            for (int page = 0; page < expectedStarts.Length; page++)
            {
                int start = approach.BroadcastPageStart(page, 0);
                Assert.That(start, Is.EqualTo(expectedStarts[page]));
                for (int i = 0; i < CheckpointStandings.BroadcastSlots; i++)
                    displayedRanks.Add(approach.Get(start + i).Rank);
            }

            Assert.That(displayedRanks.Count, Is.EqualTo(19));
            Assert.That(approach.BroadcastPageStart(7, 0), Is.Zero);
            Assert.That(approach.BroadcastPageStart(0, .9), Is.EqualTo(16));

            var crossed = new CheckpointStandings(arrivals, CheckpointClock.Global, arrivals[19].GlobalTime);
            int lastGroup = crossed.BroadcastResultStart(19);
            Assert.That(new[] { crossed.Get(lastGroup).Rank, crossed.Get(lastGroup + 1).Rank, crossed.Get(lastGroup + 2).Rank },
                Is.EqualTo(new[] { 18, 19, 20 }));
            Assert.That(crossed.Get(lastGroup + 2).AthleteId, Is.EqualTo("bib_20"));
            Assert.That(crossed.BroadcastResultStart(0), Is.Zero);
            Assert.That(crossed.BroadcastResultStart(9), Is.EqualTo(8));
        }

        [Test]
        public void CrossingCardKeepsRankWhenAFasterSprintAthleteArrivesLaterInTheSameTick()
        {
            var layout = CheckpointTimingTests.Layout();
            long line = layout.Get(1).RouteNanometers;
            CheckpointSplit Arrive(string id, long start, long travel)
            {
                var tracker = new CheckpointTracker(layout, new AthleteId(id), 20, new SimulationTick(start), new RaceEventLog(20));
                for (long tick = start + 1; tick < 101; tick++)
                    tracker.Observe(new SimulationTick(tick), 0, 0);
                tracker.Observe(new SimulationTick(101), 0, travel);
                return tracker.Get(1);
            }

            var player = Arrive("player", 10, line * 2);
            var earlier = Arrive("earlier", 15, line * 4);
            var later = Arrive("later", 20, line * 4 / 3);
            Assert.That(later.GlobalTime.WholeTicks, Is.EqualTo(player.GlobalTime.WholeTicks));
            Assert.That(later.GlobalTime.CompareTo(player.GlobalTime), Is.GreaterThan(0));
            var splits = new[] { later, earlier, player };
            var live = new CheckpointStandings(splits, CheckpointClock.Elapsed);
            var atCrossing = new CheckpointStandings(splits, CheckpointClock.Elapsed, player.GlobalTime);
            Assert.That(live.Get(0).AthleteId, Is.EqualTo("later"));
            Assert.That(live.Get(2).AthleteId, Is.EqualTo("player"));
            Assert.That(atCrossing.Count, Is.EqualTo(2));
            Assert.That(atCrossing.Get(1).AthleteId, Is.EqualTo("player"));
            Assert.That(atCrossing.Get(1).Rank, Is.EqualTo(2));
            Assert.That(atCrossing.Get(1).Seconds, Is.EqualTo(player.ElapsedTime.Seconds));
        }
        [Test] public void CalmAndHighPulseGuidanceFollowsActualBreathingFormula()
        {
            var p=PhysiologyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("physiology_gameplay_v2.json"));
            var b=BreathingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("breathing_gameplay_v2.json"));
            var calm=new ShootingReadiness(130,0,p,b,.65,.85);var high=new ShootingReadiness(180,.2,p,b,.65,.85);
            Assert.That(calm.Zone,Is.Zero);Assert.That(high.Zone,Is.EqualTo(2));Assert.That(high.BreathingAmplitudeRatio,Is.GreaterThan(calm.BreathingAmplitudeRatio));
            var actual=new BreathingSystem(b,p,20).Step(new SimulationTick(1),180,.2,0);
            Assert.That(high.BreathingAmplitudeRatio*b.BaseAmplitude,Is.EqualTo(actual.AmplitudeRadians).Within(1e-9));
        }
        [Test] public void BrakeIsExplicitRecordedIntentAndReleasingItDoesNotResetHeldPace()
        {
            var d=DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_gameplay_v3.json"));
            var device=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            device.Sample(new DeviceControlSample(false,1,1));var turn=device.BuildManual(new SimulationTick(1));Assert.That(turn.BrakeFraction,Is.Zero);
            device.Sample(new DeviceControlSample(false,0,-1,brake:true));var brake=device.BuildManual(new SimulationTick(2));
            Assert.That(brake.SteeringInput,Is.EqualTo(-500));Assert.That(brake.BrakeFraction,Is.EqualTo(1));
            Assert.That(ManualSkiCommand.FromJson(brake.ToJson()).BrakeFraction,Is.EqualTo(1));
            device.Sample(new DeviceControlSample(false,0,0));var release=device.BuildManual(new SimulationTick(3));Assert.That(release.BrakeFraction,Is.Zero);Assert.That(release.AnalogEffort,Is.EqualTo(turn.AnalogEffort));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ManualSkiCommand(new SimulationTick(1),new AthleteId("a"),0,0,brakeFraction:1.01));
        }
        [Test] public void LowSpeedRecoveryTurnsNinetyDegreesWithinOneSecondWithoutTranslation()
        {
            var steering=new ManualSkiSteeringSystem(ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),20);
            var pose=new ManualSkiHeadingState(8,12,0);
            for(int i=0;i<20;i++)pose=steering.Preview(pose,1000,0,0,4).State;
            Assert.That(pose.HeadingRadians,Is.GreaterThan(Math.PI*.5));Assert.That(pose.X,Is.EqualTo(8));Assert.That(pose.Z,Is.EqualTo(12));
        }
        [Test] public void PhysicalPivotPaysForAngularWorkInsteadOfSnappingHeadingAtRest()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var pose=f.At(1,10*SkiState.NanometersPerMeter);
            Assert.That(f.Catalog.TryProject(pose,1,10*SkiState.NanometersPerMeter,0,out var observed),Is.True);
            var system=new ManualSkiControlledDriveSystem(SkiForceTests.Definition(),f.Venue.Occupancy,
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),CorneringTests.Definition(),20);
            var body=new ManualSkiContactBody(new AthleteId("pivot"),pose,new SkiPlanarVelocity(0,0),SkiForceTests.Definition().MassKilograms);
            var powered=system.Preview(body,new SkiContactMotion(new SkiPlanarVelocity(0,0),0),observed,f.Catalog.SampleSurface(observed),new ManualSkiMotionInput(1000,350,100,100),.05);
            var coast=system.Preview(body,new SkiContactMotion(new SkiPlanarVelocity(0,0),0),observed,f.Catalog.SampleSurface(observed),new ManualSkiMotionInput(1000,0,100,100),.05);
            Assert.That(powered.ActivePivotWorkJoules,Is.InRange(double.Epsilon,350*.05));Assert.That(powered.Drive.MotionAt(0).YawRate,Is.Zero);
            Assert.That(powered.Drive.MotionAt(1).YawRate,Is.GreaterThan(0));Assert.That(coast.ActivePivotWorkJoules,Is.Zero);Assert.That(coast.Drive.MotionAt(1).YawRate,Is.Zero);
        }
        [Test,Timeout(120000)] public void RemainingFieldSimulationKeepsEveryTickAndIdenticalResult()
        {
            var a=AiTelemetryTests.Create(3,new RaceEventLog(50000),new AiDecisionCounts());var b=AiTelemetryTests.Create(3,new RaceEventLog(50000),new AiDecisionCounts());
            Assert.Throws<InvalidOperationException>(()=>b.StepRemainingField(0));
            int ticks=0;while(!a.Field.IsComplete&&ticks++<100000)
            {a.Step();if(b.Field.GetAthlete(0).HasFinished)b.StepRemainingField(0);else b.Step();}
            Assert.That(a.Field.IsComplete&&b.Field.IsComplete,Is.True);Assert.That(a.Field.Tick,Is.EqualTo(b.Field.Tick));Assert.That(a.Field.Result.ToJson(),Is.EqualTo(b.Field.Result.ToJson()));
        }
    }
}

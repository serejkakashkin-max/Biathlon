using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class OwnerShootingControlsTests
    {
        [Test] public void ManualBoltIsRequiredBeforeEveryShotAndNeverAfterFifth()
        {
            var s=new ShootingStateMachine(ShootingStateTests.Definition(),new AthleteId("owner"),"range",20,new RaceEventLog(100),manualBoltCycle:true);
            long tick=1;s.Step(new SimulationTick(tick++),ShootingAction.EnterRange);
            s.Step(new SimulationTick(tick++),ShootingAction.AssignLane,new ShootingLaneAssignment("range","lane_1",1));
            while(s.Phase==ShootingPhase.Setup)s.Step(new SimulationTick(tick++));
            for(int shot=0;shot<5;shot++)
            {
                Assert.That(s.NeedsBoltCycle,Is.True);
                Assert.That(s.Step(new SimulationTick(tick++),ShootingAction.Trigger).ShotAccepted,Is.False);
                s.Step(new SimulationTick(tick++),ShootingAction.CycleBolt);
                Assert.That(s.Phase,Is.EqualTo(ShootingPhase.BoltCycle));
                while(s.Phase==ShootingPhase.BoltCycle)s.Step(new SimulationTick(tick++));
                Assert.That(s.Step(new SimulationTick(tick++),ShootingAction.Trigger).ShotAccepted,Is.True);
            }
            Assert.That(s.ShotsFired,Is.EqualTo(5));Assert.That(s.Phase,Is.EqualTo(ShootingPhase.Exiting));Assert.That(s.NeedsBoltCycle,Is.False);
        }
        [Test] public void OrdinaryKeyboardPaceLeavesRoomForAttackAndRecovery()
        {
            var b=new DeviceCommandBuilder(DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_gameplay_v2.json")),new AthleteId("owner"),20);
            b.Sample(new DeviceControlSample(false,1,0));PlayerCommand c=default;
            for(int t=1;t<=80;t++)c=b.Build(new SimulationTick(t));
            Assert.That(c.AnalogEffort,Is.EqualTo(800));
            b.Sample(new DeviceControlSample(false,1,0,EffortPreset.Attack));
            Assert.That(b.Build(new SimulationTick(81)).Preset,Is.EqualTo(EffortPreset.Attack));
            var strong=PhysiologyIntegrationTests.Create(new RaceEventLog(100));var steady=PhysiologyIntegrationTests.Create(new RaceEventLog(100));
            for(int t=0;t<1000;t++){strong.Step(PhysiologyIntegrationTests.Input(strong,1000));steady.Step(PhysiologyIntegrationTests.Input(steady,800));}
            Assert.That(strong.State.AnaerobicReserve,Is.LessThan(steady.State.AnaerobicReserve));
            Assert.That(strong.State.MuscularFatigue,Is.GreaterThan(steady.State.MuscularFatigue));
        }
        [Test] public void RequestedTuckPersistsOnFlatAndUphillAndStillCostsPropulsion()
        {
            var d=TuckDefinition.FromJson(PhysiologyContractTests.DefinitionJson("tuck_gameplay_v2.json"));var s=new TuckSystem(d);
            foreach(double grade in new[]{-.1,0,.1})
            {
                var held=s.Evaluate(true,1000,50,new SkiState(0,5),TuckTests.Descent(grade:grade));
                Assert.That(held.Active,Is.True);Assert.That(held.EffectiveEffortControl,Is.EqualTo(200));
                Assert.That(s.Evaluate(false,1000,50,new SkiState(0,5),TuckTests.Descent(grade:grade)).Active,Is.False);
            }
        }
        [Test] public void ReloadCommandsPersistAndReplayWithTheirSelectedPolicy()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(200),1);var b=SprintFieldTests.Create(new RaceEventLog(200),1);
            var tape=new SprintRaceCommandTape("owner_bolt",a,10);var commands=new SprintAthleteInput[1];
            for(int t=1;t<=4;t++)
            {
                var tick=new SimulationTick(t);var id=a.GetAthlete(0).AthleteId;
                commands[0]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,0),1,1,0),
                    new WeaponAimCommand(tick,id,0,0),cycleBolt:t%2==0,manualBoltCycle:true);
                tape.StepAndRecord(a,commands);
            }
            string json=tape.ToJson();Assert.That(json,Does.Contain("\"cycle_bolt\":true"));Assert.That(json,Does.Contain("\"manual_bolt\":true"));
            var replay=SprintRaceCommandTape.FromJson(json).CreateReplay(b);while(replay.HasNext)replay.Step();
            Assert.That(b.GetAthlete(0).LastFrame,Is.EqualTo(a.GetAthlete(0).LastFrame));
        }
    }
}

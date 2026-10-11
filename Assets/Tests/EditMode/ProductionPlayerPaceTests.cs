using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ProductionPlayerPaceTests
    {
        // Navigation is held equal by the existing AI steering seam so this
        // diagnostic measures device effort/physiology, not human corner skill.
        [Test, Explicit("Bounded first-range production pace diagnostic.")]
        public void HeldWFirstRangePace()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            var player=f.runner.Field.GetAthlete(0);
            var device=new DeviceCommandBuilder(DeviceControlDefinition.FromJson(
                PhysiologyContractTests.DefinitionJson("device_control_gameplay_v2.json")),player.AthleteId,20);
            var effort=new EffortControlSystem(EffortControlDefinition.FromJson(
                PhysiologyContractTests.DefinitionJson("effort_control_provisional_v1.json")),player.AthleteId,20);
            long[] arrived=new long[30];double[] totalEffort=new double[30];
            for(int t=0;t<6000&&arrived[0]==0;t++)
            {
                var tick=f.runner.Field.Tick.Next();
                device.Sample(new DeviceControlSample(false,1,0));
                var command=device.BuildManual(tick);var control=effort.Step(command.ToEffortCommand(),true);
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,player.AthleteId,
                    control.AppliedControl),1,1,0),new WeaponAimCommand(tick,player.AthleteId,0,0));
                f.runner.StepWithManualPlayer(0,input,0,true);
                for(int a=0;a<30;a++)
                {
                    totalEffort[a]+=f.runner.GetLastInput(a).Ski.Effort.RequestedEffort;
                    if(arrived[a]==0&&f.runner.Field.GetAthlete(a).ActiveShooting!=null)arrived[a]=tick.Value;
                }
                if(t%400==399)
                    TestContext.WriteLine($"tick={tick.Value} player_m={player.RouteNanometers/1e9:F1} v={player.Motion.SpeedMetersPerSecond:F2} effort={control.AppliedControl} reserve={player.PhysiologyState.AnaerobicReserve:F3} fatigue={player.PhysiologyState.MuscularFatigue:F3}");
            }
            int ahead=0;
            for(int a=0;a<30;a++)
            {
                if(a>0&&arrived[a]>0&&arrived[a]<arrived[0])ahead++;
                var actor=f.runner.Field.GetAthlete(a);
                TestContext.WriteLine($"actor={a} first_range_tick={arrived[a]} mean_effort={totalEffort[a]/f.runner.Field.Tick.Value:F1} reserve={actor.PhysiologyState.AnaerobicReserve:F3} fatigue={actor.PhysiologyState.MuscularFatigue:F3} route_m={actor.RouteNanometers/1e9:F1}");
            }
            TestContext.WriteLine($"held_W_first_range_place={ahead+1}/30 tick={arrived[0]}");
            Assert.That(arrived[0],Is.GreaterThan(0));
            Assert.That(ahead+1,Is.LessThan(30),"Equal-navigation held W must not inevitably place the player last.");
        }
    }
}

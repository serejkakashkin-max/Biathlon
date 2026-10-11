using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public sealed class PlayerRangeOwnerFeedbackTests
    {
        [Test] public void ShippedWoodlandDepartureCorridorsHaveAllAuthoredKnotWidths()
        {
            var environment=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
            foreach(var corridor in environment.route_corridors)
                Assert.That(corridor.widths_m.Length,Is.EqualTo(corridor.knots.Length),corridor.loop_id);
        }
        [Test] public void CatchUpFrameKeepsAdjacentSnapshotsForSmoothRendering()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var go=new GameObject("Frame pacing regression");
            try
            {
                var host=go.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),1);
                for(int i=0;i<20;i++)host.AdvanceSimulationFrame(.05);
                long before=host.Feed.Current.Tick;host.AdvanceSimulationFrame(.175);
                Assert.That(host.Feed.Current.Tick-before,Is.GreaterThan(1),"Exercise a real multi-tick frame.");
                Assert.That(host.Feed.Current.Tick-host.Feed.Previous.Tick,Is.EqualTo(1),"Skipped snapshots disable world-pose interpolation after a slow frame.");
                Assert.That(host.Feed.InterpolationAlpha,Is.EqualTo(.5).Within(.00001));
                host.AdvanceSimulationFrame(.01);
                Assert.That(host.Feed.InterpolationAlpha,Is.EqualTo(.7).Within(.00001),"Keep the scheduler's fractional remainder.");
            }
            finally{Object.DestroyImmediate(go);}
        }
        [UnityTest] public IEnumerator RejectedRaceTickReportsOnceAndDoesNotAdvanceDeviceOrEffortClocks()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(root+"manifest")),ProductionVenueHost.ReadResource);
            var go=new GameObject("Rejected race tick test");
            try
            {
                var host=go.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(root+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(root+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),1);
                var id=host.Runner.Field.GetAthlete(0).AthleteId;
                var command=new ManualSkiCommand(new SimulationTick(1),id,650,0);
                Assert.Throws<System.ArgumentException>(()=>host.Step(command,
                    new ShootingDeviceCommand(new WeaponAimCommand(new SimulationTick(2),id,0,0),false,false)));
                Assert.That(host.Runner.Field.Tick.Value,Is.Zero);
                // Recreate the exact post-rejection runner state. The owner
                // log supplies its original mat-arrival failure separately.
                typeof(Biathlon.AI.PackAiRaceRunner).GetProperty("IsFaulted").SetValue(host.Runner,true);
                LogAssert.Expect(LogType.Exception,new System.Text.RegularExpressions.Regex("InvalidOperationException: Faulted runner must be reconstructed"));
                host.AdvanceSimulationFrame(.05);
                Assert.That(host.HasSimulationFault,Is.True);
                for(int i=0;i<600;i++)host.AdvanceSimulationFrame(.05);
                Assert.That(host.Runner.Field.Tick.Value,Is.Zero);
                var device=go.GetComponent<PlayerDeviceInput>();
                Assert.That(device.PeekManual(new SimulationTick(1)).Tick.Value,Is.EqualTo(1));
                var effort=(EffortControlSystem)typeof(ProductionVenueHost).GetField("effort",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(host);
                Assert.DoesNotThrow(()=>effort.Preview(command.ToEffortCommand(),true));
                yield return null;
            }
            finally{Object.Destroy(go);}
        }
        [UnityTest] public IEnumerator ActualArrowCaptureMakesTheShotAndExitTurnThenReturnsPlayerControl()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(root+"manifest")),ProductionVenueHost.ReadResource);
            var go=new GameObject("Owner range correction test");
            try
            {
                var host=go.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(root+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(root+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),1);
                var field=host.Runner.Field;var id=field.GetAthlete(0).AthleteId;
                var effort=(EffortControlSystem)typeof(ProductionVenueHost).GetField("effort",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(host);
                // Paid shared AI intent is only a test driver up to the actual
                // assigned mat fork; no pose/phase teleport or fabricated arrival.
                bool capture=false,departure=false,returned=false;long firstDeparture=0;
                for(int i=0;i<10000&&!returned;i++)
                {
                    var actor=field.GetAthlete(0);var tick=field.Tick.Next();
                    var aim=new WeaponAimCommand(tick,id,1000,1000);
                    bool fire=actor.ActiveShooting?.Phase==ShootingPhase.Aiming;
                    var cursor=host.ManualStepper.World.GetCursor(0);var row=host.ManualStepper.World.GetCatalog(0).Get(cursor.OccurrenceIndex);
                    bool atMat=row.End==ManualRacePathEnd.FiringLine;
                    if(!capture&&!atMat)
                    {
                        effort.Step(new PlayerCommand(tick,id,650,0),true);
                        host.Runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,650),1,1,0),aim,fire),0,true);
                    }
                    else
                    {
                        host.Step(new ManualSkiCommand(tick,id,650,0),new ShootingDeviceCommand(aim,fire,false,actor.ActiveShooting?.NeedsBoltCycle==true));
                        capture|=host.LastRangeAssistance.Mode==PlayerRangeAssistMode.FinalApproach;
                        departure|=host.LastRangeAssistance.Mode==PlayerRangeAssistMode.InitialDeparture;
                        if(departure&&firstDeparture==0)firstDeparture=tick.Value;
                        returned=departure&&!host.LastRangeAssistance.Active;
                    }
                    if(i%200==0)yield return null;
                }
                Assert.That(capture,Is.True,"Physical contact with the final arrow trail must latch approach assistance.");
                Assert.That(field.GetAthlete(0).Lifecycle.CompletedBouts,Is.EqualTo(1));
                Assert.That(departure,Is.True);Assert.That(returned,Is.True);
                double automaticSeconds=(field.Tick.Value-firstDeparture)/(double)host.ManualStepper.World.TickFrequency;
                TestContext.WriteLine($"automatic_departure_seconds={automaticSeconds:R}");
                Assert.That(automaticSeconds,Is.LessThan(15),"Bound the owned departure rather than orbiting beyond the mat.");
                var returnedBody=host.ManualStepper.World.GetActor(0);
                var returnedRow=host.ManualStepper.World.GetCatalog(0).Get(returnedBody.Observation.OccurrenceIndex);
                if(returnedRow.LoopId==field.GetAthlete(0).RangePath.MatAnchor.LoopId)
                {
                    host.ManualStepper.World.GetCatalog(0).TrySample(returnedRow.Index,returnedRow.LengthNanometers,out var exit);
                    double cosine=System.Math.Sin(returnedBody.Pose.HeadingRadians)*exit.ForwardX+System.Math.Cos(returnedBody.Pose.HeadingRadians)*exit.ForwardZ;
                    Assert.That(cosine,Is.GreaterThan(System.Math.Cos(15*System.Math.PI/180)),"Hand back controls toward the exit aisle.");
                }
                Assert.That(host.Runner.GetLastInput(0).Ski.Effort.RequestedEffort,Is.EqualTo(effort.AnalogTarget(650)),"Returned controls use the normal effort adapter.");
                host.Proxies.Render(1);yield return null;
            }
            finally{Object.Destroy(go);}
        }
    }
}

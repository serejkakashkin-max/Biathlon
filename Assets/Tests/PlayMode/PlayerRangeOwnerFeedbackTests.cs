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
                bool capture=false,departure=false,returned=false;
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
                        host.Step(new ManualSkiCommand(tick,id,650,0),new ShootingDeviceCommand(aim,fire,false));
                        capture|=host.LastRangeAssistance.Mode==PlayerRangeAssistMode.FinalApproach;
                        departure|=host.LastRangeAssistance.Mode==PlayerRangeAssistMode.InitialDeparture;
                        returned=departure&&!host.LastRangeAssistance.Active;
                    }
                    if(i%200==0)yield return null;
                }
                Assert.That(capture,Is.True,"Physical contact with the final arrow trail must latch approach assistance.");
                Assert.That(field.GetAthlete(0).Lifecycle.CompletedBouts,Is.EqualTo(1));
                Assert.That(departure,Is.True);Assert.That(returned,Is.True);
                Assert.That(host.Runner.GetLastInput(0).Ski.Effort.RequestedEffort,Is.EqualTo(effort.AnalogTarget(650)),"Returned controls use the normal effort adapter.");
                host.Proxies.Render(1);yield return null;
            }
            finally{Object.Destroy(go);}
        }
    }
}

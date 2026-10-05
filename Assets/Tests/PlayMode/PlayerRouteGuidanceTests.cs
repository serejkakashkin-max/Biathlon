using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class PlayerRouteGuidanceTests
    {
        [UnityTest] public IEnumerator ActualAssignedMatGuideAndBoundedAidReturnManualIntentAfterDeparture()
        {
            var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("guidance_host_contract_route",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var root=new GameObject("Paid guidance and range aid test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.Initialize(venue,profile,route,conditions,count:1);
                var field=host.Runner.Field;var id=field.GetAthlete(0).AthleteId;
                bool arrows=false,approach=false,departure=false,returned=false,continuePenalty=false,leavePenalty=false;long lastDistance=0;
                for(int t=0;t<18000&&!leavePenalty;t++)
                {
                    var actor=field.GetAthlete(0);bool afterShot=actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.RangeExit;
                    var tick=field.Tick.Next();host.Step(new PlayerCommand(tick,id,afterShot&&!returned?0:800,0),
                        new ShootingDeviceCommand(new WeaponAimCommand(tick,id,1000,1000),actor.ActiveShooting?.Phase==ShootingPhase.Aiming,false));
                    host.Guidance.Refresh(.125f);
                    if(host.Guidance.GuidedLaneOrdinal>0)
                    {
                        arrows=true;Assert.That(host.Guidance.GuidedLaneOrdinal,Is.EqualTo(actor.AssignedShootingLane.Ordinal));
                        Assert.That(host.Guidance.MatArrowCount,Is.GreaterThan(0));
                    }
                    if(host.LastRangeAssistance.Mode==PlayerRangeAssistMode.FinalApproach)
                    {approach=true;Assert.That(host.Runner.GetLastLateral(0),Is.Zero);}
                    if(host.LastRangeAssistance.Mode==PlayerRangeAssistMode.InitialDeparture)
                    {departure=true;Assert.That(host.Runner.GetLastInput(0).Ski.Effort.RequestedEffort,Is.EqualTo(650));}
                    if(!returned&&departure&&!host.LastRangeAssistance.Active)
                    {
                        returned=true;Assert.That(host.Runner.GetLastInput(0).Ski.Effort.RequestedEffort,Is.LessThan(650));
                    }
                    if(actor.CurrentRangeMotion?.Phase==RangeTraversalPhase.PenaltyLoop&&host.Guidance.ForkArrowCount>0)
                    {
                        bool another=actor.CurrentRangeRemainingPenaltyLoops>1;
                        Assert.That(host.Guidance.GuidedNextLoopId,Is.EqualTo(another?venue.Track.Penalty.LoopId:
                            actor.RangePath.MapRoute(actor.RouteNanometers+2*SkiState.NanometersPerMeter).LoopId));
                        if(another)continuePenalty=true;else leavePenalty=true;
                    }
                    Assert.That(actor.Motion.DistanceNanometers,Is.GreaterThanOrEqualTo(lastDistance));lastDistance=actor.Motion.DistanceNanometers;
                }
                Assert.That(arrows,Is.True);Assert.That(approach,Is.True);Assert.That(departure,Is.True);Assert.That(returned,Is.True);
                Assert.That(continuePenalty,Is.True);Assert.That(leavePenalty,Is.True);
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}

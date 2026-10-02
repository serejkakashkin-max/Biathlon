using System;
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
    public class ProductionVenueHostTests
    {
        [UnityTest] public IEnumerator ExplicitPathPlayerHostCompletesAndUsesTheCurrentCorridorForInputExtent()
        {
            // Adapter contract fixture only; no production geometry or 30-mat acceptance claim.
            var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("production_host_contract_route",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var root=new GameObject("Production path host contract test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,profile,route,conditions,count:1);
                var body=venue.Occupancy;double wide=3-body.EdgeMargin;
                Assert.That(host.CurrentLateralExtent,Is.EqualTo(wide));Assert.That(host.Mats.Count,Is.EqualTo(2));
                Assert.That(host.Mats.LaneId(1),Is.EqualTo("zulu_mat"));
                Assert.That(host.Proxies.Count,Is.EqualTo(1));Assert.That(host.ChaseCamera,Is.Not.Null);
                var id=host.Runner.Field.GetAthlete(0).AthleteId;bool privateWidth=false;int aiming=0;
                while(!host.Runner.Field.IsComplete&&host.Runner.Field.Tick.Value<50000)
                {
                    var shooting=host.Runner.Field.GetAthlete(0).ActiveShooting;
                    bool trigger=shooting?.Phase==ShootingPhase.Aiming;
                    var tick=host.Runner.Field.Tick.Next();host.Step(new PlayerCommand(tick,id,800,0),
                        new ShootingDeviceCommand(new WeaponAimCommand(tick,id,1000,1000),trigger,false));
                    var actor=host.Runner.Field.GetAthlete(0);var point=host.Runner.Field.GetPhysicalPoint(0);
                    if(point.LoopId=="mat_1")
                    {
                        privateWidth=true;Assert.That(host.CurrentLateralExtent,Is.EqualTo(1-body.EdgeMargin));
                        Assert.That(host.Feed.Current.Get(0).LoopId,Is.EqualTo(point.LoopId));
                    }
                    if(actor.ActiveShooting?.Phase==ShootingPhase.Aiming)
                    {
                        host.Proxies.Render(1);aiming++;
                        Assert.That(Vector3.Angle(host.Proxies.GetProxy(id.Value).forward,new Vector3(.6f,0,.8f)),Is.LessThan(.03f));
                        Assert.That(actor.Motion.SpeedMetersPerSecond,Is.Zero);
                    }
                }
                Assert.That(privateWidth,Is.True);Assert.That(aiming,Is.GreaterThan(0));
                Assert.That(host.Runner.Field.IsComplete,Is.True);Assert.That(host.Feed.Current.Complete,Is.True);
                Assert.That(host.Runner.Field.GetAthlete(0).TotalPenaltyLoops,Is.EqualTo(10));
                Assert.Throws<InvalidOperationException>(()=>host.Initialize(venue,profile,route,conditions,count:1));
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator InvalidPlayerSlotRejectsBeforeAddingComponentsOrChildren()
        {
            var root=new GameObject("Production host rejection test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                Assert.Throws<ArgumentOutOfRangeException>(()=>host.Initialize(null,null,null,null,count:1,playerIndex:1));
                Assert.That(host.Runner,Is.Null);Assert.That(root.transform.childCount,Is.Zero);
                Assert.That(root.GetComponents<Component>().Length,Is.EqualTo(2));yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}

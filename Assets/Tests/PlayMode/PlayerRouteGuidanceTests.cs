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
        [UnityTest] public IEnumerator WoodlandMatTrailAppearsBeforeFinalApproachAndStartsAheadOfSkier()
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/sprint_profile"));
            var route=RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/sprint_route"));
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1"));
            var root=new GameObject("Early Woodland assigned-mat navigation test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.Initialize(venue,profile,route,conditions,count:1);
                var field=host.Runner.Field;var id=field.GetAthlete(0).AthleteId;bool found=false,pulse=false,curved=false;
                for(int t=0;t<8000;t++)
                {
                    var actor=field.GetAthlete(0);var tick=field.Tick.Next();
                    host.Step(new PlayerCommand(tick,id,900,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
                    host.Guidance.Refresh(.125f);
                    if(actor.ActiveShooting!=null)break;
                    double remaining=(actor.RangePath.FiringRouteNanometers(0)-actor.RouteNanometers)/(double)SkiState.NanometersPerMeter;
                    if(remaining>90)continue;
                    if(!found)Assert.That(remaining,Is.GreaterThan(30));
                    Assert.That(host.Guidance.GuidedLaneOrdinal,Is.EqualTo(actor.RangePath.LaneOrdinal));
                    Assert.That(host.Guidance.MatArrowCount,Is.GreaterThan(0),"Show the actual mat route before entering the final mat branch.");
                    var point=field.GetPhysicalPoint(0);
                    var sample=venue.Visual.Get(point.LoopId).Sample(point.LocalNanometers/(double)SkiState.NanometersPerMeter);
                    var here=new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z);float nearest=float.MaxValue;
                    var range=venue.Manifest.Range;var mat=range.MatDefinition;
                    var f=new Vector3((float)range.Forward.X,0,(float)range.Forward.Z);
                    var anchor=venue.MatPoint(actor.RangePath.LaneOrdinal-1);
                    var rear=new Vector3((float)anchor.X,(float)anchor.Y,(float)anchor.Z)+f*(float)(mat.AthleteStopDepth-mat.Length);
                    foreach(var arrow in host.Guidance.GetComponentsInChildren<MeshFilter>())
                        if(arrow.gameObject.name=="Assigned mat chevron")
                        {
                            nearest=Mathf.Min(nearest,Vector3.Distance(here,arrow.transform.position));
                            foreach(var vertex in arrow.sharedMesh.vertices)
                                Assert.That(Vector3.Dot(arrow.transform.TransformPoint(vertex)-rear,f),Is.LessThanOrEqualTo(-.24f),
                                    "Every visible chevron vertex must leave a snow gap behind the row of mats.");
                            if(remaining>50)Assert.That(Vector3.Dot(arrow.transform.position-rear,f),Is.LessThan(-2),"Before its final turn, the trail follows the open passage, not the mat edge.");
                            curved|=Vector3.Dot(arrow.transform.forward,f)>.2f&&Vector3.Dot(arrow.transform.forward,f)<.99f;
                        }
                    if(!found)Assert.That(nearest,Is.LessThan(6),"The separated trail still starts ahead of the skier, not at the distant mat.");
                    Assert.That(host.Guidance.ForkArrowCount,Is.Zero,"Do not overlay the old close-to-mat branch trail.");
                    Assert.That(host.Guidance.GetComponentsInChildren<TextMesh>(true),Is.Empty);
                    pulse|=host.Guidance.MatPulseActive;found=true;
                }
                Assert.That(found,Is.True);Assert.That(pulse,Is.True);Assert.That(curved,Is.True);yield return null;
            }
            finally{Object.Destroy(root);}
        }
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
                        Assert.That(host.Guidance.GetComponentsInChildren<TextMesh>(true),Is.Empty,"Owner removed the mat label.");
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
                Assert.That(arrows,Is.True);
                Assert.That(approach,Is.True);Assert.That(departure,Is.True);Assert.That(returned,Is.True);
                Assert.That(continuePenalty,Is.True);Assert.That(leavePenalty,Is.True);
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}

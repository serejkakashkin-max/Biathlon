using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class PhysicalRangeProxyDirectionTests
    {
        private static string Read(string name)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(name)).text;
        private static VenueAuthoringPlan Plan()
        {
            var resources=PathPresentationFixture.Resources();
            resources["manifest"]=resources["manifest"].Replace("{\"x\":0.6,\"y\":0,\"z\":0.8}","{\"x\":0,\"y\":0,\"z\":1}");
            // The real personal mat's incoming tangent faces -X; shooting faces +Z.
            resources["visual"]=resources["visual"].Replace("\"loop_id\":\"mat_1\",\"knots\":[{\"distance_m\":0,\"x\":-20","\"loop_id\":\"mat_1\",\"knots\":[{\"distance_m\":0,\"x\":20")
                .Replace("\"distance_m\":40,\"x\":20,\"y\":1,\"z\":5","\"distance_m\":40,\"x\":-20,\"y\":1,\"z\":5");
            return PathPresentationFixture.Plan(resources);
        }
        private static PackSprintFieldSession Field(VenueAuthoringPlan plan,RaceEventLog events)
        {
            var profile=CompetitionProfile.FromJson(Read("sprint_test_v1.json").Replace("\"distance_m\":3000","\"distance_m\":900"));var requirements=profile.ToRouteRequirements();
            var route=new RaceRoute(plan.Track,RaceRoutePlan.TimeTrial("proxy_path_sprint",plan.Track.Id,"main",requirements),requirements);
            var layout=new PhysicalRangeFieldLayout(new PhysicalTrackMap(plan.Track,route),plan.Track,route,plan.Paths);
            var ski=TimeTrialAthlete.FromJson(Read("time_trial_athlete_v1.json"));var shooting=ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            return new PackSprintFieldSession(new SimulationConfig(1,"proxy_path_sim",20,123),profile,new SkiModelDefinitions(Read),new ShootingModelDefinitions(Read),plan.Track,route,
                RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json").Replace("snow_test_packed","packed")),ResultTimingDefinition.FromJson(Read("result_timing_v1.json")),
                new[]{new SprintEntrant(ski,shooting,"ski_neutral")},new SimulationTick(1),"proxy_path_result",OccupancyDefinition.FromJson(Read("occupancy_provisional_v1.json")),
                DraftDefinition.FromJson(Read("draft_provisional_v1.json")),OvertakeDefinition.FromJson(Read("overtake_provisional_v1.json")),plan.Junctions,plan.Connections,events,rangeLayout:layout);
        }
        [UnityTest] public IEnumerator RealPersonalMatTurnsTowardTargetsAndBackWithoutChangingTheSnapshotOrAuthority()
        {
            var plan=Plan();var events=new RaceEventLog(6000);var field=Field(plan,events);
            var legacyRoot=new GameObject("Legacy tangent proxy test");var directedRoot=new GameObject("Explicit heading proxy test");var plannedRoot=new GameObject("Venue heading proxy test");var physicalRoot=new GameObject("Accepted physical shooting center test");
            try
            {
                var feed=legacyRoot.AddComponent<RacePresentationFeed>();var legacy=legacyRoot.AddComponent<AthleteProxyPresentation>();legacy.Initialize(feed,plan.Visual);legacy.enabled=false;
                var directed=directedRoot.AddComponent<AthleteProxyPresentation>();directed.Initialize(feed,plan.Visual,null,Vector3.forward);directed.enabled=false;
                var planned=plannedRoot.AddComponent<AthleteProxyPresentation>();planned.Initialize(feed,plan);planned.enabled=false;
                var physicalView=physicalRoot.AddComponent<AthleteProxyPresentation>();physicalView.Initialize(feed,plan.Visual,null,Vector3.forward,preserveAcceptedLine:true);physicalView.enabled=false;
                var mats=plannedRoot.AddComponent<RangeMatPresentation>();mats.Initialize(plan);
                var inputs=new SprintAthleteInput[1];var lines=new[]{.2};bool priorShooting=false,setupPartial=false,exitPartial=false,returned=false;int stances=0,seen=0;
                float priorAngle=0,priorWeight=0;bool havePrior=false;
                while(field.Tick.Value<30000&&!field.IsComplete)
                {
                    var actor=field.GetAthlete(0);var tick=field.Tick.Next();
                    bool trigger=actor.ActiveShooting?.Phase==ShootingPhase.Aiming;
                    inputs[0]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,800),1,1,0),new WeaponAimCommand(tick,actor.AthleteId,0,0),trigger);
                    field.Step(inputs,lines);var snapshot=RacePresentationSnapshot.Capture(field);feed.Publish(snapshot);var state=snapshot.Get(0);
                    if(!state.OnCourse)continue;
                    if(!state.ShootingActive&&!priorShooting&&field.Tick.Value>10)continue;
                    var physical=field.GetPhysicalPoint(0);long routeBefore=actor.RouteNanometers,motionBefore=actor.Motion.DistanceNanometers,tickBefore=field.Tick.Value;
                    int eventCount=events.Count;double lateralBefore=field.GetLateral(0);var sightBefore=actor.ActiveShooting?.Sight??default;var frameBefore=actor.LastFrame;
                    legacy.Render(1);directed.Render(1);planned.Render(1);physicalView.Render(1);
                    var a=legacy.GetProxy(state.AthleteId);var b=directed.GetProxy(state.AthleteId);var c=planned.GetProxy(state.AthleteId);
                    var point=plan.Visual.Get(state.LoopId).Sample(state.LocalNanometers/(double)SkiState.NanometersPerMeter);var tangent=new Vector3((float)point.ForwardX,0,(float)point.ForwardZ);
                    var acceptedCentre=new Vector3((float)(point.X+point.ForwardZ*lateralBefore),(float)point.Y+1,(float)(point.Z-point.ForwardX*lateralBefore));
                    Assert.That(Vector3.Distance(physicalView.GetProxy(state.AthleteId).localPosition,acceptedCentre),Is.LessThan(1e-6f),"Physical shooting keeps the actual accepted line during every setup/aim/recovery phase.");
                    Assert.That(Quaternion.Angle(physicalView.GetProxy(state.AthleteId).localRotation,b.localRotation),Is.LessThan(.03f));
                    Assert.That(Vector3.Angle(a.forward,tangent),Is.LessThan(.03f));Assert.That(Vector3.Distance(a.localPosition,b.localPosition),Is.LessThan(1e-6f));
                    Assert.That(Quaternion.Angle(b.localRotation,c.localRotation),Is.LessThan(.03f));Assert.That(Vector3.Distance(b.localPosition,c.localPosition),Is.LessThan(1e-6f));
                    float weight=state.ShootingActive?Mathf.SmoothStep(0,1,AthleteRiflePresentation.Progress(state.ShootingPhase,state.ShootingPhaseElapsedTicks,state.ShootingPhaseDurationTicks)):0;
                    float angle=Vector3.Angle(b.forward,tangent);
                    if(state.ShootingActive)
                    {
                        Assert.That(state.LoopId,Is.EqualTo("mat_1"));Assert.That(state.LocalNanometers,Is.EqualTo(18*SkiState.NanometersPerMeter));
                        Assert.That(actor.RouteNanometers,Is.EqualTo(actor.RangePath.FiringRouteNanometers(actor.Lifecycle.CompletedBouts)));
                        Assert.That(Vector3.Angle(tangent,Vector3.left),Is.LessThan(.03f));seen|=1<<(int)state.ShootingPhase;
                        if(state.ShootingPhase==ShootingPhase.Setup&&weight>0&&weight<1){setupPartial=true;Assert.That(angle,Is.InRange(.001f,89.999f));}
                        if(state.ShootingPhase==ShootingPhase.Exiting&&weight>0&&weight<1){exitPartial=true;Assert.That(angle,Is.InRange(.001f,89.999f));}
                        if(state.ShootingPhase==ShootingPhase.Aiming)
                        {
                            stances|=1<<(int)state.Stance;Assert.That(Vector3.Angle(b.forward,Vector3.forward),Is.LessThan(.03f));
                            Assert.That(Vector3.Distance(physicalView.GetRig(state.AthleteId).transform.position,directed.GetRig(state.AthleteId).transform.position),Is.EqualTo((float)Math.Abs(lateralBefore)).Within(1e-5f));
                            Assert.That(Vector3.Distance(directed.GetRig(state.AthleteId).transform.position,mats.Anchor(state.RangeLane).position),Is.LessThan(.001f));
                            var rig=directed.GetRig(state.AthleteId);var ray=state.Sight.Ray;
                            Assert.That(Vector3.Angle(rig.GetComponent<AthleteEquipmentBinding>().Rifle.forward,rig.ModelRoot.TransformDirection(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ))),Is.LessThan(.03f));
                        }
                    }
                    else
                    {
                        Assert.That(angle,Is.LessThan(.03f));if(priorShooting)returned=true;
                    }
                    if(havePrior&&state.LoopId=="mat_1")Assert.That(Mathf.Abs(angle-priorAngle),Is.LessThanOrEqualTo(90*Mathf.Abs(weight-priorWeight)+.06f),"Rotation follows rifle transition without a separate discontinuous turn.");
                    priorAngle=angle;priorWeight=weight;havePrior=state.LoopId=="mat_1";priorShooting=state.ShootingActive;
                    Assert.That(feed.Current,Is.SameAs(snapshot));Assert.That(field.Tick.Value,Is.EqualTo(tickBefore));Assert.That(actor.RouteNanometers,Is.EqualTo(routeBefore));Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo(motionBefore));
                    Assert.That(field.GetPhysicalPoint(0),Is.EqualTo(physical));Assert.That(field.GetLateral(0),Is.EqualTo(lateralBefore));Assert.That(actor.LastFrame,Is.EqualTo(frameBefore));
                    Assert.That(actor.ActiveShooting?.Sight??default,Is.EqualTo(sightBefore));Assert.That(events.Count,Is.EqualTo(eventCount));
                    if(state.CompletedBouts>=2&&!state.ShootingActive)break;
                }
                Assert.That(stances,Is.EqualTo(3),"Actual prone and standing bouts must both face the target bank.");Assert.That(setupPartial&&exitPartial&&returned,Is.True);
                foreach(var phase in new[]{ShootingPhase.Setup,ShootingPhase.Aiming,ShootingPhase.BoltCycle,ShootingPhase.Exiting})Assert.That(seen&(1<<(int)phase),Is.Not.Zero);
                yield return null;
            }
            finally{Object.Destroy(legacyRoot);Object.Destroy(directedRoot);Object.Destroy(plannedRoot);Object.Destroy(physicalRoot);}
        }
        [UnityTest] public IEnumerator InvalidShootingDirectionRejectsBeforeClearingExistingPresentation()
        {
            var plan=Plan();var field=Field(plan,new RaceEventLog(6000));var root=new GameObject("Proxy direction validation test");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<AthleteProxyPresentation>();view.Initialize(feed,plan.Visual);view.enabled=false;
                var actor=field.GetAthlete(0);var tick=field.Tick.Next();field.Step(new[]{new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,800),1,1,0),new WeaponAimCommand(tick,actor.AthleteId,0,0),false)},new double[1]);
                feed.Publish(RacePresentationSnapshot.Capture(field));view.Render(1);var proxy=view.GetProxy(actor.AthleteId.Value);var before=proxy.localRotation;
                foreach(var direction in new[]{Vector3.zero,Vector3.up,Vector3.forward*2,new Vector3(float.NaN,0,1),new Vector3(0,0,float.PositiveInfinity)})
                {
                    Assert.Throws<ArgumentException>(()=>view.Initialize(feed,plan.Visual,null,direction));Assert.That(view.GetProxy(actor.AthleteId.Value),Is.SameAs(proxy));
                    view.Render(1);Assert.That(proxy.localRotation,Is.EqualTo(before));Assert.That(view.Count,Is.EqualTo(1));
                }
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}

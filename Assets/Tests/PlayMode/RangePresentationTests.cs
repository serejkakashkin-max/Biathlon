using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class RangePresentationTests
    {
        [Test] public void RealAimingCameraMovesBetweenAcceptedTicksWithoutAdvancingShooting()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            var root=new GameObject("Sub-tick active aim regression");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<RangePresentation>();view.enabled=false;
                view.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")));
                var lateUpdate=typeof(RangePresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                bool sampled=false;
                for(int tick=0;tick<10000;tick++)
                {
                    runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);
                    var previous=feed.Previous;var current=snapshot.Get(0);
                    if(previous==null||current.ShootingPhase!=ShootingPhase.Aiming||previous.Get(0).ShootingPhase!=ShootingPhase.Aiming||
                        previous.Get(0).ShotsFired!=current.ShotsFired||current.ShotOutcome!=ShotOutcome.NoShot)continue;
                    var clock=runner.Field.Tick;var sight=runner.Field.GetAthlete(0).ActiveShooting.Sight;
                    feed.SetInterpolationAlpha(.25);lateUpdate.Invoke(view,null);var first=view.AimCamera.transform.forward;
                    feed.SetInterpolationAlpha(.75);lateUpdate.Invoke(view,null);
                    Assert.That(Vector3.Distance(first,view.AimCamera.transform.forward),Is.GreaterThan(.00000001f),"The sight camera currently freezes between 20 Hz observations.");
                    Assert.That(runner.Field.Tick,Is.EqualTo(clock));
                    Assert.That(runner.Field.GetAthlete(0).ActiveShooting.Sight,Is.EqualTo(sight),"Rendering cannot move the authoritative optical ray.");
                    sampled=true;break;
                }
                Assert.That(sampled,Is.True,"A real consecutive aiming pair must be reached.");
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator ActualBoutDrivesCameraAndClosuresWithoutAuthorityWrites()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            var root=new GameObject("range_test");var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<RangePresentation>();
            var chase=root.AddComponent<Camera>();
            view.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")),chase);
            bool sawRange=false,sawShot=false,sawSetup=false,sawExit=false;
            for(int i=0;i<20000;i++)
            {
                runner.Step();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));view.Refresh();
                var state=feed.Current.Get(0);
                bool aiming=state.ShootingActive&&(state.ShootingPhase==ShootingPhase.Aiming||state.ShootingPhase==ShootingPhase.BoltCycle);
                Assert.That(view.Visible,Is.EqualTo(aiming),"Preparation and rifle recovery must stay visible in the athlete camera.");
                Assert.That(chase.enabled,Is.EqualTo(!aiming),"Only the active gameplay camera should submit the world.");
                if(state.ShootingActive&&state.ShootingPhase==ShootingPhase.Setup)sawSetup=true;
                if(state.ShootingActive&&state.ShootingPhase==ShootingPhase.Exiting)sawExit=true;
                if(view.Visible){sawRange=true;Assert.That(view.ClosedMask,Is.EqualTo(feed.Current.Get(0).ClosedTargetsMask));Assert.That(view.AimCamera.enabled,Is.True);
                    var geometry=TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json"));var observed=feed.Current.Get(0);
                    Assert.That(view.Bank.transform.localPosition.z,Is.EqualTo((float)geometry.DistanceMeters));
                    for(int j=0;j<5;j++){var aperture=view.Bank.Aperture(j);Assert.That(aperture.transform.localPosition,Is.EqualTo(new Vector3((float)geometry.CenterX(j),0,0)));Assert.That(aperture.transform.localScale.x,Is.EqualTo((float)(2*geometry.Radius(ShootingStance.Standing))));Assert.That(aperture.transform.localScale.y,Is.EqualTo(aperture.transform.localScale.x));}
                    Assert.That(view.Bank.GetComponentsInChildren<Collider>(),Is.Empty);
                    for(int j=0;j<5;j++)Assert.That(view.Bank.Aperture(j).sharedMaterial.shader.name=="Biathlon/ProneTargetFace",
                        Is.EqualTo(observed.Stance==ShootingStance.Prone&&(observed.ClosedTargetsMask&(1<<j))==0));
                }
                if(view.Status.Length>0)sawShot=true;
                if(sawShot&&!view.Visible)break;
            }
            Assert.That(sawRange&&sawShot,Is.True);Assert.That(view.Visible,Is.False);
            Assert.That(sawSetup&&sawExit,Is.True);
            feed.ResetRace();view.Refresh();Assert.That(view.Status,Is.Empty);
            Object.Destroy(root);yield return null;
        }
        [Test] public void CurrentPlayerAimAndAcceptedShotStayExactWhileProceduralMotionIsPredicted()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
            var root=new GameObject("Current player aim and exact shot regression");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<RangePresentation>();view.enabled=false;
                var id=runner.Field.GetAthlete(0).AthleteId;
                view.Initialize(feed,id.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")));
                bool reached=false;
                for(int n=0;n<10000;n++)
                {
                    var tick=runner.Field.Tick.Next();
                    runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,650),1,1,0),new WeaponAimCommand(tick,id,0,0)),0);
                    feed.Publish(RacePresentationSnapshot.Capture(runner.Field));
                    if(feed.Current.Get(0).ShootingPhase==ShootingPhase.Aiming&&feed.Previous?.Get(0).ShootingPhase==ShootingPhase.Aiming){reached=true;break;}
                }
                Assert.That(reached,Is.True,"Real player-controlled aiming must be reached.");
                var target=runner.Field.Tick.Next();
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(target,id,650),1,1,0),new WeaponAimCommand(target,id,1000,0)),0);
                feed.Publish(RacePresentationSnapshot.Capture(runner.Field));
                var current=feed.Current.Get(0);var prior=feed.Previous.Get(0);var session=runner.Field.GetAthlete(0).ActiveShooting;
                Assert.That(current.ControlledAim,Is.EqualTo(session.LastFrame.Aim));
                Assert.That(current.ControlledAim.YawRadians,Is.GreaterThan(prior.ControlledAim.YawRadians),"The fixture must include new player input.");
                view.Render(0);Assert.That(view.ObservedSight,Is.EqualTo(current.Sight),"New input must appear at the latest accepted angle, with no whole-tick interpolation delay.");
                view.Render(.75);
                double proceduralDelta=(current.Sight.YawRadians-current.ControlledAim.YawRadians)-(prior.Sight.YawRadians-prior.ControlledAim.YawRadians);
                Assert.That(view.ObservedSight.YawRadians-proceduralDelta*.75,Is.EqualTo(current.Sight.YawRadians).Within(1e-12),"Only procedural motion is predicted; current player control stays exact.");
                target=runner.Field.Tick.Next();
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(target,id,650),1,1,0),new WeaponAimCommand(target,id,0,0),true),0);
                feed.Publish(RacePresentationSnapshot.Capture(runner.Field));
                Assert.That(feed.Current.Get(0).ShotOutcome,Is.Not.EqualTo(ShotOutcome.NoShot));
                var clock=runner.Field.Tick;var impact=session.LastFrame.Shot.Impact;var shotSight=session.LastFrame.ShotAim;
                foreach(double alpha in new[]{0,.5,1.0})
                {
                    view.Render(alpha);Assert.That(view.ObservedSight,Is.EqualTo(shotSight),"An accepted shot always displays the exact ray used by geometric resolution.");
                }
                Assert.That(runner.Field.Tick,Is.EqualTo(clock));Assert.That(session.LastFrame.Shot.Impact,Is.EqualTo(impact));
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>view.Render(1.1));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}

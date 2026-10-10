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
    }
}

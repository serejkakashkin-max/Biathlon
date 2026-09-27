using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class G2CorrectiveTests
    {
        [UnityTest] public IEnumerator RealRaceStancesMatchPhysicalMatsAndCaptureChaseMotion()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/PlayableSprint.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            yield return null;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var host in root.GetComponentsInChildren<PlayableSprintHost>())host.gameObject.SetActive(false);
                foreach(var camera in root.GetComponentsInChildren<Camera>())camera.enabled=false;
            }
            var folder=Path.GetFullPath("artifacts/g2-corrective-016");Directory.CreateDirectory(folder);
            try
            {
                foreach(var format in new[]{CompetitionFormat.Sprint,CompetitionFormat.MassStart})
                {
                    var root=new GameObject("Corrective real-race evidence");
                    try
                    {
                        string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
                        string Track(string n)=>Resources.Load<TextAsset>("Biathlon/Tracks/"+Path.GetFileNameWithoutExtension(n)).text;
                        bool mass=format==CompetitionFormat.MassStart;string prefix=mass?"synthetic_mass_stadium":"synthetic_stadium";
                        var runner=ChampionshipRaceFactory.Create(Read,Track,format,new BalanceEventAudit(5),new AiDecisionCounts(),count:mass?30:1);
                        var visual=TrackVisualDefinition.FromJson(Read(mass?"mass_stadium_visual_v1.json":"stadium_visual_v1.json"),runner.Field.Track);
                        var lanes=PhysicalRangeLaneDefinition.FromJson(Track(prefix+"_lanes_v1.json"),runner.Field.Track);
                        var mats=root.AddComponent<RangeMatPresentation>();mats.Initialize(runner.Field.Track,visual,lanes);Assert.That(mats.Count,Is.EqualTo(30));
                        var feed=root.AddComponent<RacePresentationFeed>();var proxies=root.AddComponent<AthleteProxyPresentation>();proxies.Initialize(feed,visual);proxies.enabled=false;
                        var cam=new GameObject("Real race evidence camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);cam.enabled=false;cam.fieldOfView=45;cam.farClipPlane=1200;
                        root.AddComponent<RenderQualityPresentation>().Initialize(cam);proxies.SetViewCamera(cam);
                        int captured=0;bool motion=false;int placed=0;
                        using(var target=new BenchmarkRenderTarget(1280,720))
                        for(int tick=0;tick<50000&&captured<2;tick++)
                        {
                            runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);
                            if(!mass&&!motion&&tick>=400)
                            {
                                proxies.Render(1);var chase=cam.gameObject.AddComponent<AthleteChaseCamera>();chase.enabled=false;
                                chase.Follow(proxies.GetProxy(snapshot.Get(0).AthleteId),ChaseCameraSettings.FromJson(Read("chase_camera_v1.json")),0);
                                var frames=Path.Combine(folder,"race-motion");Directory.CreateDirectory(frames);
                                for(int frame=0;frame<60;frame++)
                                {
                                    runner.Step();snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);proxies.Render(1);chase.Render(1f/snapshot.TickFrequency);
                                    for(int settle=0;settle<2;settle++){target.Render(cam);yield return null;}
                                    target.SavePng(Path.Combine(frames,frame.ToString("D3")+".png"));
                                }
                                Object.DestroyImmediate(chase);motion=true;
                            }
                            var a=snapshot.Get(0);
                            var wanted=captured==0?ShootingStance.Prone:ShootingStance.Standing;
                            if(!a.ShootingActive||a.ShootingPhase!=ShootingPhase.Aiming||a.Stance!=wanted)continue;
                            proxies.Render(1);
                            for(int i=0;i<snapshot.Count;i++)
                            {
                                var observed=snapshot.Get(i);if(!observed.ShootingActive||observed.ShootingPhase!=ShootingPhase.Aiming)continue;
                                var anchor=mats.Anchor(observed.RangeLane);var rig=proxies.GetRig(observed.AthleteId);
                                Assert.That(observed.LoopId,Is.EqualTo(lanes.GetLane(observed.RangeLane-1)));
                                var local=anchor.InverseTransformPoint(rig.transform.position);
                                Assert.That(local.magnitude,Is.LessThan(.01f),"Race root must coincide with its own physical mat anchor.");
                                foreach(var bone in new[]{AthleteBone.Hips,AthleteBone.LeftFoot,AthleteBone.RightFoot})
                                {
                                    var contact=anchor.InverseTransformPoint(rig.Bone(bone).position);var art=StadiumArtDefinition.Load();
                                    Assert.That(Mathf.Abs(contact.x),Is.LessThan(art.mat_width_m*.5f));
                                    Assert.That(Mathf.Abs(contact.z),Is.LessThan(art.mat_length_m*.5f));
                                }
                                placed++;
                            }
                            var subject=proxies.GetRig(a.AthleteId);var focus=subject.transform.position+Vector3.up*(a.Stance==ShootingStance.Prone?.55f:1);
                            cam.transform.position=focus+subject.transform.TransformDirection(new Vector3(2.8f,1.4f,3.0f));cam.transform.LookAt(focus);cam.fieldOfView=38;
                            long frozen=runner.Field.Tick.Value;
                            double lateralBefore=runner.Field.GetLateral(0);
                            for(int settle=0;settle<3;settle++){proxies.Render(1);target.Render(cam);yield return null;}
                            Assert.That(runner.Field.Tick.Value,Is.EqualTo(frozen));
                            Assert.That(runner.Field.GetLateral(0),Is.EqualTo(lateralBefore));
                            Assert.That(feed.Current,Is.SameAs(snapshot));
                            target.SavePng(Path.Combine(folder,format+"-"+a.Stance+"-on-mat.png"));
                            File.WriteAllText(Path.Combine(folder,format+"-"+a.Stance+"-on-mat.txt"),"Actual frozen AI race; tick="+frozen+"; lane="+a.RangeLane+"; loop="+a.LoopId+"; distance="+a.LocalNanometers+"; authoritative lateral="+a.LateralMeters+"; visible setup is centered on physical lane; authority unchanged.");
                            captured++;
                        }
                        Assert.That(captured,Is.EqualTo(2));Assert.That(placed,Is.GreaterThanOrEqualTo(2));
                    }
                    finally{Object.DestroyImmediate(root);}
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

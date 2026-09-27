using System;
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
    /// <summary>Repeatable owner-review evidence. Fixture poses are explicitly separate from live gameplay.</summary>
    public class G2OwnerReviewTests
    {
        [Serializable] private sealed class FrameEvidence
        {
            public int schema_version=1,width=2560,height=1440;public string id,kind,tier,weather,lighting,shot,unity;
            public double time;public long source_tick;public Vector3 camera_position;public Quaternion camera_rotation;public float fov;public bool includes_hud=false;
        }
        [UnityTest] public IEnumerator CaptureOwnerMatrixControlsAndCloseMotionWithoutAdvancingBenchmarkRace()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            var extra=new GameObject("Owner review only fixtures");
            try
            {
                VisualBenchmarkPresentation p=null;foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent(out VisualBenchmarkPresentation found))p=found;Assert.That(p,Is.Not.Null);p.enabled=false;p.Animate=false;p.ShowControls=false;
                var folder=Path.GetFullPath("artifacts/g2-owner-review");Directory.CreateDirectory(folder);File.Delete(Path.Combine(folder,"capture-complete.txt"));long sourceTick=p.Athletes.SourceTick;Assert.That(sourceTick,Is.EqualTo(400));
                using(var target=new BenchmarkRenderTarget(2560,1440))
                {
                    foreach(var tier in new[]{"low","balanced","high"})foreach(var weather in new[]{"clear","overcast","snowfall"})foreach(var shot in new[]{"approach","athlete","targets"})
                    {
                        p.Tier=tier;p.WeatherPreset=weather;p.LightingPreset=weather=="clear"?"clear_cold":"overcast";p.Shot=shot;p.Seek(8);
                        for(int i=0;i<4;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}Save(tier+"-"+weather+"-"+shot,"detached benchmark pose fixture",8,p.View);
                    }
                    p.Tier="balanced";p.WeatherPreset="snowfall";p.LightingPreset="overcast";p.Shot="rail";
                    var rewindIds=new[]{"rewind-a","rewind-away","rewind-b"};var rewindTimes=new[]{2.0,6.0,2.0};
                    for(int k=0;k<3;k++){p.Seek(rewindTimes[k]);for(int i=0;i<4;i++){p.RenderFrame(rewindTimes[k]);target.Render(p.View);yield return null;}Save(rewindIds[k],"rewind fixture",rewindTimes[k],p.View);}
                    p.Tier="high";p.WeatherPreset="clear";p.LightingPreset="clear_cold";
                    foreach(var shot in new[]{"sector","range","finish","crowd_zoom"}){p.Shot=shot;p.Seek(8);for(int i=0;i<4;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}Save("event-"+shot,"detached benchmark venue",8,p.View);}
                    p.Shot="athlete";p.Seek(8);var subject=p.Athletes.Subject;var rig=subject.GetComponent<AthleteAnimationPresentation>().Rig;
                    foreach(var close in new[]{"front","back","side","face"})
                    {
                        for(int i=0;i<4;i++)
                        {
                            p.Athletes.RenderAt(8);Vector3 at=close=="face"?rig.Bone(AthleteBone.Head).TransformPoint(new Vector3(0,.07f,.03f)):subject.position+Vector3.up;
                            var offset=close=="front"?new Vector3(1,.4f,3.6f):close=="back"?new Vector3(-1,.4f,-3.6f):close=="side"?new Vector3(3.8f,.3f,.1f):new Vector3(.3f,.05f,1.1f);
                            p.View.transform.position=at+offset;p.View.transform.LookAt(at);p.View.fieldOfView=35;rig.GetComponent<AthleteSkinDetailBinding>().Prepare(1,false,true);SubmitEnvironment(8);target.Render(p.View);yield return null;
                        }
                        Save("hero-"+close,"detached skiing pose in benchmark lighting",8,p.View);
                    }
                    var chase=p.View.gameObject.AddComponent<AthleteChaseCamera>();chase.enabled=false;var pivot=new GameObject("Preserved gameplay pivot offset");pivot.transform.SetParent(subject,false);pivot.transform.localPosition=Vector3.up;
                    var chaseSettings=ChaseCameraSettings.FromJson(Resources.Load<TextAsset>("Biathlon/chase_camera_v1").text);chase.Follow(pivot.transform,chaseSettings,0);
                    for(int i=0;i<4;i++){p.Athletes.RenderAt(8);p.View.fieldOfView=60;chase.Render(0);SubmitEnvironment(8);target.Render(p.View);yield return null;}
                    Assert.That(Vector3.Distance(p.View.transform.position,pivot.transform.position-Vector3.forward*chaseSettings.distance_m+Vector3.up*chaseSettings.height_m),Is.LessThan(.001f),"Capture must retain chase composition, not the broadcast director pose.");
                    Save("gameplay-chase","production chase camera on detached benchmark subject; collision-free fixture",8,p.View);
                    var clipFolder=Path.Combine(folder,"motion-frames");Directory.CreateDirectory(clipFolder);
                    using(var motionTarget=new BenchmarkRenderTarget(1280,720))for(int frame=0;frame<48;frame++)
                    {
                        double time=8+frame/24.0;p.Athletes.RenderAt(time);chase.Render(1f/24);
                        // Test coroutines may resume before PostLateUpdate; settle each frozen sample before readback.
                        for(int settle=0;settle<3;settle++){p.Athletes.RenderAt(time);SubmitEnvironment(time);motionTarget.Render(p.View);yield return null;}
                        motionTarget.SavePng(Path.Combine(clipFolder,"chase-"+frame.ToString("D3")+".png"));Assert.That(p.Athletes.SourceTick,Is.EqualTo(sourceTick));
                    }
                    Object.DestroyImmediate(chase);Object.DestroyImmediate(pivot);
                    var stadium=StadiumArtDefinition.Load();const int reviewLane=1;
                    var pose=AthleteRigFactory.Create(extra.transform);pose.transform.position=new Vector3(stadium.LaneX(reviewLane),.0675f,0);pose.GetComponent<AthleteBibPresentation>().SetNumber(12);
                    foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                    {
                        pose.ResetPose();pose.GetComponent<AthleteShootingPose>().Sample(pose,stance,1);pose.GetComponent<AthleteRiflePresentation>().Sample(pose,1,stance:stance);pose.GetComponent<AthleteGroundContact>().Sample(pose,0,0,SkiLocomotion.Idle,1);pose.GetComponent<AthleteGripPresentation>().Render(pose);
                        foreach(var bone in new[]{AthleteBone.Hips,AthleteBone.LeftFoot,AthleteBone.RightFoot})
                        {
                            var point=pose.Bone(bone).position;
                            Assert.That(Mathf.Abs(point.x-stadium.LaneX(reviewLane)),Is.LessThan(stadium.mat_width_m*.5f));
                            Assert.That(Mathf.Abs(point.z),Is.LessThan(stadium.mat_length_m*.5f));
                        }
                        foreach(var angle in new[]{"front","side"})
                        {
                            for(int i=0;i<4;i++){p.Athletes.RenderAt(8);var at=pose.transform.position+new Vector3(0,stance==ShootingStance.Prone?.6f:1.1f,.25f);p.View.transform.position=at+(angle=="side"?new Vector3(3.8f,.4f,.1f):new Vector3(1.5f,.4f,3.3f));p.View.transform.LookAt(at);p.View.fieldOfView=35;SubmitEnvironment(8);target.Render(p.View);yield return null;}
                            Save("shooting-"+stance+"-"+angle,"posed stance review fixture; no gameplay shot",8,p.View);
                        }
                    }
                    Object.DestroyImmediate(pose.gameObject);
                    string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
                    string Track(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
                    var runner=ChampionshipRaceFactory.Create(Read,Track,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:1);
                    var rangeRoot=new GameObject("Actual bout observation camera");rangeRoot.transform.SetParent(extra.transform);var feed=rangeRoot.AddComponent<RacePresentationFeed>();var range=rangeRoot.AddComponent<RangePresentation>();
                    range.Initialize(feed,runner.Field.GetAthlete(0).AthleteId.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")));
                    int photographed=0;
                    for(int tick=0;tick<30000&&photographed<2;tick++)
                    {
                        runner.Step();var snapshot=RacePresentationSnapshot.Capture(runner.Field);feed.Publish(snapshot);range.Refresh();
                        if(!range.Visible||snapshot.Get(0).ShotsFired!=0||snapshot.Get(0).CompletedBouts!=photographed)continue;
                        range.AimCamera.enabled=false;long frozen=feed.Current.Tick;for(int i=0;i<4;i++){target.Render(range.AimCamera);yield return null;}Assert.That(feed.Current.Tick,Is.EqualTo(frozen));
                        Save("dedicated-aim-"+snapshot.Get(0).Stance,"production RangePresentation camera from frozen real AI bout; no HUD overlay",frozen/60.0,range.AimCamera,frozen);photographed++;
                    }
                    Assert.That(photographed,Is.EqualTo(2));Object.DestroyImmediate(rangeRoot);
                    void Save(string id,string kind,double time,Camera camera,long tick=-1)
                    {
                        Assert.That(p.Athletes.SourceTick,Is.EqualTo(sourceTick));target.SavePng(Path.Combine(folder,id+".png"));
                        var e=new FrameEvidence{id=id,kind=kind,tier=p.Tier,weather=p.WeatherPreset,lighting=p.LightingPreset,shot=p.Shot,time=time,source_tick=tick<0?sourceTick:tick,unity=Application.unityVersion,camera_position=camera.transform.position,camera_rotation=camera.transform.rotation,fov=camera.fieldOfView};
                        File.WriteAllText(Path.Combine(folder,id+".json"),JsonUtility.ToJson(e,true));
                    }
                    void SubmitEnvironment(double time)
                    {
                        p.Backdrop.RenderTrees(p.View);
                        foreach(var f in p.Forests)f.Render(p.View,p.Tier,(float)time,new Vector2(1,.3f));p.GroundDressing.Render(p.View);p.Understory.Render(p.View);p.GetComponent<SpectatorCrowd>().Render(p.View,p.Tier,(float)time);
                    }
                }
                File.WriteAllText(Path.Combine(folder,"capture-complete.txt"),"Owner review captures complete; benchmark source tick remained "+sourceTick+".\n");
            }
            finally{Object.DestroyImmediate(extra);if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

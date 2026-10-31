using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Presentation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class M164VisualReviewTests
    {
        private const string Output="artifacts/m16-4/views/";
        [UnityTest]public IEnumerator NativeEquipmentAndFrontPlateViews()
        {
            Directory.CreateDirectory(Output);var root=new GameObject("M16.4 visual review");BenchmarkRenderTarget target=null;Material snow=null,matMaterial=null;
            var ambient=RenderSettings.ambientLight;
            try
            {
                RenderSettings.ambientLight=new Color(.7f,.75f,.8f);
                var lightObject=new GameObject("Review light");lightObject.transform.SetParent(root.transform,false);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
                var cameraObject=new GameObject("Review camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.3f,.4f,.47f);camera.fieldOfView=40;
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localScale=new Vector3(8,.04f,8);floor.transform.localPosition=new Vector3(0,-.02f,0);floor.GetComponent<Collider>().enabled=false;
                snow=new Material(Shader.Find("Universal Render Pipeline/Lit"));snow.SetColor("_BaseColor",new Color(.86f,.91f,.94f));floor.GetComponent<Renderer>().sharedMaterial=snow;
                // Match the real flush-top physical mat, rather than raising a fixture through the skis.
                var mat=GameObject.CreatePrimitive(PrimitiveType.Cube);mat.transform.SetParent(root.transform,false);mat.transform.localPosition=new Vector3(0,-.0075f,0);mat.transform.localScale=new Vector3(1.5f,.015f,2);mat.GetComponent<Collider>().enabled=false;
                matMaterial=new Material(Resources.Load<Shader>("Biathlon/Stadium/PhysicalRangeMat"));matMaterial.SetColor("_BaseColor",new Color(.15f,.18f,.21f));mat.GetComponent<Renderer>().sharedMaterial=matMaterial;
                var rig=AthleteRigFactory.Create(root.transform);if(rig.Animator!=null)rig.Animator.enabled=false;
                foreach(var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}
                var pose=rig.GetComponent<AthleteShootingPose>();var rifle=rig.GetComponent<AthleteRiflePresentation>();var contact=rig.GetComponent<AthleteGroundContact>();
                target=new BenchmarkRenderTarget(1280,720);
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                {
                    rig.ResetPose();pose.Sample(rig,stance,1);rifle.Sample(rig,1,stance:stance);contact.Sample(rig,0,0,SkiLocomotion.Idle,rifle.HandWeight);
                    camera.transform.position=stance==ShootingStance.Prone?new Vector3(2.7f,1.5f,2.7f):new Vector3(2.6f,1.7f,3);
                    camera.transform.LookAt(new Vector3(0,stance==ShootingStance.Prone?.3f:.8f,0));yield return Capture(stance.ToString().ToLowerInvariant());
                    camera.transform.position=new Vector3(0,4,.05f);camera.transform.LookAt(Vector3.zero,Vector3.forward);yield return Capture(stance.ToString().ToLowerInvariant()+"-top");
                }
                rig.gameObject.SetActive(false);floor.SetActive(false);mat.SetActive(false);
                var geometry=TargetGeometryDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/target_geometry_v1"));
                var bank=Object.Instantiate(Resources.Load<TargetBankArt>("Biathlon/Stadium/TargetBank"),root.transform);bank.Apply(geometry,ShootingStance.Prone,0);var settings=ShotImpactVisualDefinition.Load();bank.ConfigureImpacts(geometry,settings);
                var ledger=new ShotImpactLedger(geometry,settings.bullet_diameter_m,1);var resolver=new TargetGeometrySystem(geometry);
                foreach(var item in new[]{(-geometry.SpacingMeters+geometry.Radius(ShootingStance.Prone)-.0004,0d),(geometry.Radius(ShootingStance.Prone)+.0004,0d),(.08,.04)})
                {
                    var impact=resolver.IntersectPlanePoint(item.Item1,item.Item2,ShootingStance.Prone);int i=ledger.AcceptedCount+1;
                    ledger.TryObserve("review",i,0,1,i,ShootingStance.Prone,impact.Hit?ShotOutcome.TargetClosed:ShotOutcome.Miss,impact,out var evidence);bank.RecordImpact(evidence);
                }
                camera.orthographic=true;camera.orthographicSize=.19f;camera.transform.position=new Vector3(0,0,-2);camera.transform.LookAt(Vector3.zero);yield return Capture("plate-open");
                bank.Apply(geometry,ShootingStance.Prone,2);yield return Capture("plate-edge-hit-closed");
                IEnumerator Capture(string name)
                {
                    for(int i=0;i<5;i++){target.Render(camera);yield return null;}
                    string path=Path.GetFullPath(Output+name+".png");target.SavePng(path);
                    var probe=new Texture2D(2,2);
                    try{ImageConversion.LoadImage(probe,File.ReadAllBytes(path));int visible=0;foreach(var pixel in probe.GetPixels32())if(pixel.r>10||pixel.g>10||pixel.b>10)visible++;Assert.That(visible,Is.GreaterThan(10000),"The actual native camera must render, not save an unsubmitted black texture.");}
                    finally{Object.DestroyImmediate(probe);}
                }
            }
            finally{target?.Dispose();RenderSettings.ambientLight=ambient;Object.DestroyImmediate(root);if(snow!=null)Object.DestroyImmediate(snow);if(matMaterial!=null)Object.DestroyImmediate(matMaterial);}
            yield return null;
        }

        [UnityTest]public IEnumerator ActualAcceptedShotFeedbackFitsNormalAndLargeHud()
        {
            Directory.CreateDirectory(Output);
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            string Fixture(string n)=>File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/"+n));
            var runner=Biathlon.AI.ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new Biathlon.AI.BalanceEventAudit(5),new Biathlon.AI.AiDecisionCounts(),count:1);
            var root=new GameObject("Actual shot HUD review");
            try
            {
                string actor=runner.Field.GetAthlete(0).AthleteId.Value;var feed=root.AddComponent<RacePresentationFeed>();var bank=Object.Instantiate(Resources.Load<TargetBankArt>("Biathlon/Stadium/TargetBank"),root.transform);
                var view=root.AddComponent<ShotImpactPresentation>();view.Initialize(feed,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")),new[]{bank},actor);
                var hud=root.AddComponent<GameplayHud>();hud.Initialize(feed,actor);
                for(int i=0;i<20000;i++){runner.Step();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));if(view.Ledger.AcceptedCount>0)break;}
                hud.Refresh();Assert.That(view.Ledger.AcceptedCount,Is.EqualTo(1));Assert.That(hud.Root.Q("shot_impact_feedback").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                long tick=runner.Field.Tick.Value;
                yield return Capture(1600,900,"ru",0);yield return Capture(1024,768,"en",2);yield return Capture(1280,720,"ru",0);
                // Explicit presentation layout fixtures. They do not fire or alter this race;
                // the real accepted-shot capture above remains separate evidence.
                var state=feed.Current.Get(0);var geometry=view.Geometry;var resolver=new TargetGeometrySystem(geometry);
                foreach(bool hit in new[]{true,false})
                {
                    var point=resolver.IntersectPlanePoint(geometry.Radius(state.Stance)+(hit?-.0004:.0004),0,state.Stance);
                    view.Ledger.Clear();view.Ledger.TryObserve(actor,tick,state.CompletedBouts,state.RangeLane,state.ShotsFired,state.Stance,point.Hit?ShotOutcome.TargetClosed:ShotOutcome.Miss,point,out _);
                    yield return Capture(1600,900,"ru",0,hit?"layout-edge-hit":"layout-edge-miss");
                    Assert.That(hud.Root.Q<Label>("shot_impact_status").text,Is.EqualTo(hit?"Габарит · попадание":"Промах у кромки"));
                }
                Assert.That(runner.Field.Tick.Value,Is.EqualTo(tick),"Native layout/capture must not advance the shooting clock.");
                IEnumerator Capture(int width,int height,string locale,int textSize,string name="hud")
                {
                    var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);var target=new RenderTexture(width,height,0);target.Create();panel.targetTexture=target;Texture2D pixels=null;var previous=RenderTexture.active;
                    try
                    {
                        hud.SetPreferences(locale,textSize,"Alex");for(int i=0;i<4;i++)yield return null;
                        var top=hud.Root.Q("timing_panel").worldBound;var bottom=hud.Root.Q("shooting_panel").worldBound;var condition=hud.Root.Q("condition_panel").worldBound;var hint=hud.Root.Q("help_hint").worldBound;
                        Assert.That(bottom.yMin,Is.GreaterThan(top.yMax+12));Assert.That(bottom.xMax,Is.LessThan(hud.Root.worldBound.center.x));
                        float area=(top.width*top.height+bottom.width*bottom.height+condition.width*condition.height+hint.width*hint.height)/(hud.Root.worldBound.width*hud.Root.worldBound.height);
                        File.WriteAllText(Output+name+"-"+width+"-"+locale+"-coverage.txt",area.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Output+name+"-"+width+"-"+locale+".png",pixels.EncodeToPNG());
                        Assert.That(area,Is.LessThan(textSize==0?.10f:.17f),"The accepted compact HUD coverage must survive the new shot evidence.");
                    }
                    finally{panel.targetTexture=null;RenderTexture.active=previous;if(pixels!=null)Object.Destroy(pixels);Object.Destroy(target);}
                }
            }
            finally{Object.DestroyImmediate(root);}
            yield return null;
        }
    }
}

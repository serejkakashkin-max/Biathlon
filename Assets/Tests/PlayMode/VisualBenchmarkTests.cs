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
    public class VisualBenchmarkTests
    {
        [Test] public void FinishedEntrantsRemainVisibleWithIndependentPoseTimeAndUnchangedResult()
        {
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            var runner=ChampionshipRaceFactory.Create(Read,n=>Resources.Load<TextAsset>("Biathlon/Tracks/"+Path.GetFileNameWithoutExtension(n)).text,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var root=new GameObject("finish test");
            try
            {
                var feed=root.AddComponent<RacePresentationFeed>();var view=root.AddComponent<AthleteProxyPresentation>();view.Initialize(feed,TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"),runner.Field.Track));
                for(int i=0;i<30000&&!runner.Field.IsComplete;i++)runner.Step();Assert.That(runner.Field.IsComplete,Is.True);var result=runner.Field.Result.ToJson();feed.Publish(RacePresentationSnapshot.Capture(runner.Field));view.Render(1,0);
                var id=feed.Current.Get(0).AthleteId;var proxy=view.GetProxy(id);Assert.That(proxy.gameObject.activeSelf,Is.True);Assert.That(view.GetProxy(feed.Current.Get(1).AthleteId).gameObject.activeSelf,Is.True);
                Assert.That(Vector3.Distance(proxy.position,view.GetProxy(feed.Current.Get(1).AthleteId).position),Is.GreaterThan(1));
                var adapter=proxy.GetComponent<AthleteAnimationPresentation>();double before=adapter.LastFrame.TimeSeconds;var placement=proxy.position;view.Render(1,3);Assert.That(adapter.LastFrame.TimeSeconds-before,Is.EqualTo(3).Within(.001));Assert.That(proxy.position,Is.EqualTo(placement));
                var geometry=TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"),runner.Field.Track);var position=feed.Position(0,1);var route=geometry.Get(position.LoopId);
                double stagedDistance=position.LocalMeters+FinishStagingSettings.Load().Offset(0).z;
                double grade=VisualRouteSurface.Grade(route,stagedDistance,GroundContactSettings.Load().sample_half_span_m);
                Assert.That(Math.Abs(grade),Is.GreaterThan(.001),"Fixture must exercise a sloping finish.");
                Assert.That(proxy.position.y,Is.EqualTo(route.Sample(stagedDistance).Y+1).Within(.0001));
                Assert.That(adapter.LastFrame.PresentationGrade,Is.EqualTo(grade));
                Assert.That(Vector3.Distance(adapter.Rig.GetComponent<AthleteGroundContact>().SurfaceNormal,proxy.TransformDirection(new Vector3(0,1,-(float)grade).normalized)),Is.LessThan(.0001));
                Assert.That(adapter.Rig.GetComponent<AthleteLocomotionDriver>().FinishWeight,Is.EqualTo(1).Within(.001));Assert.That(runner.Field.Result.ToJson(),Is.EqualTo(result));Assert.That(feed.Current.Tick,Is.EqualTo(runner.Field.Tick.Value));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void TerrainCrestContactPlaneErrorIsBoundedAtSkiLengthScale()
        {
            foreach(float z in new[]{-40f,-31f,-30f,-29f,-10f})
            {
                var p=new Vector3(0,0,z);float height=VisualBenchmarkAthletes.Height(p),grade=VisualBenchmarkAthletes.Grade(p);
                foreach(float delta in new[]{-1.1f,0,1.1f})Assert.That(Mathf.Abs(height+grade*delta-VisualBenchmarkAthletes.Height(p+Vector3.forward*delta)),Is.LessThan(.045f));
            }
        }
#if UNITY_EDITOR
        [UnityTest] public IEnumerator GeneratedBenchmarkIntegratesTenObservedAthletesAndRestoresSceneState()
        {
            var fog=RenderSettings.fogColor;
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            var p=Object.FindFirstObjectByType<VisualBenchmarkPresentation>();Assert.That(p,Is.Not.Null);Assert.That(p.Ready,Is.True);p.ShowControls=false;
            Assert.That(p.View.isActiveAndEnabled,Is.True,"Normal benchmark startup must enable its window camera.");
            Assert.That(p.View.targetTexture,Is.Null,"Interactive review renders to the window.");
            int rendered=0;
            void Observe(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera){if(camera==p.View)rendered++;}
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=Observe;
            try{yield return null;yield return null;Assert.That(rendered,Is.GreaterThan(0),"URP must actually render the interactive benchmark camera.");}
            finally{UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Observe;}
            Assert.That(p.Athletes.Count,Is.EqualTo(10));long source=p.Athletes.SourceTick;Assert.That(source,Is.EqualTo(400));
            foreach(string shot in new[]{"sector","athlete","rail"}){p.Shot=shot;p.Seek(8);Assert.That(p.Athletes.MaximumFootResidual,Is.LessThan(.002f));Assert.That(p.Athletes.SourceTick,Is.EqualTo(source));}
            foreach(string tier in new[]{"low","balanced","high"}){p.Tier=tier;p.RenderFrame(8);Assert.That(p.VisibleSpectators,Is.GreaterThan(0));}
            p.Shot="sector";p.RenderFrame(8);Assert.That(p.VisibleTrees,Is.GreaterThan(0));foreach(var root in scene.GetRootGameObjects())foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())Assert.That(mesh.sharedMesh,Is.Not.Null);
            p.Shot="athlete";p.Seek(8);for(int i=0;i<20;i++)p.RenderFrame(8);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<60;i++)p.RenderFrame(8);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
            yield return SceneManager.UnloadSceneAsync(scene);Assert.That(RenderSettings.fogColor,Is.EqualTo(fog));
        }
#endif
    }
}

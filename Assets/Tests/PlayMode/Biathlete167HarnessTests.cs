#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167HarnessTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        [Serializable] private sealed class OcclusionRow{public int pose,worstVertex,worstTriangle;public string state;public float phase,maximum_occlusion_m;}
        [Serializable] private sealed class OcclusionReport{public int schema_version=1;public string id="m167_actual_imported_rear_ribbon_occlusion";public float maximum_occlusion_m;public List<OcclusionRow> rows;}
        [TestCase(false),TestCase(true)]
        public void EntireSuspensionEndSectionsRemainAttachedToTheActualWebbingAndStock(bool animate)
        {
            var rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();
            var baked=new Mesh();
            try
            {
                var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
                var mesh=skin.sharedMesh;var positions=mesh.vertices;var weights=mesh.boneWeights;var uv=mesh.uv;
                int webbing=Array.FindIndex(skin.sharedMaterials,m=>m.name=="PULSE_Rifle_Carry_Webbing");
                int suspension=Array.FindIndex(skin.sharedMaterials,m=>m.name=="PULSE_Rifle_Harness_Suspension");
                int carry=Array.IndexOf(skin.bones,rig.Socket(Biathlete167Socket.RifleBack).parent);
                Assert.That(Math.Min(webbing,suspension),Is.GreaterThanOrEqualTo(0));Assert.That(carry,Is.GreaterThanOrEqualTo(0));
                var triangles=mesh.GetTriangles(suspension);var representatives=new Dictionary<Vector3Int,int>();var weld=new Dictionary<int,int>();var graph=new Dictionary<int,HashSet<int>>();
                foreach(int i in triangles.Distinct())
                {
                    var key=Vector3Int.RoundToInt(positions[i]*1000000);if(!representatives.TryGetValue(key,out int id)){id=i;representatives.Add(key,id);graph[id]=new HashSet<int>();}weld[i]=id;
                }
                for(int i=0;i<triangles.Length;i+=3)for(int j=0;j<3;j++)
                {int a=weld[triangles[i+j]],b=weld[triangles[i+(j+1)%3]];graph[a].Add(b);graph[b].Add(a);}
                float CarryWeight(int index)
                {var w=weights[index];return(w.boneIndex0==carry?w.weight0:0)+(w.boneIndex1==carry?w.weight1:0)+(w.boneIndex2==carry?w.weight2:0)+(w.boneIndex3==carry?w.weight3:0);}
                var remaining=new HashSet<int>(graph.Keys);var sections=new List<(int[] shoulder,int[] stock)>();
                while(remaining.Count>0)
                {
                    var component=new List<int>();var pending=new Stack<int>();pending.Push(remaining.First());
                    while(pending.Count>0){int v=pending.Pop();if(!remaining.Remove(v))continue;component.Add(v);foreach(int n in graph[v])pending.Push(n);}
                    // Authored longitudinal UV coordinates identify the actual
                    // end sections even when interior rows share a torso bone.
                    float minimum=component.Min(i=>uv[i].y),maximum=component.Max(i=>uv[i].y);var start=component.Where(i=>Mathf.Abs(uv[i].y-minimum)<.00001f).ToArray();var end=component.Where(i=>Mathf.Abs(uv[i].y-maximum)<.00001f).ToArray();
                    if(start.Average(CarryWeight)>end.Average(CarryWeight)){var swap=start;start=end;end=swap;}
                    Assert.That(end.All(i=>CarryWeight(i)>.99999f),Is.True,$"End section uv{minimum}/{maximum}: shoulder [{string.Join(",",start.Select(CarryWeight))}], stock [{string.Join(",",end.Select(CarryWeight))}].");
                    Assert.That(start.Length,Is.EqualTo(4),"Measure all four actual end corners, not just the midpoint.");Assert.That(end.Length,Is.EqualTo(4));sections.Add((start,end));
                }
                Assert.That(sections.Count,Is.EqualTo(4));
                var webTriangles=mesh.GetTriangles(webbing);var rifleMesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;
                int wood=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="PULSE_Natural_Rifle_Walnut");Assert.That(wood,Is.GreaterThanOrEqualTo(0));
                var stockTriangles=rifleMesh.GetTriangles(wood);var stockLocal=rifleMesh.vertices;
                using(var motion=new Biathlete167MainMotion(rig))
                {
                    var states=(SkiMotionState[])Enum.GetValues(typeof(SkiMotionState));int count=animate?states.Length*4:0;
                    for(int pose=-1;pose<count;pose++)
                    {
                        if(pose<0)rig.SetPose(0);else{motion.State=states[pose/4];motion.Evaluate((pose%4)/4f);}
                        skin.BakeMesh(baked);var body=baked.vertices;for(int i=0;i<body.Length;i++)body[i]=skin.transform.TransformPoint(body[i]);
                        var stock=stockLocal.Select(p=>rig.Rifle.TransformPoint(p)).ToArray();
                        foreach(var section in sections)
                        {
                            foreach(int i in section.shoulder)Assert.That(Distance(body[i],body,webTriangles),Is.LessThan(.004f),$"Visible shoulder seam breaks at pose{pose}, corner{i}.");
                            foreach(int i in section.stock)Assert.That(Distance(body[i],stock,stockTriangles),Is.LessThan(.004f),$"Visible stock seam breaks at pose{pose}, corner{i}.");
                        }
                    }
                }
            }finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void RearSuspensionDoesNotDisappearUnderTheActualGarmentInProductionPoses()
        {
            var rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();
            var baked=new Mesh();var cloth=new Mesh();var probe=new GameObject("QA garment occlusion probe");
            try
            {
                var collider=probe.AddComponent<MeshCollider>();var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();var mesh=skin.sharedMesh;
                var boneWeights=mesh.boneWeights;
                float ArmWeight(int vertex)
                {
                    var w=boneWeights[vertex];float total=0;
                    foreach(var pair in new[]{(w.boneIndex0,w.weight0),(w.boneIndex1,w.weight1),(w.boneIndex2,w.weight2),(w.boneIndex3,w.weight3)})
                        if(pair.Item2>0&&(skin.bones[pair.Item1]==rig.Bone(HumanBodyBones.LeftUpperArm)||skin.bones[pair.Item1]==rig.Bone(HumanBodyBones.RightUpperArm)))total+=pair.Item2;
                    return total;
                }
                var garmentTriangles=new List<int>();
                for(int material=0;material<skin.sharedMaterials.Length;material++)
                {
                    string name=skin.sharedMaterials[material].name;if(name!="PULSE_Bib_Printed_Textile"&&name!="PULSE_Natural_Tailored_Textile")continue;
                    var source=mesh.GetTriangles(material);
                    for(int i=0;i<source.Length;i+=3)
                    {
                        // A lowered arm can legitimately cover a rear attachment
                        // from this camera direction. Keep bib/torso collision;
                        // exclude triangles driven primarily by an upper arm.
                        if(name=="PULSE_Natural_Tailored_Textile"&&(ArmWeight(source[i])+ArmWeight(source[i+1])+ArmWeight(source[i+2]))/3>.5f)continue;
                        garmentTriangles.Add(source[i]);garmentTriangles.Add(source[i+1]);garmentTriangles.Add(source[i+2]);
                    }
                }
                var clothTriangles=garmentTriangles.ToArray();
                int suspension=Array.FindIndex(skin.sharedMaterials,m=>m.name=="PULSE_Rifle_Harness_Suspension");Assert.That(suspension,Is.GreaterThanOrEqualTo(0));var vertices=mesh.GetTriangles(suspension).Distinct().ToArray();
                var evidence=new List<OcclusionRow>();float maximum=0;
                using(var motion=new Biathlete167MainMotion(rig))
                {
                    var states=(SkiMotionState[])Enum.GetValues(typeof(SkiMotionState));
                    for(int pose=-1;pose<states.Length*4;pose++)
                    {
                        if(pose<0)rig.SetPose(0);else{motion.State=states[pose/4];motion.Evaluate((pose%4)/4f);}
                        skin.BakeMesh(baked);var points=baked.vertices;for(int i=0;i<points.Length;i++)points[i]=skin.transform.TransformPoint(points[i]);
                        collider.sharedMesh=null;cloth.Clear();cloth.vertices=points;cloth.triangles=clothTriangles;collider.sharedMesh=cloth;
                        float poseMaximum=0;int worstVertex=-1,worstTriangle=-1;
                        foreach(int i in vertices)
                        {
                            var ray=new Ray(points[i]-rig.transform.forward*.5f,rig.transform.forward);
                            if(collider.Raycast(ray,out var hit,.5f)&&.5f-hit.distance>poseMaximum){poseMaximum=.5f-hit.distance;worstVertex=i;worstTriangle=hit.triangleIndex;}
                        }
                        evidence.Add(new OcclusionRow{pose=pose,state=pose<0?"Rest":states[pose/4].ToString(),phase=pose<0?0:(pose%4)/4f,maximum_occlusion_m=poseMaximum,worstVertex=worstVertex,worstTriangle=worstTriangle});maximum=Mathf.Max(maximum,poseMaximum);
                    }
                }
                Directory.CreateDirectory("artifacts/m16-7-harness-refit-20261106");File.WriteAllText("artifacts/m16-7-harness-refit-20261106/native-occlusion-probe.json",JsonUtility.ToJson(new OcclusionReport{maximum_occlusion_m=maximum,rows=evidence},true));
                Assert.That(maximum,Is.LessThan(.0003f),"Actual rear ribbon must remain outside the garment across the sampled production poses.");
            }finally{Object.DestroyImmediate(probe);Object.DestroyImmediate(cloth);Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        private static float Distance(Vector3 p,Vector3[] v,int[] triangles)
        {
            float minimum=float.PositiveInfinity;
            for(int i=0;i<triangles.Length;i+=3)minimum=Mathf.Min(minimum,(p-Closest(p,v[triangles[i]],v[triangles[i+1]],v[triangles[i+2]])).sqrMagnitude);
            return Mathf.Sqrt(minimum);
        }
        private static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;
            var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
            var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
            float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
            float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-20f)return a;return a+ab*(vb/sum)+ac*(vc/sum);
        }
        [UnityTest] public IEnumerator StudioRendersHarnessFrontBackAndTuckDetails()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();
            if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            string folder="artifacts/m16-7-harness-refit-20261106/native";Directory.CreateDirectory(folder);studio.Motion.Stop();studio.Rig.GetComponent<Biathlete167Outfit>().ApplyNation("NOR");
            var go=new GameObject("Harness inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=35;
            try{using(var target=new BenchmarkRenderTarget(1300,1500))
            {
                foreach(bool tuck in new[]{false,true})
                {
                    if(tuck){studio.Motion.MainReference.State=SkiMotionState.Tuck;studio.Motion.EvaluateCycle(.25f);}else studio.Rig.SetPose(0);
                    foreach(var view in new[]{("front",new Vector3(0,1.42f,1.30f),new Vector3(0,1.35f,0)),("rear",new Vector3(0,1.42f,-1.4f),new Vector3(0,1.34f,-.1f)),("rear-three-quarter",new Vector3(.8f,1.42f,-1.1f),new Vector3(.06f,1.35f,-.1f)),("side",new Vector3(1.2f,1.35f,0),new Vector3(.05f,1.30f,0))})
                    {
                        var shift=tuck?new Vector3(0,-.35f,.33f):Vector3.zero;
                        camera.transform.position=studio.Rig.transform.TransformPoint(view.Item2+shift);camera.transform.LookAt(studio.Rig.transform.TransformPoint(view.Item3+shift));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+(tuck?"tuck-":"rest-")+view.Item1+".png");
                    }
                }
            }}finally{Object.Destroy(go);studio.Motion.Stop();studio.Rig.SetPose(0);studio.SetView(0);}
        }
    }
}
#endif

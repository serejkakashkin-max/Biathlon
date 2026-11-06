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
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167RangeFitTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence="artifacts/m16-7-range-fit-20261106";
        private Biathlete167Rig rig;private Biathlete167Motion motion;
        [SetUp] public void Create(){rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);}
        [TearDown] public void Cleanup(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}
        [TestCase("\"arm_clearance_angles\": 48","\"arm_clearance_angles\": 0"),TestCase("\"hand_fit_iterations\": 32","\"hand_fit_iterations\": 1000"),TestCase("\"setup_pose_blend_end\": 0.15","\"setup_pose_blend_end\": 0")]
        public void InvalidRangeDefinitionsAreRejectedBeforeCreatingRuntimeMeshes(string valid,string invalid)
        {
            var definition=rig.PerformanceDefinition;Assert.That(definition.text,Does.Contain(valid));var broken=new TextAsset(definition.text.Replace(valid,invalid));
            try{rig.ConfigurePerformance(broken,rig.PerformanceLocomotion);Assert.Throws<ArgumentException>(()=>new Biathlete167SportMotion(rig,motion.MainReference));}
            finally{rig.ConfigurePerformance(definition,rig.PerformanceLocomotion);Object.DestroyImmediate(broken);}
        }
        [TestCase(Biathlete167Action.Standing,.25f),TestCase(Biathlete167Action.Prone,.25f),TestCase(Biathlete167Action.EnterStanding,.6f),TestCase(Biathlete167Action.EnterProne,.75f)]
        public void ActualRifleStructuralEdgesDoNotCrossTheSkinnedBody(Biathlete167Action action,float phase)
        {
            motion.EvaluateAction(action,phase);var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
            var baked=new Mesh();var body=new Mesh{indexFormat=IndexFormat.UInt32};var go=new GameObject("Actual body intersection probe");go.transform.SetParent(skin.transform,false);
            try
            {
                skin.BakeMesh(baked);var triangles=new List<int>();
                for(int sub=0;sub<skin.sharedMaterials.Length;sub++)if(skin.sharedMaterials[sub].name is "Anatomical_Skin_CC0_2K" or "PULSE_Natural_Tailored_Textile" or "PULSE_Bib_Printed_Textile")triangles.AddRange(skin.sharedMesh.GetTriangles(sub));
                Assert.That(triangles.Count,Is.GreaterThan(1000));body.vertices=baked.vertices;body.triangles=triangles.ToArray();var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=body;
                var mesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;var materials=rig.Rifle.GetComponent<Renderer>().sharedMaterials;var vertices=mesh.vertices;var edges=new HashSet<ulong>();int hits=0;Vector3 witness=default;var hitBones=new Dictionary<string,int>();var weights=skin.sharedMesh.boneWeights;
                for(int sub=0;sub<materials.Length;sub++)if(materials[sub].name is "PULSE_Natural_Rifle_Walnut" or "PULSE_Rifle_Barrel_Satin")
                {
                    var indices=mesh.GetTriangles(sub);
                    for(int i=0;i<indices.Length;i+=3)for(int edge=0;edge<3;edge++)
                    {
                        int a=indices[i+edge],b=indices[i+(edge+1)%3];ulong key=((ulong)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);if(!edges.Add(key))continue;
                        // The butt may touch its shoulder pocket, but the stock
                        // further forward and barrel must remain outside cloth/body.
                        if(Mathf.Max(vertices[a].z,vertices[b].z)<-.475f)continue;
                        var start=rig.Rifle.TransformPoint(vertices[a]);var end=rig.Rifle.TransformPoint(vertices[b]);float length=Vector3.Distance(start,end);if(length<.002f)continue;
                        var direction=(end-start)/length;
                        if(collider.Raycast(new Ray(start+direction*.0005f,direction),out var hit,length-.001f)||collider.Raycast(new Ray(end-direction*.0005f,-direction),out hit,length-.001f))
                        {hits++;witness=hit.point;var w=weights[triangles[hit.triangleIndex*3]];int index=w.boneIndex0;float best=w.weight0;if(w.weight1>best){index=w.boneIndex1;best=w.weight1;}if(w.weight2>best){index=w.boneIndex2;best=w.weight2;}if(w.weight3>best)index=w.boneIndex3;string bone=skin.bones[index].name;hitBones[bone]=hitBones.TryGetValue(bone,out int count)?count+1:1;}
                    }
                }
                string distribution=string.Join(", ",hitBones.Select(x=>x.Key+":"+x.Value));Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/intersection-"+action+".txt","phase="+phase+" structural_edge_crossings="+hits+" witness="+witness+" bones="+distribution+"\n");
                Assert.That(hits,Is.Zero,action+"/"+phase+" actual stock/barrel crosses the skinned body at "+witness+" bones="+distribution);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(body);Object.DestroyImmediate(baked);}
        }
        [TestCase(Biathlete167Eyewear.Glasses),TestCase(Biathlete167Eyewear.Mask)]
        public void SelectedLensIsRaisedBeforeSettledShootingAndReturnsForSkiing(Biathlete167Eyewear choice)
        {
            rig.SetEyewear(choice);motion.EvaluateCycle(0);var head=rig.Bone(HumanBodyBones.Head);string material=choice==Biathlete167Eyewear.Glasses?"Amber_Mirror":"PULSE_Mask_Amber_Lens";
            var lens=rig.GetComponentsInChildren<Renderer>(true).Single(r=>r.sharedMaterials.Any(m=>m.name==material));var mesh=lens.GetComponent<MeshFilter>().sharedMesh;var points=mesh.vertices;int sub=Array.FindIndex(lens.sharedMaterials,m=>m.name==material);var ids=mesh.GetTriangles(sub).Distinct().ToArray();var centre=ids.Aggregate(Vector3.zero,(sum,i)=>sum+points[i])/ids.Length;
            var before=head.InverseTransformPoint(lens.transform.TransformPoint(centre));
            motion.EvaluateAction(Biathlete167Action.Standing,0);var raised=head.InverseTransformPoint(lens.transform.TransformPoint(centre));
            Assert.That(raised.y-before.y,Is.GreaterThan(.065f),"Lift the selected lens onto the cap before aiming.");
            motion.EvaluateCycle(.4f);Assert.That(Vector3.Distance(head.InverseTransformPoint(lens.transform.TransformPoint(centre)),before),Is.LessThan(.0001f),"Restore eyewear for skiing.");
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne),TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone),TestCase(Biathlete167Action.RangeEntry)]
        public void CompleteSetupKeepsTheActualStockAndBarrelOutsideTheBody(Biathlete167Action action)
        {for(int i=0;i<=128;i++)ActualRifleStructuralEdgesDoNotCrossTheSkinnedBody(action,i/128f);}
        [Test] public void EndOfApproachMatchesTheStartOfBothSetups()
        {
            var ids=new[]{HumanBodyBones.Hips,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
            motion.EvaluateAction(Biathlete167Action.RangeEntry,1);var positions=ids.Select(x=>rig.Bone(x).position).ToArray();var weapon=rig.Rifle.position;var glasses=rig.EyewearRoot(Biathlete167Eyewear.Glasses).position;
            foreach(var action in new[]{Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne})
            {motion.EvaluateAction(action,0);for(int i=0;i<ids.Length;i++)Assert.That(Vector3.Distance(positions[i],rig.Bone(ids[i]).position),Is.LessThan(.001f),ids[i]+" approach/setup seam");Assert.That(Vector3.Distance(weapon,rig.Rifle.position),Is.LessThan(.001f));Assert.That(Vector3.Distance(glasses,rig.EyewearRoot(Biathlete167Eyewear.Glasses).position),Is.LessThan(.001f));}
        }
        [Test] public void SetupFitsEngagedPalmsWithoutLengtheningTheNeck()
        {
            float neckLength=Vector3.Distance(rig.Bone(HumanBodyBones.Neck).position,rig.Bone(HumanBodyBones.Head).position);
            foreach(var action in new[]{Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne})for(int i=0;i<=128;i++)
            {motion.EvaluateAction(action,i/128f);Assert.That(motion.Performance.MaximumPalmFitResidualMeters,Is.LessThan(.001f),action+" "+i+" engaged palm");Assert.That(Vector3.Distance(rig.Bone(HumanBodyBones.Neck).position,rig.Bone(HumanBodyBones.Head).position),Is.EqualTo(neckLength).Within(.00003f),"Settle the head through rotation, never stretch the neck.");}
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void SetupDoesNotSnapTheArmsOrWeapon(Biathlete167Action action)
        {
            var ids=new[]{HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand};var previous=new Vector3[ids.Length];motion.EvaluateAction(action,0);for(int i=0;i<ids.Length;i++)previous[i]=rig.Bone(ids[i]).position;var weapon=rig.Rifle.position;
            for(int sample=1;sample<=256;sample++)
            {motion.EvaluateAction(action,sample/256f);for(int i=0;i<ids.Length;i++){var current=rig.Bone(ids[i]).position;Assert.That(Vector3.Distance(previous[i],current),Is.LessThan(.05f),action+" phase "+sample+" "+ids[i]);previous[i]=current;}Assert.That(Vector3.Distance(weapon,rig.Rifle.position),Is.LessThan(.04f),"No weapon teleport");weapon=rig.Rifle.position;}
        }
        [Serializable] private sealed class Cost {public string action;public double mean_ms,p95_ms,maximum_ms;}
        [Serializable] private sealed class Costs {public int schema_version=1;public string id="m167_editor_range_sampling_costs";public int samples_per_action=256;public Cost[] actions;}
        [Test] public void RecordWarmRangeSamplingCosts()
        {
            var costs=new List<Cost>();var stopwatch=new System.Diagnostics.Stopwatch();var times=new double[256];
            foreach(var action in new[]{Biathlete167Action.RangeEntry,Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne,Biathlete167Action.Standing,Biathlete167Action.Prone})
            {
                for(int i=0;i<16;i++)motion.EvaluateAction(action,i/16f);double sum=0;
                for(int i=0;i<times.Length;i++){stopwatch.Restart();motion.EvaluateAction(action,i/255f);stopwatch.Stop();times[i]=stopwatch.Elapsed.TotalMilliseconds;sum+=times[i];}
                Array.Sort(times);costs.Add(new Cost{action=action.ToString(),mean_ms=sum/times.Length,p95_ms=times[242],maximum_ms=times[255]});
            }
            Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/sampling-costs.json",JsonUtility.ToJson(new Costs{actions=costs.ToArray()},true)+"\n");
        }
        [UnityTest] public IEnumerator CaptureActualRangeFit()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();studio.Rig.gameObject.SetActive(false);var go=new GameObject("Range fit inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;Directory.CreateDirectory(Evidence+"/native");
            try{using(var target=new BenchmarkRenderTarget(1200,1000))
            {
                foreach(var action in new[]{Biathlete167Action.RangeEntry,Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne,Biathlete167Action.Standing,Biathlete167Action.Prone})foreach(float phase in new[]{.2f,.5f,.75f,1f})
                {
                    motion.EvaluateAction(action,phase);bool low=action==Biathlete167Action.Prone||(action==Biathlete167Action.EnterProne&&phase>.7f);var focus=new Vector3(0,low?.35f:.96f,0);
                    foreach(var angle in new[]{("side",new Vector3(3.6f,.15f,0)),("front",new Vector3(.15f,.15f,3.8f))})
                    {camera.transform.position=rig.transform.TransformPoint(focus+angle.Item2);camera.transform.LookAt(rig.transform.TransformPoint(focus));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-"+angle.Item1+".png");}
                }
                foreach(var eyewear in new[]{Biathlete167Eyewear.Glasses,Biathlete167Eyewear.Mask})
                {rig.SetEyewear(eyewear);motion.EvaluateAction(Biathlete167Action.Standing,.3f);var focus=rig.Bone(HumanBodyBones.Head).position;camera.transform.position=focus+new Vector3(.45f,.1f,.75f);camera.transform.LookAt(focus);yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/raised-"+eyewear+".png");}
            }}finally{studio.Rig.gameObject.SetActive(true);Object.Destroy(go);motion.Stop();}
        }
    }
}
#endif

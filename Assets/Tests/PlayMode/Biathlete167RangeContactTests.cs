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
    public sealed class Biathlete167RangeContactTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence="artifacts/m16-7-range-contact-20261107";
        private Biathlete167Rig rig;private Biathlete167Motion motion;
        private readonly Quaternion[] neutralHands=new Quaternion[2];private readonly Transform[] hands=new Transform[2],poles=new Transform[2];
        private readonly Vector3[] poleGrips=new Vector3[2],poleTips=new Vector3[2];private SkinnedMeshRenderer skin;private int[] rightPupil;
        private Vector3 rearSight,frontSight;
        [SetUp] public void Create()
        {
            rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);
            for(int i=0;i<2;i++){hands[i]=rig.Bone(i==0?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);neutralHands[i]=Quaternion.Inverse(hands[i].parent.rotation)*hands[i].rotation;var mesh=hands[i].GetComponentsInChildren<MeshFilter>(true).OrderByDescending(p=>p.sharedMesh.bounds.size.sqrMagnitude).First();poles[i]=mesh.transform;poleGrips[i]=poles[i].InverseTransformPoint(rig.GripContact(i==1).position);poleTips[i]=mesh.sharedMesh.vertices.OrderByDescending(v=>(v-poleGrips[i]).sqrMagnitude).First();}
            skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();int pupil=Array.FindIndex(skin.sharedMaterials,m=>m.name=="Eye_Pupil");Assert.That(pupil,Is.GreaterThanOrEqualTo(0));var vertices=skin.sharedMesh.vertices;rightPupil=skin.sharedMesh.GetTriangles(pupil).Distinct().Where(i=>rig.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i])).x>0).ToArray();Assert.That(rightPupil.Length,Is.GreaterThan(20));
            ReadActualSightRings(out rearSight,out frontSight);
        }
        [TearDown] public void Cleanup(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}
        [TestCase("\"maximum_forearm_pronation_degrees\": 180","\"maximum_forearm_pronation_degrees\": 0"),TestCase("\"maximum_wrist_twist_degrees\": 15","\"maximum_wrist_twist_degrees\": 45"),TestCase("\"sight_height_settle_end\": 0.95","\"sight_height_settle_end\": 0")]
        public void InvalidRangeContactDefinitionsAreRejected(string valid,string invalid)
        {
            var original=rig.PerformanceDefinition;Assert.That(original.text,Does.Contain(valid));var broken=new TextAsset(original.text.Replace(valid,invalid));
            try{rig.ConfigurePerformance(broken,rig.PerformanceLocomotion);Assert.Throws<ArgumentException>(()=>new Biathlete167SportMotion(rig,motion.MainReference));}
            finally{rig.ConfigurePerformance(original,rig.PerformanceLocomotion);Object.DestroyImmediate(broken);}
        }
        private void ReadActualSightRings(out Vector3 rear,out Vector3 front)
        {
            var mesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;int black=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="Rifle_Padded_Composite");var triangles=mesh.GetTriangles(black);var ids=new Dictionary<Vector3Int,int>();var parents=new List<int>();var points=new List<Vector3>();
            int Id(int i){var p=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));if(ids.TryGetValue(key,out int found))return found;int next=parents.Count;parents.Add(next);points.Add(p);ids[key]=next;return next;}
            int Find(int i){while(parents[i]!=i){parents[i]=parents[parents[i]];i=parents[i];}return i;}
            for(int i=0;i<triangles.Length;i+=3){int a=Id(triangles[i]);parents[Find(Id(triangles[i+1]))]=Find(a);parents[Find(Id(triangles[i+2]))]=Find(a);}
            var rings=new List<Vector3>();foreach(var group in Enumerable.Range(0,points.Count).GroupBy(Find))
            {if(group.Count()<128)continue;var bounds=new Bounds(points[group.First()],Vector3.zero);foreach(int i in group)bounds.Encapsulate(points[i]);if(bounds.size.x>.025f&&bounds.size.x<.04f&&bounds.size.y>.025f&&bounds.size.y<.04f&&bounds.size.z<.04f)rings.Add(bounds.center);}
            Assert.That(rings.Count,Is.EqualTo(2),"Find the two actual hollow sight-ring components independently of animation fitting data.");rings.Sort((a,b)=>a.z.CompareTo(b.z));rear=rings[0];front=rings[1];
        }
        private float WristTwist(int side)
        {
            var hand=hands[side];var axis=(hand.position-hand.parent.position).normalized;var delta=hand.rotation*Quaternion.Inverse(hand.parent.rotation*neutralHands[side]);float sine=Vector3.Dot(new Vector3(delta.x,delta.y,delta.z),axis);return Mathf.Abs(Mathf.DeltaAngle(0,2*Mathf.Atan2(sine,delta.w)*Mathf.Rad2Deg));
        }
        [Test] public void ApproachKeepsHeldPoleTipsBelowTheHandsUntilRelease()
        {
            for(int phase=0;phase<=64;phase++)
            {motion.EvaluateAction(Biathlete167Action.RangeEntry,phase/64f*.3f);for(int side=0;side<2;side++){var grip=poles[side].TransformPoint(poleGrips[side]);var tip=poles[side].TransformPoint(poleTips[side]);Assert.That(Vector3.Dot(tip-grip,rig.transform.up),Is.LessThan(-.25f),"Approach phase="+phase+" pole "+side+" points up beside the torso.");Assert.That(Vector3.Distance(grip,rig.GripContact(side==1).position),Is.LessThan(.001f),"Keep the actual handle in the palm until pole release.");}}
        }
        [TestCase(Biathlete167Action.RangeEntry),TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne),TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        public void WristsDoNotTakeTheForearmsAxialPronation(Biathlete167Action action)
        {
            for(int phase=0;phase<=128;phase++){motion.EvaluateAction(action,phase/128f);for(int side=0;side<2;side++)Assert.That(WristTwist(side),Is.LessThanOrEqualTo(20.1f),action+" phase="+phase+"/128 side="+side+": distribute axial rotation through the forearm, not the wrist seam.");}
        }
        [TestCase(Biathlete167Action.RangeEntry),TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void ReachingWristRotationsDoNotJumpAcrossQuaternionBranches(Biathlete167Action action)
        {
            motion.EvaluateAction(action,0);var previous=hands.Select(h=>Quaternion.Inverse(h.parent.rotation)*h.rotation).ToArray();
            for(int i=1;i<=256;i++)
            {motion.EvaluateAction(action,i/256f);for(int side=0;side<2;side++){var now=Quaternion.Inverse(hands[side].parent.rotation)*hands[side].rotation;Assert.That(Quaternion.Angle(previous[side],now),Is.LessThan(12f),action+" phase="+i+" wrist="+side+": no sudden relative wrist flip during reaching.");previous[side]=now;}}
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void RemovalHandReachesTheActualBarrelAboveTheShoulder(Biathlete167Action action)
        {
            motion.EvaluateAction(action,.45f);var hand=rig.GripContact(true).position;var shoulder=rig.Bone(HumanBodyBones.RightUpperArm).position;
            Assert.That(Vector3.Dot(hand-shoulder,rig.transform.up),Is.GreaterThan(.08f),"Reach over the shoulder instead of folding the wrist beside the upper arm.");
            var mesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;int sub=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="PULSE_Rifle_Barrel_Satin");var vertices=mesh.vertices;
            float gap=mesh.GetTriangles(sub).Distinct().Min(i=>Vector3.Distance(hand,rig.Rifle.TransformPoint(vertices[i])));
            Assert.That(gap,Is.LessThan(.02f),"The palm must reach actual barrel geometry rather than an arbitrary point in the air.");
        }
        [Test] public void ProneRearApertureIsAtTheDominantPupilsHeight()
        {
            var baked=new Mesh();try{for(int phase=0;phase<=32;phase++)
            {
                motion.EvaluateAction(Biathlete167Action.Prone,phase/32f);skin.BakeMesh(baked);var vertices=baked.vertices;var eye=rightPupil.Aggregate(Vector3.zero,(sum,i)=>sum+skin.transform.TransformPoint(vertices[i]))/rightPupil.Length;
                var rear=rig.Rifle.TransformPoint(rearSight);float error=Mathf.Abs(Vector3.Dot(rear-eye,rig.transform.up));
                Assert.That(error,Is.LessThan(.006f),"Prone aperture/pupil height error="+error+" m. This checks the owner's height defect; lateral cheek/stock fitting remains open.");
            }}finally{Object.DestroyImmediate(baked);}
        }
        [UnityTest] public IEnumerator CaptureActualRangeContacts()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();studio.Rig.gameObject.SetActive(false);var go=new GameObject("Range contact inspection camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;Directory.CreateDirectory(Evidence+"/native");
            try{using(var target=new BenchmarkRenderTarget(1200,1000))foreach(var action in new[]{Biathlete167Action.RangeEntry,Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne,Biathlete167Action.Standing,Biathlete167Action.Prone})foreach(float phase in new[]{.125f,.25f,.5f,.75f,1f})
            {
                motion.EvaluateAction(action,phase);bool low=action==Biathlete167Action.Prone||(action==Biathlete167Action.EnterProne&&phase>.8f);var focus=new Vector3(0,low?.3f:.92f,low?-.1f:0);
                foreach(var view in new[]{("side",new Vector3(4.4f,.25f,0)),("front",new Vector3(.2f,.3f,4.2f)),("overhead",new Vector3(0,4.9f,0))})
                {camera.transform.position=rig.transform.TransformPoint(focus+view.Item2);if(view.Item1=="overhead")camera.transform.rotation=Quaternion.LookRotation(-rig.transform.up,rig.transform.forward);else camera.transform.LookAt(rig.transform.TransformPoint(focus));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-"+view.Item1+".png");}
                if(action!=Biathlete167Action.RangeEntry&&phase>=.25f)
                {var head=rig.Bone(HumanBodyBones.Head).position;camera.transform.position=head+rig.transform.TransformVector(new Vector3(.75f,.1f,.15f));camera.transform.LookAt(head);yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-face.png");}
                if((action==Biathlete167Action.EnterStanding||action==Biathlete167Action.EnterProne)&&phase>=.25f&&phase<=.75f)
                {var hand=rig.Bone(HumanBodyBones.RightHand).position;camera.transform.position=hand+rig.transform.TransformVector(new Vector3(.7f,.2f,-.1f));camera.transform.LookAt(hand);yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-handover.png");}
            }}finally{studio.Rig.gameObject.SetActive(true);Object.Destroy(go);motion.Stop();}
        }
    }
}
#endif

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
    public sealed class Biathlete167RangeAnatomyTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Evidence="artifacts/m16-7-range-anatomy-20261106";
        private Biathlete167Rig rig;private Biathlete167Motion motion;
        [SetUp] public void Create(){rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,true);}
        [TearDown] public void Cleanup(){if(rig!=null)Object.DestroyImmediate(rig.gameObject);}
        [TestCase("\"body_volume_scale\": 1.08","\"body_volume_scale\": 0"),TestCase("\"rifle_hand_reach_end\": 0.45","\"rifle_hand_reach_end\": 0"),TestCase("\"arm_guide_bias\": 0.01","\"arm_guide_bias\": -1"),TestCase("\"standing_right_elbow_curve\"","\"missing_standing_elbow_curve\"")]
        public void InvalidAnatomyDefinitionsFailBeforeCreatingRuntimeEquipment(string valid,string invalid)
        {
            var definition=rig.PerformanceDefinition;Assert.That(definition.text,Does.Contain(valid));var broken=new TextAsset(definition.text.Replace(valid,invalid));
            try{rig.ConfigurePerformance(broken,rig.PerformanceLocomotion);Assert.Throws<ArgumentException>(()=>new Biathlete167SportMotion(rig,motion.MainReference));}
            finally{rig.ConfigurePerformance(definition,rig.PerformanceLocomotion);Object.DestroyImmediate(broken);}
        }
        private static int Dominant(BoneWeight w){int index=w.boneIndex0;float best=w.weight0;if(w.weight1>best){index=w.boneIndex1;best=w.weight1;}if(w.weight2>best){index=w.boneIndex2;best=w.weight2;}if(w.weight3>best)index=w.boneIndex3;return index;}
        private sealed class BodyProbe:IDisposable
        {
            private readonly Biathlete167Rig rig;private readonly SkinnedMeshRenderer skin;private readonly Mesh baked=new Mesh(),body=new Mesh{indexFormat=IndexFormat.UInt32};private readonly GameObject go;private readonly MeshCollider collider;
            private readonly (int a,int b,int side)[] edges;public int Crossings;public Vector3 Witness;
            public BodyProbe(Biathlete167Rig rig)
            {
                this.rig=rig;skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();var bones=skin.bones;var weights=skin.sharedMesh.boneWeights;
                HashSet<int> Set(params HumanBodyBones[] ids)=>new HashSet<int>(ids.Select(id=>Array.IndexOf(bones,rig.Bone(id))).Where(i=>i>=0));
                var core=Set(HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.UpperChest,HumanBodyBones.Neck,HumanBodyBones.Head);
                var arms=new[]{Set(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand),Set(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand)};
                var torso=new List<int>();var armEdges=new List<(int,int,int)>();var keys=new HashSet<ulong>();var dominant=weights.Select(Dominant).ToArray();
                var materials=skin.sharedMaterials;
                for(int sub=0;sub<materials.Length;sub++)if(materials[sub].name is "Anatomical_Skin_CC0_2K" or "PULSE_Natural_Tailored_Textile" or "PULSE_Bib_Printed_Textile")
                {
                    var indices=skin.sharedMesh.GetTriangles(sub);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        int a=indices[i],b=indices[i+1],c=indices[i+2];
                        if(core.Contains(dominant[a])&&core.Contains(dominant[b])&&core.Contains(dominant[c])){torso.Add(a);torso.Add(b);torso.Add(c);}
                        for(int side=0;side<2;side++)if(arms[side].Contains(dominant[a])&&arms[side].Contains(dominant[b])&&arms[side].Contains(dominant[c]))for(int edge=0;edge<3;edge++)
                        {int x=indices[i+edge],y=indices[i+(edge+1)%3];ulong key=((ulong)Math.Min(x,y)<<32)|(uint)Math.Max(x,y);if(keys.Add(key))armEdges.Add((x,y,side));}
                    }
                }
                Assert.That(torso.Count,Is.GreaterThan(1000));Assert.That(armEdges.Count,Is.GreaterThan(100));edges=armEdges.ToArray();skin.BakeMesh(baked);body.vertices=baked.vertices;body.triangles=torso.ToArray();
                go=new GameObject("Actual arm/torso surface probe");go.transform.SetParent(skin.transform,false);collider=go.AddComponent<MeshCollider>();collider.sharedMesh=body;
            }
            public void Sample()
            {
                skin.BakeMesh(baked);var vertices=baked.vertices;collider.sharedMesh=null;body.vertices=vertices;collider.sharedMesh=body;Crossings=0;
                var shoulder=new[]{rig.Bone(HumanBodyBones.LeftUpperArm).position,rig.Bone(HumanBodyBones.RightUpperArm).position};
                foreach(var edge in edges)
                {
                    var a=skin.transform.TransformPoint(vertices[edge.a]);var b=skin.transform.TransformPoint(vertices[edge.b]);
                    // The connected shoulder seam is not an independent body
                    // collision. The remainder of both sleeves is checked.
                    if(Vector3.Distance(a,shoulder[edge.side])<.09f||Vector3.Distance(b,shoulder[edge.side])<.09f)continue;
                    float length=Vector3.Distance(a,b);if(length<.002f)continue;var direction=(b-a)/length;
                    if(collider.Raycast(new Ray(a+direction*.0005f,direction),out var hit,length-.001f)||collider.Raycast(new Ray(b-direction*.0005f,-direction),out hit,length-.001f)){Crossings++;Witness=hit.point;}
                }
            }
            public void Dispose(){Object.DestroyImmediate(go);Object.DestroyImmediate(body);Object.DestroyImmediate(baked);}
        }
        [TestCase(Biathlete167Action.RangeEntry),TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne),TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        [TestCase(Biathlete167Action.Skate), TestCase(Biathlete167Action.Acceleration), TestCase(Biathlete167Action.Uphill)]
        public void SleevesDoNotPassThroughTheActualTorsoDuringMotion(Biathlete167Action action)
        {
            using(var probe=new BodyProbe(rig))for(int i=0;i<=128;i++)
            {motion.EvaluateAction(action,i/128f);probe.Sample();Assert.That(probe.Crossings,Is.Zero,action+" phase="+i+"/128 sleeve/torso crossings at "+probe.Witness);}
        }
        private static Transform Ski(Transform foot)=>foot.GetComponentsInChildren<MeshFilter>(true).OrderByDescending(m=>m.sharedMesh.bounds.size.sqrMagnitude).First().transform;
        [TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        public void ShootingSkisFollowTheReferenceStanceInsteadOfPointingTogetherAtTargets(Biathlete167Action action)
        {
            var skis=new[]{Ski(rig.Bone(HumanBodyBones.LeftFoot)),Ski(rig.Bone(HumanBodyBones.RightFoot))};var localForward=skis.Select(s=>s.InverseTransformDirection(rig.transform.forward)).ToArray();
            motion.EvaluateAction(action,.25f);
            for(int i=0;i<2;i++)
            {var axis=skis[i].TransformDirection(localForward[i]);float yaw=Vector3.SignedAngle(rig.transform.forward,Vector3.ProjectOnPlane(axis,rig.transform.up),rig.transform.up);if(action==Biathlete167Action.Standing)Assert.That(Mathf.Abs(yaw),Is.GreaterThan(35),"Standing skis follow the hips.");else{Assert.That(Mathf.Abs(yaw),Is.InRange(8,22),"Prone keeps a shallow V rather than transverse skis.");Assert.That(Mathf.Sign(yaw),Is.EqualTo(i==0?-1:1),"Both tips face forward and outwards.");}}
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void ShootingSkiChangesKeepTheToeBindingAndLegLengths(Biathlete167Action action)
        {
            var legs=new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot}.Select(rig.Bone).ToArray();
            var lengths=new[]{Vector3.Distance(legs[0].position,legs[1].position),Vector3.Distance(legs[1].position,legs[2].position),Vector3.Distance(legs[3].position,legs[4].position),Vector3.Distance(legs[4].position,legs[5].position)};
            for(int i=0;i<=128;i++)
            {
                motion.EvaluateAction(action,i/128f);Assert.That(motion.Performance.MaximumShootingBindingResidualMeters,Is.LessThan(.001f),action+" phase="+i+"/128 toe detached");
                for(int side=0;side<2;side++)for(int part=0;part<2;part++)Assert.That(Vector3.Distance(legs[side*3+part].position,legs[side*3+part+1].position),Is.EqualTo(lengths[side*2+part]).Within(.00003f),"Do not stretch the shooting leg.");
            }
        }
        [TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        public void SettledSkiBasesRestOnTheFloorAndProneLegsExtendBehindTheHips(Biathlete167Action action)
        {
            var skis=new[]{Ski(rig.Bone(HumanBodyBones.LeftFoot)),Ski(rig.Bone(HumanBodyBones.RightFoot))};var localUp=skis.Select(s=>s.InverseTransformDirection(rig.transform.up)).ToArray();motion.EvaluateAction(action,.25f);
            for(int side=0;side<2;side++)
            {
                float bottom=skis[side].GetComponent<MeshFilter>().sharedMesh.vertices.Min(p=>rig.transform.InverseTransformPoint(skis[side].TransformPoint(p)).y);
                Assert.That(bottom,Is.InRange(0,.004f),action+" actual ski support height");float edge=Vector3.Angle(rig.transform.up,skis[side].TransformDirection(localUp[side]));
                Assert.That(edge,action==Biathlete167Action.Prone?Is.InRange(45,75):Is.LessThan(.1f),"Prone rests on the edge; standing uses flat ski bases.");
                if(action==Biathlete167Action.Prone){var hip=rig.Bone(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);var ankle=rig.Bone(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);Assert.That(Vector3.Dot(hip.position-ankle.position,rig.transform.forward),Is.GreaterThan(.73f),"Avoid a frog-leg crouch when prone.");}
            }
        }
        [Test] public void ProneReferenceHasExtendedLegsWithoutSidewaysKnees()
        {
            motion.EvaluateAction(Biathlete167Action.Prone,.25f);
            for(int side=0;side<2;side++)
            {
                var hip=rig.Bone(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg).position;var knee=rig.Bone(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg).position;var ankle=rig.Bone(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot).position;
                Assert.That(Vector3.Angle(hip-knee,ankle-knee),Is.GreaterThan(150),"The reference extends the legs rather than folding them beside the hips.");
                Assert.That(Mathf.Abs(Vector3.Dot(knee-(hip+ankle)*.5f,rig.transform.right)),Is.LessThan(.035f),"The knee bends towards the mat, not sideways into a frog pose.");
            }
        }
        [TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        public void ActualSkiFootprintsStaySeparateInTheSettledShootingPose(Biathlete167Action action)
        {
            var skis=new[]{Ski(rig.Bone(HumanBodyBones.LeftFoot)),Ski(rig.Bone(HumanBodyBones.RightFoot))};var vertices=skis.Select(s=>s.GetComponent<MeshFilter>().sharedMesh.vertices).ToArray();var forward=skis.Select(s=>s.InverseTransformDirection(rig.transform.forward)).ToArray();
            for(int phase=0;phase<=128;phase++)
            {
                motion.EvaluateAction(action,phase/128f);var points=new[]{new Vector2[vertices[0].Length],new Vector2[vertices[1].Length]};var axes=new List<Vector2>();
                for(int side=0;side<2;side++)
                {
                    for(int i=0;i<vertices[side].Length;i++){var p=rig.transform.InverseTransformPoint(skis[side].TransformPoint(vertices[side][i]));points[side][i]=new Vector2(p.x,p.z);}
                    var direction=rig.transform.InverseTransformDirection(skis[side].TransformDirection(forward[side]));var axis=new Vector2(direction.x,direction.z).normalized;axes.Add(axis);axes.Add(new Vector2(-axis.y,axis.x));
                }
                float separation=float.NegativeInfinity;
                foreach(var axis in axes){float aMin=points[0].Min(p=>Vector2.Dot(p,axis)),aMax=points[0].Max(p=>Vector2.Dot(p,axis)),bMin=points[1].Min(p=>Vector2.Dot(p,axis)),bMax=points[1].Max(p=>Vector2.Dot(p,axis));separation=Mathf.Max(separation,Mathf.Max(bMin-aMax,aMin-bMax));}
                Assert.That(separation,Is.GreaterThan(.005f),action+" phase="+phase+"/128: the actual projected ski footprints cross, including the tails. Parallel or opposite tips are allowed; crossing is not.");
            }
        }
        [TestCase(Biathlete167Action.EnterStanding),TestCase(Biathlete167Action.EnterProne)]
        public void ActualSkiVolumesStaySeparateThroughoutTheSetup(Biathlete167Action action)
        {
            var skis=new[]{Ski(rig.Bone(HumanBodyBones.LeftFoot)),Ski(rig.Bone(HumanBodyBones.RightFoot))};var vertices=skis.Select(s=>s.GetComponent<MeshFilter>().sharedMesh.vertices).ToArray();
            var frames=skis.Select(s=>new[]{s.InverseTransformDirection(rig.transform.right),s.InverseTransformDirection(rig.transform.up),s.InverseTransformDirection(rig.transform.forward)}).ToArray();
            for(int phase=0;phase<=256;phase++)
            {
                motion.EvaluateAction(action,phase/256f);var points=skis.Select((s,side)=>vertices[side].Select(p=>s.TransformPoint(p)).ToArray()).ToArray();var directions=skis.Select((s,side)=>frames[side].Select(s.TransformDirection).ToArray()).ToArray();var axes=new List<Vector3>();axes.AddRange(directions[0]);axes.AddRange(directions[1]);
                foreach(var a in directions[0])foreach(var b in directions[1]){var cross=Vector3.Cross(a,b);if(cross.sqrMagnitude>1e-8f)axes.Add(cross.normalized);}
                float separation=float.NegativeInfinity;
                foreach(var axis in axes){float aMin=points[0].Min(p=>Vector3.Dot(p,axis)),aMax=points[0].Max(p=>Vector3.Dot(p,axis)),bMin=points[1].Min(p=>Vector3.Dot(p,axis)),bMax=points[1].Max(p=>Vector3.Dot(p,axis));separation=Mathf.Max(separation,Mathf.Max(bMin-aMax,aMin-bMax));}
                Assert.That(separation,Is.GreaterThan(.002f),action+" phase="+phase+"/256 actual ski volumes have no separating axis: lift and turn one ski rather than passing through the other.");
            }
        }
        [TestCase(Biathlete167Action.Standing),TestCase(Biathlete167Action.Prone)]
        public void LaidPolesStayCloseAndAboveTheSnow(Biathlete167Action action)
        {
            // Owner correction 2026-11-07: projected pole/ski overlap is allowed.
            // Poles may rest on skis; distance and actual solid intersections
            // matter, rather than prohibiting all overlap in a top-down image.
            var skis=new[]{Ski(rig.Bone(HumanBodyBones.LeftFoot)),Ski(rig.Bone(HumanBodyBones.RightFoot))};var poles=new[]{Ski(rig.Bone(HumanBodyBones.LeftHand)),Ski(rig.Bone(HumanBodyBones.RightHand))};motion.EvaluateAction(action,.25f);
            foreach(var pole in poles)
            {
                var vertices=pole.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>rig.transform.InverseTransformPoint(pole.TransformPoint(v))).ToArray();
                Assert.That(vertices.Min(p=>p.y),Is.GreaterThanOrEqualTo(-.002f),"A laid shaft or basket must not sink into snow.");
                Assert.That(vertices.Max(p=>p.y),Is.LessThan(.13f),"A laid pole must remain near the snow/skis.");
                var grip=vertices.OrderByDescending(p=>p.z).First();Assert.That(new Vector2(grip.x,grip.z).magnitude,Is.LessThan(.8f),"Keep handles beside the athlete, not across the neighbouring shooting mat.");
            }
            foreach(var ski in skis)
            {
                var go=new GameObject("Actual laid pole / ski contact probe");go.transform.SetParent(ski,false);var collider=go.AddComponent<MeshCollider>();collider.sharedMesh=ski.GetComponent<MeshFilter>().sharedMesh;
                try{foreach(var pole in poles)
                {
                    var mesh=pole.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)for(int edge=0;edge<3;edge++)
                    {
                        var a=pole.TransformPoint(vertices[triangles[i+edge]]);var b=pole.TransformPoint(vertices[triangles[i+(edge+1)%3]]);float length=Vector3.Distance(a,b);if(length<=.004f)continue;var direction=(b-a)/length;
                        Assert.That(collider.Raycast(new Ray(a+direction*.002f,direction),out _,length-.004f)||collider.Raycast(new Ray(b-direction*.002f,-direction),out _,length-.004f),Is.False,"Rest on a ski surface without passing the shaft through the ski.");
                    }
                }}finally{Object.DestroyImmediate(go);}
            }
        }
        [UnityTest] public IEnumerator CaptureActualSkiStancesAndArmRemoval()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();}
            if(rig==null)Create();studio.Rig.gameObject.SetActive(false);var go=new GameObject("Anatomy and ski stance camera");var camera=go.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=36;Directory.CreateDirectory(Evidence+"/native");
            try{using(var target=new BenchmarkRenderTarget(1200,1000))foreach(var action in new[]{Biathlete167Action.EnterStanding,Biathlete167Action.EnterProne,Biathlete167Action.Standing,Biathlete167Action.Prone})foreach(float phase in new[]{.125f,.25f,.5f,.75f,1f})
            {
                motion.EvaluateAction(action,phase);bool low=action==Biathlete167Action.Prone||(action==Biathlete167Action.EnterProne&&phase>.7f);var focus=new Vector3(0,low?.25f:.92f,low?-.15f:0);
                foreach(var angle in new[]{("side",new Vector3(4.4f,.4f,0)),("front",new Vector3(.2f,.4f,4.2f)),("overhead",new Vector3(0,4.9f,0))})
                {camera.transform.position=rig.transform.TransformPoint(focus+angle.Item2);if(angle.Item1=="overhead")camera.transform.rotation=Quaternion.LookRotation(-rig.transform.up,rig.transform.forward);else camera.transform.LookAt(rig.transform.TransformPoint(focus));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(Evidence+"/native/"+action+"-"+phase.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"-"+angle.Item1+".png");}
            }}finally{studio.Rig.gameObject.SetActive(true);Object.Destroy(go);motion.Stop();}
        }
    }
}
#endif

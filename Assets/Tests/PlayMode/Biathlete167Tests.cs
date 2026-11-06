#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167Tests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private Biathlete167Rig Create()=>Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7.prefab")).GetComponent<Biathlete167Rig>();
        [Test] public void ImportedPrefabHasHumanAvatarIndependentSocketsAndMeasuredLods()
        {
            var rig=Create();try
            {
                rig.Validate();Assert.That(rig.Animator.avatar.isHuman&&rig.Animator.avatar.isValid,Is.True);Assert.That(rig.Animator.applyRootMotion,Is.False);
                for(int i=0;i<6;i++)Assert.That(rig.Socket((Biathlete167Socket)i).IsChildOf(rig.transform),Is.True);
                Assert.That(rig.Rifle.parent,Is.SameAs(rig.Socket(Biathlete167Socket.RifleBack)));
                Assert.That(rig.GetComponentsInChildren<Collider>(true),Is.Empty);
                Assert.That(rig.GetComponentsInChildren<LODGroup>(true).Length,Is.EqualTo(1),"FBX automatic LOD groups must not compete with the prefab group.");
                var levels=rig.Lods.GetLODs();Assert.That(levels.Length,Is.EqualTo(3));int previous=int.MaxValue;
                for(int i=0;i<3;i++)
                {
                    int triangles=0;foreach(var renderer in levels[i].renderers)
                    {
                        Assert.That(renderer,Is.Not.Null);var skin=renderer as SkinnedMeshRenderer;var mesh=skin!=null?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                        triangles+=mesh.triangles.Length/3;Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
                        foreach(var material in renderer.sharedMaterials){Assert.That(AssetDatabase.GetAssetPath(material).StartsWith(Root+"/Materials/",StringComparison.Ordinal),Is.True,material.name);Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));}
                    }
                    Assert.That(triangles,Is.LessThan(previous));if(i==0)Assert.That(triangles,Is.InRange(60000,200000));previous=triangles;
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void ActualPbrMapsHaveExpectedResolutionAndNormalImport()
        {
            foreach(string name in new[]{"Textile_Normal_4K","Textile_Roughness_4K","Textile_BaseColor_4K","Suit_Panels_BaseColor_4K","Textile_MetallicSmoothness_4K"})
            {string path=Root+"/Textures/"+name+".png";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(4096));Assert.That(texture.height,Is.EqualTo(4096));var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.sRGBTexture,Is.EqualTo(name.Contains("BaseColor")));if(name.Contains("Normal"))Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.NormalMap));}
            Assert.That(File.Exists(Root+"/Models/Source~/Biathlete_16_7.blend"),Is.True);
            Assert.That(File.Exists(Root+"/Animations/Source_Walk_Run_Not_Ski.fbx"),Is.True);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)] public void FourDeformationPosesPreserveRootLengthsAndFiniteSkin(int pose)
        {
            var rig=Create();var baked=new Mesh();try
            {
                rig.transform.SetPositionAndRotation(new Vector3(3,0,8),Quaternion.Euler(0,32,0));
                var origin=rig.transform.position;var rotation=rig.transform.rotation;
                HumanBodyBones[][] limbs={new[]{HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand},new[]{HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand},new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot},new[]{HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot}};
                var lengths=limbs.Select(x=>new[]{Vector3.Distance(rig.Bone(x[0]).position,rig.Bone(x[1]).position),Vector3.Distance(rig.Bone(x[1]).position,rig.Bone(x[2]).position)}).ToArray();
                var leftFoot=rig.Bone(HumanBodyBones.LeftFoot).position;var rightFoot=rig.Bone(HumanBodyBones.RightFoot).position;
                rig.SetPose(pose);var transforms=rig.Animator.GetComponentsInChildren<Transform>();var positions=transforms.Select(t=>t.position).ToArray();
                rig.SetPose(pose);
                for(int i=0;i<transforms.Length;i++)Assert.That(Vector3.Distance(positions[i],transforms[i].position),Is.LessThan(.00002f),transforms[i].name);
                for(int i=0;i<limbs.Length;i++)for(int j=0;j<2;j++)Assert.That(Vector3.Distance(rig.Bone(limbs[i][j]).position,rig.Bone(limbs[i][j+1]).position),Is.EqualTo(lengths[i][j]).Within(.00002f));
                if(pose==1){Assert.That(Vector3.Distance(leftFoot,rig.Bone(HumanBodyBones.LeftFoot).position),Is.LessThan(.001f));Assert.That(Vector3.Distance(rightFoot,rig.Bone(HumanBodyBones.RightFoot).position),Is.LessThan(.001f));}
                Assert.That(rig.transform.position,Is.EqualTo(origin));Assert.That(Quaternion.Angle(rig.transform.rotation,rotation),Is.LessThan(.001f));
                foreach(var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.BakeMesh(baked);Assert.That(baked.vertexCount,Is.GreaterThan(100));
                    foreach(var v in baked.vertices){Assert.That(float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z),Is.True);Assert.That(v.magnitude,Is.LessThan(4));}
                }
                Assert.That(rig.Rifle.parent,Is.SameAs(rig.Socket(pose==3?Biathlete167Socket.RifleHand:Biathlete167Socket.RifleBack)));
                rig.SetPose(0);Assert.That(rig.Rifle.parent,Is.SameAs(rig.Socket(Biathlete167Socket.RifleBack)));
            }
            finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        [UnityTest] public IEnumerator StudioOpensWithWorkingOrbitPoseControlsAndActualModelCaptures()
        {
            // Editor AI subscription notices are ordinary Log messages, unrelated
            // to this asset. Keep a severity-specific Console gate; UnityTestRunner
            // also continues to fail on every unexpected Error/Assert/Exception.
            var errors=new System.Collections.Generic.List<string>();
            Application.LogCallback receive=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception)errors.Add(message);};
            Application.logMessageReceived+=receive;
            try
            {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new LoadSceneParameters(LoadSceneMode.Single));
            for(int i=0;i<8;i++)yield return null;
            var studio=Object.FindFirstObjectByType<Biathlete167Studio>();Assert.That(studio,Is.Not.Null);studio.Rig.Validate();
            var document=studio.GetComponent<UIDocument>();Assert.That(document.rootVisualElement.Query<Button>().ToList().Count,Is.EqualTo(14));
            Assert.That(studio.Motion,Is.Not.Null);
            var scrub=document.rootVisualElement.Q<Slider>("motion-phase");var speed=document.rootVisualElement.Q<Slider>("motion-speed");
            Assert.That(scrub,Is.Not.Null);Assert.That(speed,Is.Not.Null);
            scrub.value=.37f;Assert.That(studio.Motion.Phase,Is.EqualTo(.37f).Within(.00001f));Assert.That(studio.Motion.PlaybackEnabled,Is.False);
            speed.value=.5f;Assert.That(studio.Motion.PlaybackSpeed,Is.EqualTo(.5f));studio.Motion.Stop();
            string folder=Path.Combine(Application.dataPath,"../artifacts/m16-7/native");Directory.CreateDirectory(folder);
            using(var target=new BenchmarkRenderTarget(1100,1400))
            {
                for(int i=0;i<6;i++)
                {studio.SetView(i);yield return null;target.Render(studio.View);yield return null;target.SavePng(Path.Combine(folder,new[]{"front","side","back","face","rifle","boots"}[i]+".png"));}
                studio.SetView(0);
                for(int i=1;i<=3;i++){studio.Rig.SetPose(i);yield return null;target.Render(studio.View);yield return null;target.SavePng(Path.Combine(folder,"pose"+i+".png"));}
                studio.SetView(1);
                for(int i=0;i<8;i++){studio.Motion.EvaluateCycle(i/8f);yield return null;target.Render(studio.View);yield return null;target.SavePng(Path.Combine(folder,"skate"+i+".png"));}
                if(studio.Motion.MainReference!=null)
                {
                    string comparison=Path.Combine(Application.dataPath,"../artifacts/m16-7-refinement/main-motion-comparison");Directory.CreateDirectory(comparison);
                    var candidateRenderers=studio.Rig.GetComponentsInChildren<Renderer>(true);var visibility=candidateRenderers.Select(r=>r.enabled).ToArray();
                    try
                    {
                        for(int angle=0;angle<2;angle++)for(int phase=0;phase<4;phase++)
                        {
                            studio.SetView(angle);studio.Motion.EvaluateCycle(phase/4f);yield return null;
                            target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"natural-"+angle+"-"+phase+".png"));
                            foreach(var renderer in candidateRenderers)renderer.enabled=false;
                            studio.Motion.MainReference.SetReferenceVisible(true);yield return null;
                            target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"main-"+angle+"-"+phase+".png"));
                            studio.Motion.MainReference.SetReferenceVisible(false);for(int at=0;at<candidateRenderers.Length;at++)candidateRenderers[at].enabled=visibility[at];
                        }
                    }
                    finally{studio.Motion.MainReference.SetReferenceVisible(false);for(int at=0;at<candidateRenderers.Length;at++)candidateRenderers[at].enabled=visibility[at];}
                    foreach(var state in new[]{SkiMotionState.Uphill,SkiMotionState.MaxSprint,SkiMotionState.Tuck})
                    {
                        studio.Motion.MainReference.State=state;studio.SetView(1);
                        for(int phase=0;phase<4;phase++)
                        {
                            studio.Motion.EvaluateCycle(phase/4f);yield return null;
                            target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,state+"-"+phase+".png"));
                        }
                    }
                    studio.Motion.MainReference.State=SkiMotionState.Flat;
                    studio.Motion.Stop();
                    var profile=new GameObject("Neck inspection camera").AddComponent<Camera>();profile.CopyFrom(studio.View);profile.enabled=false;
                    try{for(int neck=0;neck<4;neck++)
                    {
                        if(neck==1)studio.Motion.EvaluateCycle(.25f);
                        if(neck==2)studio.Motion.EvaluateCycle(.75f);
                        if(neck==3){studio.Motion.MainReference.State=SkiMotionState.Tuck;studio.Motion.EvaluateCycle(0);}
                        yield return null;
                        var head=studio.Rig.Bone(HumanBodyBones.Head).position;
                        var aim=head+Vector3.down*.045f;
                        profile.transform.position=aim+Vector3.right*.85f;
                        profile.transform.LookAt(aim);target.Render(profile);yield return new WaitForEndOfFrame();
                        target.SavePng(Path.Combine(comparison,"neck-profile-"+neck+".png"));
                    }}finally{Object.Destroy(profile.gameObject);}
                    var close=new GameObject("Equipment inspection camera").AddComponent<Camera>();close.CopyFrom(studio.View);close.enabled=false;
                    try{foreach(var state in new[]{SkiMotionState.Flat,SkiMotionState.Uphill,SkiMotionState.MaxSprint,SkiMotionState.Tuck})
                    for(int phase=0;phase<4;phase++)
                    {
                        studio.Motion.MainReference.State=state;studio.Motion.EvaluateCycle(phase/4f);yield return null;
                        var wrist=studio.Rig.Bone(HumanBodyBones.LeftHand).position;
                        close.transform.position=wrist+new Vector3(.40f,.18f,.55f);close.transform.LookAt(wrist);
                        target.Render(close);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"hands-"+state+"-"+phase+".png"));
                        if(state==SkiMotionState.Flat||state==SkiMotionState.Tuck)
                        {
                            var rightWrist=studio.Rig.Bone(HumanBodyBones.RightHand).position;
                            close.transform.position=rightWrist+new Vector3(-.40f,.18f,.55f);close.transform.LookAt(rightWrist);
                            target.Render(close);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"hands-right-"+state+"-"+phase+".png"));
                            foreach(bool right in new[]{false,true})
                            {
                                var at=studio.Rig.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position;
                                close.transform.position=at+new Vector3(right?-.60f:.60f,.35f,.20f);close.transform.LookAt(at);
                                target.Render(close);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"hands-dorsal-"+(right?"right":"left")+"-"+state+"-"+phase+".png"));
                            }
                        }
                        var foot=studio.Rig.ToeHinge(false).position+Vector3.up*.10f;
                        close.transform.position=foot+new Vector3(.80f,.10f,.15f);close.transform.LookAt(foot);
                        target.Render(close);yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(comparison,"binding-"+state+"-"+phase+".png"));
                    }}finally{Object.Destroy(close.gameObject);studio.Motion.MainReference.State=SkiMotionState.Flat;}
                    foreach(var frame in Directory.GetFiles(comparison,"*.png"))
                    {
                        var pixels=new Texture2D(2,2);try
                        {
                            Assert.That(pixels.LoadImage(File.ReadAllBytes(frame)),Is.True,frame);int visible=0,total=0;
                            for(int y=0;y<pixels.height;y+=16)for(int x=0;x<pixels.width;x+=16)
                            {total++;var pixel=pixels.GetPixel(x,y);if(pixel.maxColorComponent>.3f)visible++;}
                            Assert.That((float)visible/total,Is.GreaterThan(.01f),"Empty actual model capture: "+frame);
                        }finally{Object.Destroy(pixels);}
                    }
                    studio.Motion.MainReference.State=SkiMotionState.Flat;
                }
                studio.Motion.Stop();
            }
            Assert.That(File.Exists(Path.Combine(folder,"pose3.png")),Is.True);
            Assert.That(errors,Is.Empty);
            }
            finally{Application.logMessageReceived-=receive;}
        }
    }
}
#endif

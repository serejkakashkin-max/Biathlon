using System;
using System.Collections;
using System.IO;
using Biathlon.Career;
using Unity.Profiling;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteCreatorStudioTests
    {
        private GameObject root;
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private AthleteCreatorStudio Studio()
        {root=new GameObject("Creator studio test");var studio=root.AddComponent<AthleteCreatorStudio>();studio.Initialize();return studio;}
        [UnityTest] public IEnumerator NativePreviewReusesOneRigAndReleasesItsOwnedResources()
        {
            var fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;var sun=RenderSettings.sun;
            var studio=Studio();var rig=studio.Rig;var target=studio.Texture;var camera=studio.PreviewCamera;
            for(int i=0;i<20;i++){studio.Initialize();studio.Rotate(30);studio.SetView(i*10,.5f);studio.SetPose(AthletePreviewPose.Skate);studio.SamplePose(i*.1);}
            Assert.That(studio.Rig,Is.SameAs(rig));Assert.That(studio.Texture,Is.SameAs(target));Assert.That(studio.PreviewCamera,Is.SameAs(camera));
            Assert.That(root.GetComponentsInChildren<AthleteRigBinding>().Length,Is.EqualTo(1));Assert.That(root.GetComponentsInChildren<Camera>().Length,Is.EqualTo(1));
            Assert.That(target.IsCreated(),Is.True);Assert.That(camera.targetTexture,Is.SameAs(target));
            Assert.That(RenderSettings.fog,Is.EqualTo(fog));Assert.That(RenderSettings.ambientLight,Is.EqualTo(ambient));Assert.That(RenderSettings.sun,Is.SameAs(sun));
            studio.SetView(25,0);studio.SetPose(AthletePreviewPose.Idle);yield return null;yield return new WaitForEndOfFrame();
            Save(studio.Texture,"studio-full-body.png");studio.SetView(0,1);yield return null;yield return new WaitForEndOfFrame();Save(studio.Texture,"studio-face.png");
            studio.Release();Assert.That(target.IsCreated(),Is.False);yield return null;
            Assert.That(rig==null,Is.True);Assert.That(camera==null,Is.True);Assert.That(target==null,Is.True);
            Assert.That(root.transform.childCount,Is.Zero);Assert.Throws<ObjectDisposedException>(()=>studio.Initialize());
        }
        [Test] public void ViewPresetsBoundZoomAndReducedMotionKeepsTheFaceStill()
        {
            var studio=Studio();studio.SetView(720,2);Assert.That(studio.Yaw,Is.Zero);Assert.That(studio.Zoom,Is.EqualTo(1));
            studio.SetPose(AthletePreviewPose.BackCarry);Assert.That(studio.Yaw,Is.EqualTo(180));studio.Rotate(-450);Assert.That(studio.Yaw,Is.EqualTo(90));
            studio.SetView(0,1);studio.SetPose(AthletePreviewPose.Skate);studio.SamplePose(0);
            var hand=studio.Rig.Bone(AthleteBone.LeftHand).position;studio.SamplePose(10);Assert.That(Vector3.Distance(hand,studio.Rig.Bone(AthleteBone.LeftHand).position),Is.LessThan(.00001f));
            studio.SetView(0,0);studio.ReducedMotion=true;studio.SamplePose(0);hand=studio.Rig.Bone(AthleteBone.LeftHand).position;
            studio.SamplePose(10);Assert.That(Vector3.Distance(hand,studio.Rig.Bone(AthleteBone.LeftHand).position),Is.LessThan(.00001f));
            Assert.Throws<ArgumentException>(()=>studio.Rotate(float.NaN));Assert.Throws<ArgumentException>(()=>studio.SetView(0,float.NaN));
            Assert.Throws<ArgumentException>(()=>studio.SamplePose(double.NaN));Assert.Throws<ArgumentOutOfRangeException>(()=>studio.SetPose((AthletePreviewPose)99));
        }
        [Test] public void ExplicitFacesAndSkinShareTheApprovedModelWithoutRepaintingOtherAthletes()
        {
            var studio=Studio();var a=studio.Rig.GetComponent<AthleteAppearancePresentation>();var other=AthleteRigFactory.Create(root.transform);
            var b=other.GetComponent<AthleteAppearancePresentation>();b.ApplyIdentity("studio_other");b.ApplyNation("GER");
            var bskin=other.GetComponentInChildren<SkinnedMeshRenderer>();var bmaterial=bskin.sharedMaterials[3];int oldFace=b.FaceVariant,oldTone=b.SkinVariant;
            var skin=studio.Rig.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;var recipe=AthleteFaceCatalog.Load();
            for(int face=0;face<recipe.profiles.Length;face++)for(int tone=0;tone<recipe.skin_colors.Length;tone++)
            {
                a.ApplyCosmeticSelection(recipe.profiles[face].id,tone,1);Assert.That(skin.sharedMesh,Is.SameAs(mesh));
                Assert.That(a.FaceVariant,Is.EqualTo(face));Assert.That(a.SkinVariant,Is.EqualTo(tone));
                Assert.That(skin.GetBlendShapeWeight(mesh.GetBlendShapeIndex(recipe.profiles[face].ShapeName)),Is.EqualTo(100));
                Assert.That(b.FaceVariant,Is.EqualTo(oldFace));Assert.That(b.SkinVariant,Is.EqualTo(oldTone));Assert.That(bskin.sharedMaterials[3],Is.SameAs(bmaterial));
            }
            a.ApplyNation("FRA");Assert.That(a.SkinVariant,Is.EqualTo(recipe.skin_colors.Length-1));
            Assert.Throws<ArgumentException>(()=>a.ApplyCosmeticSelection("missing",0,1));
            Assert.Throws<ArgumentException>(()=>a.ApplyCosmeticSelection(recipe.profiles[0].id,99,1));
        }
        [Test] public void SavedProfileProducesIdenticalPreviewAndRaceRigAppearanceAcrossAllLods()
        {
            var studio=Studio();var raceRig=AthleteRigFactory.Create(root.transform);var saved=AppearanceProfile.FromJson(AppearanceProfile.Create("defined_chin","brown_deep","slim").ToJson());
            var a=studio.Rig.GetComponent<AthleteAppearancePresentation>();var b=raceRig.GetComponent<AthleteAppearancePresentation>();
            a.ApplyNation("ITA");b.ApplyNation("ITA");a.ApplyProfile(saved);b.ApplyProfile(saved);
            var skinA=studio.Rig.GetComponentInChildren<SkinnedMeshRenderer>();var skinB=raceRig.GetComponentInChildren<SkinnedMeshRenderer>();
            foreach(float distance in new[]{0f,20f,60f})
            {
                studio.Rig.GetComponent<AthleteSkinDetailBinding>().Prepare(distance,false,true);raceRig.GetComponent<AthleteSkinDetailBinding>().Prepare(distance,false,true);
                Assert.That(skinA.sharedMesh,Is.SameAs(skinB.sharedMesh));
                // Joint correctives follow the current pose; cosmetic identity does not.
                for(int i=4;i<skinA.sharedMesh.blendShapeCount;i++)Assert.That(skinA.GetBlendShapeWeight(i),Is.EqualTo(skinB.GetBlendShapeWeight(i)),skinA.sharedMesh.GetBlendShapeName(i));
                for(int i=0;i<skinA.sharedMaterials.Length;i++)Assert.That(skinA.sharedMaterials[i],Is.SameAs(skinB.sharedMaterials[i]));
            }
            long bytes=0;
            using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",2048,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {Assert.That(recorder.Valid,Is.True);recorder.Reset();for(int i=0;i<1000;i++)b.ApplyProfile(saved);recorder.Stop();for(int i=0;i<recorder.Count;i++)bytes+=recorder.GetSample(i).Value;}
            Assert.That(bytes,Is.Zero);
            b.ApplyProfile(AppearanceProfile.Legacy("saved_old_identity"));var recipe=AthleteFaceCatalog.Load();recipe.Resolve("saved_old_identity",out int face,out int tone);
            Assert.That(b.FaceVariant,Is.EqualTo(face));Assert.That(b.SkinVariant,Is.EqualTo(tone));
        }
        private static void Save(RenderTexture target,string filename)
        {
            var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                if(filename=="studio-full-body.png")
                {
                    var pixels=image.GetPixels32();var background=pixels[0];int subject=0,clipped=0;
                    foreach(var p in pixels)if(Math.Abs(p.r-background.r)+Math.Abs(p.g-background.g)+Math.Abs(p.b-background.b)>12)
                    {subject++;if(p.r>245&&p.g>245&&p.b>245)clipped++;}
                    Assert.That(subject,Is.GreaterThan(pixels.Length/10),"Preview must contain the actual visible athlete.");
                    Assert.That(clipped/(double)Math.Max(1,subject),Is.LessThan(.35),"Studio light must preserve skin and garment detail.");
                }
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../docs/quality/m15-6"));Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,filename),image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;Object.Destroy(image);}
        }
    }
}

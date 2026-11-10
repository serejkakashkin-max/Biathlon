using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteEquipmentBatchesTests
    {
        [Test] public void CachedHumanoidBindingsMatchTheNativeAvatarAcrossPoses()
        {
            var root=new GameObject("Humanoid binding cache witness");
            try
            {
                var source=AthleteRigFactory.Create(root.transform);var hero=Biathlete167RacePresentation.Attach(source).Rig;
                for(int pose=0;pose<4;pose++)
                {
                    hero.SetPose(pose);
                    for(int bone=0;bone<(int)HumanBodyBones.LastBone;bone++)
                        Assert.That(hero.Bone((HumanBodyBones)bone),Is.SameAs(hero.Animator.GetBoneTransform((HumanBodyBones)bone)));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void AimingCameraAndChangedShadowModeAreObserved()
        {
            var root=new GameObject("Equipment camera switch");
            try
            {
                var chase=new GameObject("Chase",typeof(Camera)).GetComponent<Camera>();chase.transform.SetParent(root.transform);chase.enabled=false;
                var aim=new GameObject("Aim",typeof(Camera)).GetComponent<Camera>();aim.transform.SetParent(root.transform);aim.transform.rotation=Quaternion.Euler(0,180,0);
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(root.transform);cube.transform.position=new Vector3(0,0,-5);
                var renderer=cube.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var batches=root.AddComponent<AthleteEquipmentBatches>();batches.ViewCamera=chase;batches.RangeCamera=aim;batches.RegisterVisual(cube.transform);
                batches.Render();Assert.That(batches.SubmittedInstances,Is.EqualTo(1));
                aim.enabled=false;batches.Render();Assert.That(batches.SubmittedInstances,Is.Zero);
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                batches.Render();Assert.That(batches.SubmittedInstances,Is.EqualTo(1));
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                batches.Render();Assert.That(batches.SubmittedInstances,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }

        [UnityTest] public IEnumerator CullingPreservesNativeEquipmentAndShadowPixels()
        {
            if(!SystemInfo.supportsInstancing)Assert.Ignore("GPU instancing unavailable.");
            const int layer=29;
            var root=new GameObject("Equipment native image witness");var previousSun=RenderSettings.sun;
            var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=Color.white;
            string folder="artifacts/m16-8-performance-pass/visual";System.IO.Directory.CreateDirectory(folder);
            try
            {
                var source=AthleteRigFactory.Create(root.transform);var hero=Biathlete167RacePresentation.Attach(source);hero.RenderPreview();
                foreach(var skin in hero.Rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=true;
                var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.transform.SetParent(root.transform);ground.transform.localScale=Vector3.one*3;
                ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                var sun=new GameObject("Witness light",typeof(Light)).GetComponent<Light>();sun.transform.SetParent(root.transform);
                sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(40,25,0);RenderSettings.sun=sun;
                var hidden=GameObject.CreatePrimitive(PrimitiveType.Cube);hidden.transform.SetParent(root.transform);hidden.transform.position=new Vector3(0,0,-120);
                hidden.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var caster=GameObject.CreatePrimitive(PrimitiveType.Cube);caster.transform.SetParent(root.transform);caster.transform.position=new Vector3(0,3,-1);caster.transform.localScale=Vector3.one*.5f;
                var casterRenderer=caster.GetComponent<MeshRenderer>();casterRenderer.sharedMaterial=groundMaterial;
                var view=new GameObject("Witness camera",typeof(Camera)).GetComponent<Camera>();view.transform.SetParent(root.transform);view.enabled=false;
                view.cullingMask=1<<layer;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.15f,.2f,.25f);view.fieldOfView=38;view.aspect=1;
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(view);quality.Apply("high");
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
                var batches=root.AddComponent<AthleteEquipmentBatches>();batches.RegisterVisual(hero.Rig.transform);batches.RegisterVisual(hidden.transform);batches.RegisterVisual(caster.transform);
                using(var target=new BenchmarkRenderTarget(640,640))
                {
                    for(int angle=0;angle<2;angle++)
                    {
                        view.transform.position=angle==0?new Vector3(3,2,-4):new Vector3(-4,2,1);
                        view.transform.LookAt(Vector3.up);
                        casterRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                        if(angle==0)Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(view),casterRenderer.bounds),Is.False,"The shadow witness must be outside the view.");
                        for(int pass=0;pass<2;pass++)
                        {
                            batches.ViewCamera=pass==0?null:view;
                            for(int frame=0;frame<4;frame++){target.Render(view);yield return new WaitForEndOfFrame();yield return null;}
                            target.SavePng(folder+"/"+angle+"-"+pass+".png");
                            if(pass==1)Assert.That(batches.CulledSources,Is.GreaterThan(0));
                        }
                        var before=new Texture2D(2,2);var after=new Texture2D(2,2);
                        try
                        {
                            before.LoadImage(System.IO.File.ReadAllBytes(folder+"/"+angle+"-0.png"));after.LoadImage(System.IO.File.ReadAllBytes(folder+"/"+angle+"-1.png"));
                            var a=before.GetPixels32();var b=after.GetPixels32();int changed=0;
                            for(int p=0;p<a.Length;p++)if(Mathf.Abs(a[p].r-b[p].r)+Mathf.Abs(a[p].g-b[p].g)+Mathf.Abs(a[p].b-b[p].b)>12)changed++;
                            Assert.That(changed/(float)a.Length,Is.LessThan(.001f),"Culling must retain visible skis, poles, rifle and offscreen-caster shadows.");
                        }
                        finally{Object.Destroy(before);Object.Destroy(after);}
                        if(angle==0)
                        {
                            casterRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                            for(int frame=0;frame<4;frame++){target.Render(view);yield return new WaitForEndOfFrame();yield return null;}
                            target.SavePng(folder+"/shadow-removed.png");
                            var withShadow=new Texture2D(2,2);var withoutShadow=new Texture2D(2,2);
                            try
                            {
                                withShadow.LoadImage(System.IO.File.ReadAllBytes(folder+"/0-1.png"));withoutShadow.LoadImage(System.IO.File.ReadAllBytes(folder+"/shadow-removed.png"));
                                var a=withShadow.GetPixels32();var b=withoutShadow.GetPixels32();int changed=0;
                                for(int p=0;p<a.Length;p++)if(Mathf.Abs(a[p].r-b[p].r)+Mathf.Abs(a[p].g-b[p].g)+Mathf.Abs(a[p].b-b[p].b)>12)changed++;
                                Assert.That(changed,Is.GreaterThan(20),"An offscreen caster must contribute a visible shadow in this fixture.");
                            }
                            finally{Object.Destroy(withShadow);Object.Destroy(withoutShadow);}
                        }
                    }
                }
            }
            finally{RenderSettings.sun=previousSun;Object.Destroy(root);Object.Destroy(groundMaterial);}
        }

        [Test] public void OffscreenEquipmentIsNotSubmittedAndReappearsWhenViewed()
        {
            var root=new GameObject("Equipment frustum witness");
            try
            {
                var view=new GameObject("Equipment observer",typeof(Camera)).GetComponent<Camera>();view.transform.SetParent(root.transform);
                view.enabled=false;view.fieldOfView=60;view.aspect=1;view.farClipPlane=200;
                var batches=root.AddComponent<AthleteEquipmentBatches>();batches.ViewCamera=view;
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(root.transform);cube.transform.position=new Vector3(0,0,-120);
                var renderer=cube.GetComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                batches.RegisterVisual(cube.transform);batches.Render();
                Assert.That(batches.SubmittedInstances,Is.Zero,"An offscreen rigid mesh must not reach an instanced draw merely because another athlete shares its batch.");
                view.transform.rotation=Quaternion.Euler(0,180,0);batches.Render();
                Assert.That(batches.SubmittedInstances,Is.EqualTo(1));
                view.transform.rotation=Quaternion.identity;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                cube.transform.position=new Vector3(0,0,-2);batches.Render();
                Assert.That(batches.SubmittedInstances,Is.EqualTo(1),"Retain nearby offscreen equipment that can cast a visible shadow.");
                batches.enabled=false;Assert.That(renderer.forceRenderingOff,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator OriginalEquipmentRetainsVisibilityAndCanBeRestored()
        {
            if (!SystemInfo.supportsInstancing) Assert.Ignore("GPU instancing unavailable on this device.");
            var root = new GameObject("Equipment batches test");
            try
            {
                var rig = AthleteRigFactory.Create(root.transform);
                var batches = root.AddComponent<AthleteEquipmentBatches>();
                var originals = rig.GetComponentsInChildren<MeshRenderer>(true);
                batches.Register(rig); batches.Render();
                Assert.That(batches.SourceCount, Is.GreaterThan(0));
                Assert.That(batches.SubmittedInstances, Is.GreaterThan(0));
                int visible = batches.SubmittedInstances;
                foreach (var renderer in originals) renderer.enabled = false;
                batches.Render();
                Assert.That(batches.SubmittedInstances, Is.Zero, "The shooting camera must still be able to hide the player's body/equipment.");
                foreach (var renderer in originals) renderer.enabled = true;
                batches.Render(); Assert.That(batches.SubmittedInstances, Is.EqualTo(visible));
                batches.enabled = false;
                foreach (var renderer in originals) Assert.That(renderer.forceRenderingOff, Is.False);
                batches.enabled = true;
                batches.Clear(); Assert.That(batches.SourceCount, Is.Zero);
                foreach (var renderer in originals) Assert.That(renderer.forceRenderingOff, Is.False);
                batches.Register(rig); batches.Render();
                Assert.That(batches.SubmittedInstances, Is.EqualTo(visible), "Restarting a race must not retain dead sources.");
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}

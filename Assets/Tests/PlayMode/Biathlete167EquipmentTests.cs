#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Security.Cryptography;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167EquipmentTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        private Biathlete167Rig Create()=>Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();
        [Test] public void CarriedBarrelClearsTheActualHeadAndNeckInAllMotionStates()
        {
            var rig=Create();var baked=new Mesh();try
            {
                var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
                int skinIndex=Array.FindIndex(skin.sharedMaterials,m=>m.name=="Anatomical_Skin_CC0_2K");Assert.That(skinIndex,Is.GreaterThanOrEqualTo(0));
                var triangles=skin.sharedMesh.GetTriangles(skinIndex);
                var barrel=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;
                int barrelIndex=Array.FindIndex(rig.Rifle.GetComponent<Renderer>().sharedMaterials,m=>m.name=="PULSE_Rifle_Barrel_Satin");Assert.That(barrelIndex,Is.GreaterThanOrEqualTo(0));
                var vertices=barrel.vertices;var points=barrel.GetTriangles(barrelIndex).Distinct().Select(i=>vertices[i]).ToArray();
                var centre=points.Aggregate(Vector3.zero,(sum,p)=>sum+p)/points.Length;var axis=Vector3.up;
                // Measure the real imported cylinder's long axis, independent of
                // the generator's carry transform and authored length constants.
                for(int pass=0;pass<12;pass++)axis=points.Aggregate(Vector3.zero,(sum,p)=>sum+(p-centre)*Vector3.Dot(p-centre,axis)).normalized;
                var across=Vector3.Cross(axis,Vector3.right).normalized;var vertical=Vector3.Cross(axis,across);
                float low=points.Min(p=>Vector3.Dot(p-centre,axis)),high=points.Max(p=>Vector3.Dot(p-centre,axis));
                float radius=points.Max(p=>Vector3.ProjectOnPlane(p-centre,axis).magnitude)+.002f;
                using(var motion=new Biathlete167MainMotion(rig))foreach(SkiMotionState state in Enum.GetValues(typeof(SkiMotionState)))for(int phase=0;phase<4;phase++)
                {
                    motion.State=state;motion.Evaluate(phase/4f);skin.BakeMesh(baked);var body=baked.vertices;
                    for(int i=0;i<body.Length;i++)body[i]=skin.transform.TransformPoint(body[i]);
                    for(int ray=0;ray<9;ray++)
                    {
                        float angle=(ray-1)*Mathf.PI/4;var offset=ray==0?Vector3.zero:(across*Mathf.Cos(angle)+vertical*Mathf.Sin(angle))*radius;
                        var start=rig.Rifle.TransformPoint(centre+axis*low+offset);var end=rig.Rifle.TransformPoint(centre+axis*high+offset);var direction=(end-start).normalized;
                        for(int i=0;i<triangles.Length;i+=3)
                        {
                            var a=body[triangles[i]];var edge=body[triangles[i+1]]-a;var other=body[triangles[i+2]]-a;var cross=Vector3.Cross(direction,other);float determinant=Vector3.Dot(edge,cross);
                            if(Mathf.Abs(determinant)<1e-9f)continue;
                            var delta=start-a;float u=Vector3.Dot(delta,cross)/determinant;if(u<0||u>1)continue;
                            var q=Vector3.Cross(delta,edge);float v=Vector3.Dot(direction,q)/determinant;if(v<0||u+v>1)continue;
                            float distance=Vector3.Dot(other,q)/determinant;
                            Assert.That(distance<=0||distance>=Vector3.Distance(start,end),Is.True,$"Carried barrel crosses actual head/neck: {state}, phase {phase}, ray {ray}, triangle {i}.");
                        }
                    }
                }
            }finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void CarriedRifleFitsTheAthletesVisualProportions()
        {
            var rig=Create();var baked=new Mesh();try
            {
                rig.SetPose(0);var skin=rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();skin.BakeMesh(baked);
                var body=baked.vertices.Select(v=>rig.transform.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
                var mesh=rig.Rifle.GetComponent<MeshFilter>().sharedMesh;var rifle=mesh.vertices.Select(v=>rig.transform.InverseTransformPoint(rig.Rifle.TransformPoint(v))).ToArray();
                float shoulders=Mathf.Abs(rig.transform.InverseTransformPoint(rig.Bone(HumanBodyBones.LeftUpperArm).position).x-rig.transform.InverseTransformPoint(rig.Bone(HumanBodyBones.RightUpperArm).position).x);
                float height=body.Max(v=>v.y)-body.Min(v=>v.y),rifleHeight=rifle.Max(v=>v.y)-rifle.Min(v=>v.y),width=rifle.Max(v=>v.x)-rifle.Min(v=>v.x),depth=rifle.Max(v=>v.z)-rifle.Min(v=>v.z);
                Assert.That(rifleHeight/height,Is.InRange(.49f,.58f),"Owner reference uses a compact carried rifle relative to athlete height.");
                Assert.That(width/shoulders,Is.LessThanOrEqualTo(.50f),"The rejected broad prop occupied most of the back width.");
                Assert.That(depth,Is.LessThanOrEqualTo(.060f),"Actual imported mesh, including fittings, must fit the slim visual profile.");
            }finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
        [Serializable] private sealed class Receipt{public string equipment_detail_definition_sha256;public string[] eyewear_variants;}
        [Test] public void MaskStrapHasAClosedVolumeAndDedicatedPrint()
        {
            var rig=Create();try
            {
                var renderer=rig.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="Eyewear_Mask");var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                var faces=new System.Collections.Generic.List<int>();
                for(int i=0;i<renderer.sharedMaterials.Length;i++)if(renderer.sharedMaterials[i].name=="PULSE_Mask_Woven_Strap"||renderer.sharedMaterials[i].name=="PULSE_Mask_Strap_Accent")faces.AddRange(mesh.GetTriangles(i));
                var positions=mesh.vertices;var ids=new System.Collections.Generic.Dictionary<Vector3Int,int>();var welded=new int[positions.Length];
                for(int i=0;i<positions.Length;i++){var key=Vector3Int.RoundToInt(positions[i]*1000000);if(!ids.TryGetValue(key,out int id)){id=ids.Count;ids.Add(key,id);}welded[i]=id;}
                var edges=new System.Collections.Generic.Dictionary<(int,int),int>();double volume=0;
                for(int i=0;i<faces.Count;i+=3)
                {
                    int a=faces[i],b=faces[i+1],c=faces[i+2];volume+=Vector3.Dot(positions[a],Vector3.Cross(positions[b],positions[c]))/6.0;
                    foreach(var pair in new[]{(welded[a],welded[b]),(welded[b],welded[c]),(welded[c],welded[a])}){var key=pair.Item1<pair.Item2?pair:(pair.Item2,pair.Item1);edges.TryGetValue(key,out int count);edges[key]=count+1;}
                }
                Assert.That(edges.Count,Is.GreaterThan(500));Assert.That(edges.Values.All(n=>n==2),Is.True,"The old flat strap has boundary edges and cannot qualify as solid webbing.");
                Assert.That(Math.Abs(volume),Is.InRange(.000005,.00008));
                var material=renderer.sharedMaterials.Single(m=>m.name=="PULSE_Mask_Woven_Strap");Assert.That(material.GetTexture("_BaseMap").name,Is.EqualTo("Mask_PULSE_Strap_BaseColor_2K"));
            }finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Serializable] private sealed class SourceFile{public string file,sha256;}
        [Serializable] private sealed class SourceReceipt{public string asset_id,license;public SourceFile[] downloads;}
        [Serializable] private sealed class SourceExportReceipt{public SourceReceipt walnut_source;}
        [Test] public void NaturalWalnutSourceFilesRetainTheirDeclaredCC0Hashes()
        {
            var data=JsonUtility.FromJson<SourceReceipt>(File.ReadAllText(Root+"/Models/Source~/Walnut/provenance.json"));
            Assert.That(data.asset_id,Is.EqualTo("natural_walnut_veneer"));Assert.That(data.license,Is.EqualTo("CC0-1.0"));Assert.That(data.downloads.Length,Is.EqualTo(2));
            var export=JsonUtility.FromJson<SourceExportReceipt>(File.ReadAllText(Root+"/Documentation/natural-asset-manifest.json"));
            Assert.That(export.walnut_source,Is.Not.Null,"The model export must declare the actual wood source, not only have a downloaded file nearby.");
            Assert.That(export.walnut_source.asset_id,Is.EqualTo(data.asset_id));Assert.That(export.walnut_source.downloads.Select(f=>f.sha256),Is.EqualTo(data.downloads.Select(f=>f.sha256)));
            using(var sha=SHA256.Create())foreach(var item in data.downloads)
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Root+"/Models/Source~/Walnut/"+item.file))).Replace("-","").ToLowerInvariant(),Is.EqualTo(item.sha256));
        }
        [Test] public void EyewearSelectionIsExclusiveAndSurvivesAllMotionAndInspectionPoses()
        {
            var rig=Create();
            try
            {
                Assert.That(rig.HasEyewearChoice,Is.True);Assert.That(rig.Eyewear,Is.EqualTo(Biathlete167Eyewear.Glasses));
                var glasses=rig.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Eyewear_Glasses");
                var mask=rig.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Eyewear_Mask");
                Assert.That(glasses.transform.parent,Is.SameAs(mask.transform.parent));
                using(var motion=new Biathlete167MainMotion(rig))foreach(var value in new[]{Biathlete167Eyewear.Mask,Biathlete167Eyewear.Glasses})
                {
                    rig.SetEyewear(value);var parent=mask.transform.parent;var position=mask.transform.localPosition;var rotation=mask.transform.localRotation;
                    foreach(var state in new[]{SkiMotionState.Flat,SkiMotionState.Uphill,SkiMotionState.MaxSprint,SkiMotionState.Tuck})for(int phase=0;phase<8;phase++)
                    {
                        motion.State=state;motion.Evaluate(phase/8f);
                        Assert.That(mask.gameObject.activeSelf,Is.EqualTo(value==Biathlete167Eyewear.Mask));
                        Assert.That(glasses.gameObject.activeSelf,Is.EqualTo(value==Biathlete167Eyewear.Glasses));
                        Assert.That(mask.transform.parent,Is.SameAs(parent));Assert.That(Vector3.Distance(mask.transform.localPosition,position),Is.LessThan(.00001f));Assert.That(Quaternion.Angle(mask.transform.localRotation,rotation),Is.LessThan(.001f));
                    }
                    for(int pose=0;pose<4;pose++){rig.SetPose(pose);Assert.That(rig.Eyewear,Is.EqualTo(value));Assert.That(mask.gameObject.activeSelf,Is.EqualTo(value==Biathlete167Eyewear.Mask));}
                }
                Assert.Throws<ArgumentOutOfRangeException>(()=>rig.SetEyewear((Biathlete167Eyewear)999));
                Assert.Throws<InvalidOperationException>(()=>rig.ConfigureEyewear(glasses.gameObject,glasses.gameObject));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void EquipmentExportAndMaterialsMatchTheirVersionedDefinition()
        {
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(Root+"/Documentation/natural-asset-manifest.json"));
            Assert.That(receipt.eyewear_variants,Is.EqualTo(new[]{"glasses","mask"}));
            using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Root+"/Documentation/equipment-detail-v1.json"))).Replace("-","").ToLowerInvariant(),Is.EqualTo(receipt.equipment_detail_definition_sha256));
            foreach(var pair in new[]{("PULSE_Ski_Printed_Topsheet","Ski_PULSE_Topsheet_BaseColor_4K"),("PULSE_Pole_Carbon_Weave","Pole_PULSE_Carbon_BaseColor_2K"),("PULSE_Natural_Rifle_Walnut","Rifle_PULSE_Walnut_BaseColor_2K")})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+pair.Item1+".mat");Assert.That(mat,Is.Not.Null);Assert.That(mat.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(mat.GetTexture("_BaseMap").name,Is.EqualTo(pair.Item2));
            }
            var provenance=File.ReadAllText(Root+"/Models/Source~/SkiingLady/provenance.json");
            Assert.That(provenance,Does.Contain("CC-BY-4.0").And.Contain("lancehuang526"));
        }
        [UnityTest] public IEnumerator StudioEyewearMenuSwitchesTheActualImportedRenderers()
        {
            var studio=Object.FindAnyObjectByType<Biathlete167Studio>();
            if(studio==null)
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return null;studio=Object.FindAnyObjectByType<Biathlete167Studio>();
            }
            var field=studio.GetComponent<UIDocument>().rootVisualElement.Q<DropdownField>("athlete-eyewear");Assert.That(field,Is.Not.Null);
            field.index=1;yield return null;Assert.That(studio.Rig.Eyewear,Is.EqualTo(Biathlete167Eyewear.Mask));
            field.index=0;yield return null;Assert.That(studio.Rig.Eyewear,Is.EqualTo(Biathlete167Eyewear.Glasses));
            string folder=Path.Combine(Application.dataPath,"../artifacts/m16-7-rifle-scale-20261106/native");Directory.CreateDirectory(folder);
            studio.Motion.Stop();studio.Rig.SetPose(0);
            using(var target=new BenchmarkRenderTarget(1100,1400))
            {
                foreach(int option in new[]{0,1})
                {
                    field.index=option;studio.SetView(3);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+(option==0?"glasses-face":"mask-face")+".png");
                    studio.SetView(0);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+(option==0?"glasses-full":"mask-full")+".png");
                }
                var reviewCameraObject=new GameObject("Equipment detail evidence camera");var camera=reviewCameraObject.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;
                try
                {
                    camera.fieldOfView=37f;
                    foreach(var item in new[]{("mask-side-detail",new Vector3(.72f,1.70f,.03f),new Vector3(0,1.69f,0)),("mask-rear-detail",new Vector3(.50f,1.72f,-.60f),new Vector3(0,1.70f,0)),("rifle-detail",new Vector3(.7f,1.38f,-1.9f),new Vector3(0,1.36f,-.18f)),("rifle-back-full",new Vector3(0,1.02f,-3.8f),new Vector3(0,1.02f,0)),("rifle-three-quarter-full",new Vector3(2.4f,1.2f,-3.2f),new Vector3(0,1.02f,0))})
                    {
                        camera.transform.position=studio.Rig.transform.TransformPoint(item.Item2);camera.transform.LookAt(studio.Rig.transform.TransformPoint(item.Item3));
                        yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+item.Item1+".png");
                    }
                }
                finally{Object.Destroy(reviewCameraObject);}
                studio.SetView(1);studio.Motion.MainReference.State=SkiMotionState.Tuck;studio.Motion.EvaluateCycle(.25f);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/mask-tuck.png");
                studio.SetView(4);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/rifle-tuck.png");
            }
            field.index=0;studio.Motion.Stop();studio.Rig.SetPose(0);studio.SetView(0);
        }
    }
}
#endif

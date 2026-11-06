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
        [Serializable] private sealed class Receipt{public string equipment_detail_definition_sha256;public string[] eyewear_variants;}
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
            string folder=Path.Combine(Application.dataPath,"../artifacts/m16-7-equipment-20261106/native");Directory.CreateDirectory(folder);
            studio.Motion.Stop();studio.Rig.SetPose(0);
            using(var target=new BenchmarkRenderTarget(1100,1400))
            {
                foreach(int option in new[]{0,1})
                {
                    field.index=option;studio.SetView(3);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+(option==0?"glasses-face":"mask-face")+".png");
                    studio.SetView(0);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+(option==0?"glasses-full":"mask-full")+".png");
                }
                studio.SetView(1);studio.Motion.MainReference.State=SkiMotionState.Tuck;studio.Motion.EvaluateCycle(.25f);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/mask-tuck.png");
                studio.SetView(4);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/rifle-tuck.png");
            }
            field.index=0;studio.Motion.Stop();studio.Rig.SetPose(0);studio.SetView(0);
        }
    }
}
#endif

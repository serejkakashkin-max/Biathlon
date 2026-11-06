#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167OutfitTests
    {
        private const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        [Test] public void NaturalPrefabHasEveryExistingNationalKit()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab");
            var outfit=prefab.GetComponent<Biathlete167Outfit>();Assert.That(outfit,Is.Not.Null);
            Assert.That(outfit.Nations,Is.EqualTo(Enumerable.Range(0,AthleteAppearancePresentation.NationCount).Select(AthleteAppearancePresentation.NationAt)));
        }
        [Test] public void SwitchingKitsUsesSharedMaterialsAcrossAllLodsAndPreservesTheBib()
        {
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Biathlete_16_7_Natural.prefab"));
            try
            {
                var rig=go.GetComponent<Biathlete167Rig>();var outfit=go.GetComponent<Biathlete167Outfit>();Assert.That(outfit,Is.Not.Null);
                var skins=rig.Lods.GetLODs().SelectMany(l=>l.renderers).OfType<SkinnedMeshRenderer>().ToArray();Assert.That(skins.Length,Is.EqualTo(3));
                var bib=skins[0].sharedMaterials.Single(m=>m.name=="PULSE_Bib_Printed_Textile");var sourceTexture=bib.GetTexture("_BaseMap");
                var root=go.transform.position;
                using(var motion=new Biathlete167MainMotion(rig))
                {
                // G2 reference initialization creates its bounded shared palette.
                // Measure kit switching after that independent initialization.
                motion.Evaluate(0);int before=Resources.FindObjectsOfTypeAll<Material>().Length;
                for(int repeat=0;repeat<3;repeat++)foreach(var code in outfit.Nations)
                {
                    outfit.ApplyNation(code);
                    foreach(var skin in skins)
                    {
                        var suit=skin.sharedMaterials.Single(m=>m.name=="PULSE_Natural_Tailored_Textile");
                        Assert.That(suit.GetTexture("_BaseMap").name,Is.EqualTo("Natural_Kit_"+code+"_BaseColor_4K"));
                        Assert.That(suit.GetTexture("_BumpMap").name,Is.EqualTo("Natural_Suit_Knit_Normal_4K"));
                        Assert.That(suit.GetTexture("_MetallicGlossMap").name,Is.EqualTo("Natural_Suit_MetallicSmoothness_4K"));
                        Assert.That(Vector4.Distance(skin.sharedMaterials.Single(m=>m.name=="PULSE_Race_Red").GetColor("_BaseColor"),AthleteAppearancePresentation.NationColor(code)),Is.LessThan(.000001f),"Serialized shader colour must match the canonical nation palette.");
                        Assert.That(skin.sharedMaterials.Single(m=>m.name=="PULSE_Bib_Printed_Textile"),Is.SameAs(bib));
                    }
                    motion.State=SkiMotionState.Tuck;motion.Evaluate(.25f);Assert.That(outfit.Nation,Is.EqualTo(code));Assert.That(go.transform.position,Is.EqualTo(root));
                }
                Assert.That(bib.GetTexture("_BaseMap"),Is.SameAs(sourceTexture));Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length,Is.EqualTo(before),"Switching shared kit assets must not instantiate materials.");
                }
                Assert.Throws<ArgumentException>(()=>outfit.ApplyNation("UNKNOWN"));
            }finally{Object.DestroyImmediate(go);}
        }
        [UnityTest] public IEnumerator StudioOffersAndRendersAllSevenNationalKits()
        {
            var studio=Object.FindFirstObjectByType<Biathlete167Studio>();
            if(studio==null){yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root+"/Preview/Biathlete_16_7_Preview.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));yield return null;studio=Object.FindFirstObjectByType<Biathlete167Studio>();}
            var outfit=studio.Rig.GetComponent<Biathlete167Outfit>();Assert.That(outfit,Is.Not.Null);
            var field=studio.GetComponent<UIDocument>().rootVisualElement.Q<DropdownField>("athlete-nation");Assert.That(field,Is.Not.Null);Assert.That(field.choices.Count,Is.EqualTo(outfit.Nations.Count));
            string folder="artifacts/m16-7-textile-kits-20261106/native";Directory.CreateDirectory(folder);studio.Motion.Stop();
            using(var target=new BenchmarkRenderTarget(1100,1400))
            {
                for(int i=0;i<outfit.Nations.Count;i++)
                {
                    field.index=i;yield return null;Assert.That(outfit.Nation,Is.EqualTo(outfit.Nations[i]));
                    foreach(int view in new[]{0,2}){studio.Rig.SetPose(0);studio.SetView(view);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+outfit.Nation+(view==0?"-front":"-back")+".png");}
                    studio.SetView(1);studio.Motion.MainReference.State=SkiMotionState.Tuck;studio.Motion.EvaluateCycle(.25f);yield return null;target.Render(studio.View);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+outfit.Nation+"-tuck.png");
                }
                var cameraObject=new GameObject("Textile detail evidence camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(studio.View);camera.enabled=false;camera.fieldOfView=37;
                try
                {
                    field.index=6;studio.Rig.SetPose(0);
                    foreach(var item in new[]{("bib-hem",new Vector3(.5f,1.2f,1.1f),new Vector3(0,1.22f,0)),("harness-return",new Vector3(1.1f,1.37f,0),new Vector3(.05f,1.32f,0)),("harness-rear",new Vector3(.6f,1.45f,-.9f),new Vector3(.07f,1.35f,-.1f)),("bib-armhole",new Vector3(.65f,1.5f,.7f),new Vector3(.10f,1.42f,0)),("suit-knee",new Vector3(.55f,.55f,.95f),new Vector3(.09f,.55f,0))})
                    {camera.transform.position=studio.Rig.transform.TransformPoint(item.Item2);camera.transform.LookAt(studio.Rig.transform.TransformPoint(item.Item3));yield return null;target.Render(camera);yield return new WaitForEndOfFrame();target.SavePng(folder+"/"+item.Item1+".png");}
                }finally{Object.Destroy(cameraObject);}
            }
            field.index=0;studio.Motion.Stop();studio.Rig.SetPose(0);studio.SetView(0);
        }
    }
}
#endif

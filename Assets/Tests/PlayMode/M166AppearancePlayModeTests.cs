using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Career;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class M166AppearancePlayModeTests
    {
        private static AppearanceProfile Look(string headwear,string goggles)=>AthleteLookCatalog.Load().Edit(AppearanceProfile.Create("angular","light_warm","natural"),headwearId:headwear,gogglesId:goggles,hairStyleId:"short_swept",hairColorId:"chestnut",eyeColorId:"blue");
        [TestCase("cap","worn")][TestCase("cap","raised")][TestCase("cap","absent")]
        [TestCase("headband","worn")][TestCase("headband","raised")][TestCase("headband","absent")]
        [TestCase("none","worn")][TestCase("none","raised")][TestCase("none","absent")]
        public void AllHeadwearAndGoggleChoicesKeepIndependentCompleteVisibility(string headwear,string goggles)
        {
            var rig=AthleteRigFactory.Create(null);var other=AthleteRigFactory.Create(null);
            try
            {
                other.GetComponent<AthleteAppearancePresentation>().ApplyProfile(Look("cap","worn"));
                var a=rig.GetComponent<AthleteAppearancePresentation>();var body=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                a.ApplyProfile(Look(headwear,"raised"));a.ApplyProfile(Look(headwear,goggles));
                foreach(int slot in new[]{4,6,7})
                {
                    Assert.That(body.sharedMaterials[slot].GetFloat("_GogglesHidden"),Is.EqualTo(goggles=="absent"?1:0));
                    Assert.That(other.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials[slot].GetFloat("_GogglesHidden"),Is.Zero);
                    Assert.That(body.sharedMaterials[slot].FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));
                    Assert.That(body.sharedMaterials[slot].FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
                }
                Assert.That(body.GetBlendShapeWeight(body.sharedMesh.GetBlendShapeIndex("CreatorGogglesRaised")),Is.EqualTo(goggles=="raised"?100:0));
                Assert.That(a.AppliedProfile.HeadwearId,Is.EqualTo(headwear));
                var actual=body.sharedMaterials[3].GetColor("_EyeColor");var expected=AthleteLookCatalog.Color("eye_color","blue");
                Assert.That(actual.r,Is.EqualTo(expected.r).Within(1e-6f));Assert.That(actual.g,Is.EqualTo(expected.g).Within(1e-6f));Assert.That(actual.b,Is.EqualTo(expected.b).Within(1e-6f));
            }
            finally{Object.DestroyImmediate(rig.gameObject);Object.DestroyImmediate(other.gameObject);}
        }
        [Test] public void LensFrameAndStrapOwnershipIsCompleteInAllThreeLodsWithoutTouchingEyes()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var lod=rig.GetComponent<AthleteSkinDetailBinding>();var body=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                foreach(float distance in new[]{0,25,100})
                {
                    lod.Prepare(distance,false,true);var mesh=body.sharedMesh;var tags=new List<Vector2>();mesh.GetUVs(2,tags);
                    var moved=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(mesh.GetBlendShapeIndex("CreatorGogglesRaised"),0,moved,null,null);
                    int accessory=0,eyes=0;
                    for(int i=0;i<moved.Length;i++){if(moved[i].sqrMagnitude>1e-10f){Assert.That(tags[i].x,Is.EqualTo(7));accessory++;}if(tags[i].x==1)eyes++;}
                    foreach(int i in mesh.GetTriangles(7))Assert.That(tags[i].x,Is.EqualTo(7));
                    Assert.That(accessory,Is.GreaterThan(30));Assert.That(eyes,Is.GreaterThan(50));Assert.That(mesh.blendShapeCount,Is.EqualTo(18));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [UnityTest] public IEnumerator NativeStudioShowsUncoveredEyesHeadbandAndDeterministicRivalLooks()
        {
            var root=new GameObject("M16.6 appearance acceptance");var studio=root.AddComponent<AthleteCreatorStudio>();studio.Initialize();studio.ReducedMotion=true;
            var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(studio.PreviewCamera);quality.Apply("high");
            string output=Path.GetFullPath("docs/quality/m16-6/appearance");Directory.CreateDirectory(output);
            try
            {
                var a=studio.Rig.GetComponent<AthleteAppearancePresentation>();studio.SetView(0,1);
                foreach(string goggles in new[]{"worn","raised","absent"})
                {a.ApplyProfile(Look("headband",goggles));yield return null;yield return new WaitForEndOfFrame();Save("creator-headband-"+goggles+".png");}
                a.ApplyProfile(Look("none","absent"));yield return null;yield return new WaitForEndOfFrame();Save("creator-no-headwear.png");
                var generator=new CareerRivalAppearance(id=>ProductionVenueHost.ReadResource("Biathlon/"+id.Substring(0,id.Length-5)));
                for(int i=0;i<6;i++)
                {a.ApplyProfile(generator.Generate("m166_rival_"+i));a.ApplyNation(AthleteAppearancePresentation.NationAt(i));yield return null;yield return new WaitForEndOfFrame();Save("rival-"+i+".png");}
            }
            finally{Object.DestroyImmediate(root);}
            void Save(string file)
            {
                var old=RenderTexture.active;var image=new Texture2D(studio.Texture.width,studio.Texture.height,TextureFormat.RGB24,false);
                try{RenderTexture.active=studio.Texture;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,file),image.EncodeToPNG());}
                finally{RenderTexture.active=old;Object.Destroy(image);}
            }
        }
    }
}

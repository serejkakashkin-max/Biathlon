using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteCreatorAppearanceTests
    {
        private GameObject root;
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);}
        private AthleteRigBinding Rig(){root=new GameObject("Creator appearance verification");return AthleteRigFactory.Create(root.transform);}
        private static AppearanceProfile Profile(int height=1850,int shoulders=5000)=>AthleteLookCatalog.Load().Edit(AppearanceProfile.Create("defined_chin","olive","natural"),heightMillimeters:height,shoulderWidthBasis:shoulders,hairStyleId:"short_swept",hairColorId:"blond",eyeColorId:"blue",headwearId:"headband",gogglesId:"raised",lensId:"amber",glovesId:"ivory",bootsId:"red",trainingAccentId:"sunrise");
        [Test] public void DetailsUseOneApprovedSkinEightSlotsAndActualCosmeticMorphsAtEveryLod()
        {
            var rig=Rig();var look=rig.GetComponent<AthleteAppearancePresentation>();look.ApplyNation("ITA");look.ApplyProfile(Profile(1980,10000));
            var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();var lod=rig.GetComponent<AthleteSkinDetailBinding>();var firstPalette=skin.sharedMaterials;
            Assert.That(rig.GetComponentsInChildren<SkinnedMeshRenderer>().Length,Is.EqualTo(1));Assert.That(skin.sharedMaterials.Length,Is.EqualTo(8));
            Assert.That(rig.transform.localScale.x,Is.EqualTo(1980/1850f).Within(.00001));
            foreach(float distance in new[]{0f,20f,70f})
            {
                lod.Prepare(distance,false,true);var mesh=skin.sharedMesh;
                Assert.That(mesh.blendShapeCount,Is.EqualTo(18));Assert.That(mesh.GetBlendShapeIndex("CreatorShouldersWide"),Is.EqualTo(14));
                Assert.That(skin.GetBlendShapeWeight(14),Is.EqualTo(100));Assert.That(skin.GetBlendShapeWeight(15),Is.EqualTo(100));Assert.That(skin.GetBlendShapeWeight(16),Is.EqualTo(100));Assert.That(skin.GetBlendShapeWeight(17),Is.EqualTo(100));
                var tag=new List<Vector2>();mesh.GetUVs(2,tag);int eyes=0,hair=0,hands=0,boots=0;
                foreach(var t in tag){if(t.x==1)eyes++;if(t.x==2)hair++;if(t.x==3)hands++;if(t.x==4)boots++;}
                Assert.That(eyes,Is.GreaterThan(50));Assert.That(hair,Is.GreaterThan(10));Assert.That(hands,Is.GreaterThan(10));Assert.That(boots,Is.GreaterThan(10));
                Assert.That(skin.sharedMaterials,Is.EqualTo(firstPalette));
            }
            SameColor(firstPalette[3].GetColor("_EyeColor"),AthleteLookCatalog.Color("eye_color","blue"));
            SameColor(firstPalette[4].GetColor("_GloveColor"),AthleteLookCatalog.Color("gloves","ivory"));
            SameColor(firstPalette[5].GetColor("_BootColor"),AthleteLookCatalog.Color("boots","red"));
            Assert.That(firstPalette[6].GetFloat("_HeadwearMode"),Is.EqualTo(1));SameColor(firstPalette[7].GetColor("_LensColor"),AthleteLookCatalog.Color("lens","amber"));
        }
        [Test] public void TrainingAccentCannotRepaintOfficialNationalKitOrAnotherAthlete()
        {
            var rig=Rig();var other=AthleteRigFactory.Create(root.transform);var a=rig.GetComponent<AthleteAppearancePresentation>();var b=other.GetComponent<AthleteAppearancePresentation>();
            a.ApplyNation("NOR");b.ApplyNation("NOR");var profile=Profile();a.ApplyProfile(profile);b.ApplyProfile(profile);
            var ar=rig.GetComponentInChildren<SkinnedMeshRenderer>();var br=other.GetComponentInChildren<SkinnedMeshRenderer>();var original=ar.sharedMaterials[1];var bib=ar.sharedMaterials[2];
            a.ShowTrainingKit(true);Assert.That(ar.sharedMaterials[1],Is.Not.SameAs(original));Assert.That(br.sharedMaterials[1],Is.SameAs(original));Assert.That(ar.sharedMaterials[2],Is.SameAs(bib));
            Assert.That(a.Nation,Is.EqualTo("NOR"));a.ShowTrainingKit(false);Assert.That(ar.sharedMaterials[1],Is.SameAs(original));
            a.ApplyProfile(AppearanceProfile.Legacy("approved_legacy"));Assert.That(rig.transform.localScale,Is.EqualTo(Vector3.one));for(int i=13;i<18;i++)Assert.That(ar.GetBlendShapeWeight(i),Is.Zero);
        }
        [TestCase(1700,0)] [TestCase(1980,10000)]
        public void DimensionsRetainPoleGripFootGroundAndShootingSightContracts(int height,int shoulders)
        {
            var rig=Rig();rig.GetComponent<AthleteAppearancePresentation>().ApplyProfile(Profile(height,shoulders));var motion=rig.GetComponent<AthleteHybridLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();var contact=rig.GetComponent<AthleteGroundContact>();var grip=rig.GetComponent<AthleteGripPresentation>();
            foreach(float time in new[]{0f,.2f,.5f,.8f})
            {
                motion.Sample(rig,new LocomotionInput(9,.06f,1.2f,sprint:true),time);rifle.Sample(rig,0);contact.SampleMoving(rig,.06f,motion.PoleCycle,motion.ContactState,0,time);motion.ApplySkiEdges(rig);grip.Render(rig);
                Assert.That(grip.MaximumResidualMeters,Is.LessThan(.02f));Assert.That(contact.MaxFootResidualMeters,Is.LessThan(.02f));
            }
            foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
            {
                rig.GetComponent<AthleteShootingPose>().Sample(rig,stance,1);rifle.Sample(rig,1,stance:stance);grip.Render(rig);
                Assert.That(grip.MaximumResidualMeters,Is.LessThan(.025f));var weapon=rig.GetComponent<AthleteEquipmentBinding>().Rifle;
                Assert.That(Vector3.Dot(weapon.forward,rig.ModelRoot.forward),Is.GreaterThan(.999f),"Cosmetic height cannot rotate the authoritative aim ray.");
            }
        }
        [UnityTest] public IEnumerator NativeStudioCapturesGenuineEyesHairHeadwearAndDimensionChoices()
        {
            root=new GameObject("Creator cosmetic captures");var studio=root.AddComponent<AthleteCreatorStudio>();studio.Initialize();studio.ReducedMotion=true;
            var a=studio.Rig.GetComponent<AthleteAppearancePresentation>();a.ApplyNation("FIN");a.ApplyProfile(Profile());studio.SetView(12,1);
            yield return null;yield return new WaitForEndOfFrame();Save(studio.Texture,"creator-hair-eyes.png");
            a.ApplyProfile(AthleteLookCatalog.Load().Edit(Profile(),heightMillimeters:1700,shoulderWidthBasis:0,hairStyleId:"crew",hairColorId:"black",eyeColorId:"brown",skinId:"dark_deep",headwearId:"none",glovesId:"crimson",bootsId:"ice_white"));studio.SetView(0,1);
            yield return null;yield return new WaitForEndOfFrame();Save(studio.Texture,"creator-uncovered-face.png");
            a.ApplyProfile(Profile(1980,10000));studio.SetView(25,0);a.ShowTrainingKit(true);
            yield return null;yield return new WaitForEndOfFrame();Save(studio.Texture,"creator-full-equipment.png");
        }
        private static void Save(RenderTexture target,string file)
        {
            var old=RenderTexture.active;var t=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=target;t.ReadPixels(new Rect(0,0,target.width,target.height),0,0);t.Apply();File.WriteAllBytes(Path.GetFullPath("docs/quality/m15-6/"+file),t.EncodeToPNG());}
            finally{RenderTexture.active=old;Object.Destroy(t);}
        }
        private static void SameColor(Color actual,Color expected)
        {Assert.That(actual.r,Is.EqualTo(expected.r).Within(.00001));Assert.That(actual.g,Is.EqualTo(expected.g).Within(.00001));Assert.That(actual.b,Is.EqualTo(expected.b).Within(.00001));Assert.That(actual.a,Is.EqualTo(expected.a).Within(.00001));}
    }
}

using System;
using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class AthleteFaceTests
    {
        [Test] public void StableIdentitiesCoverIndependentSkinAndFaceChoices()
        {
            var recipe=AthleteFaceCatalog.Load();var faces=new HashSet<int>();var tones=new HashSet<int>();var pairs=new HashSet<int>();
            for(int i=0;i<512;i++)
            {
                string id="career-athlete-"+i;recipe.Resolve(id,out int face,out int tone);recipe.Resolve(id,out int againFace,out int againTone);
                Assert.That(againFace,Is.EqualTo(face));Assert.That(againTone,Is.EqualTo(tone));faces.Add(face);tones.Add(tone);pairs.Add(face*recipe.skin_colors.Length+tone);
            }
            Assert.That(faces.Count,Is.EqualTo(recipe.profiles.Length));Assert.That(tones.Count,Is.EqualTo(recipe.skin_colors.Length));
            Assert.That(pairs.Count,Is.EqualTo(recipe.profiles.Length*recipe.skin_colors.Length),"Skin tone and facial structure must not be coupled.");
            float lightest=0,darkest=1;for(int i=0;i<recipe.skin_colors.Length;i++){float light=recipe.SkinColor(i).grayscale;lightest=Mathf.Max(lightest,light);darkest=Mathf.Min(darkest,light);}
            Assert.That(lightest-darkest,Is.GreaterThan(.4f));Assert.Throws<ArgumentException>(()=>recipe.Resolve(" ",out _,out _));
        }
        [Test] public void FaceAndToneSurviveLodNationBibAndLeaderChanges()
        {
            var a=AthleteRigFactory.Create(null);var b=AthleteRigFactory.Create(null);
            try
            {
                var aa=a.GetComponent<AthleteAppearancePresentation>();var bb=b.GetComponent<AthleteAppearancePresentation>();
                aa.ApplyIdentity("saved-athlete-42");bb.ApplyIdentity("saved-athlete-42");aa.ApplyNation("NOR");bb.ApplyNation("FRA");
                var skin=a.GetComponentInChildren<SkinnedMeshRenderer>();var other=b.GetComponentInChildren<SkinnedMeshRenderer>();
                var skinMaterial=skin.sharedMaterials[3];Assert.That(other.sharedMaterials[3],Is.SameAs(skinMaterial));
                int tone=aa.SkinVariant,face=aa.FaceVariant;var props=a.GetComponent<AthleteEquipmentBinding>();var rifle=props.Rifle.position;var root=a.transform.position;
                foreach(float distance in new[]{70,20,0})
                {
                    a.GetComponent<AthleteSkinDetailBinding>().Prepare(distance,false,true);
                    a.GetComponent<AthleteBibPresentation>().SetNumber(23);a.GetComponent<AthleteBibPresentation>().SetLeader(CareerLeaderBib.Total|CareerLeaderBib.Discipline);
                    aa.ApplyNation("GER");aa.ApplyIdentity("saved-athlete-42");
                    Assert.That(aa.SkinVariant,Is.EqualTo(tone));Assert.That(aa.FaceVariant,Is.EqualTo(face));Assert.That(skin.sharedMaterials[3],Is.SameAs(skinMaterial));
                    var recipe=AthleteFaceCatalog.Load();for(int i=0;i<recipe.profiles.Length;i++)
                        Assert.That(skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(recipe.profiles[i].ShapeName)),Is.EqualTo(i==face?100:0));
                    var block=new MaterialPropertyBlock();skin.GetPropertyBlock(block,3);Assert.That(block.isEmpty,Is.True,"Keep skin material sharing/SRP batching.");
                    skin.GetPropertyBlock(block,2);Assert.That(block.GetFloat("_BibNumber"),Is.EqualTo(23));
                }
                Assert.That(props.Rifle.position,Is.EqualTo(rifle));Assert.That(a.transform.position,Is.EqualTo(root));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<200;i++){aa.ApplyIdentity("saved-athlete-42");aa.ApplyNation("GER");}
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test] public void OneCountryCanContainAllSkinTonesAndMaterialCountIsBounded()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var appearance=rig.GetComponent<AthleteAppearancePresentation>();appearance.ApplyNation("NOR");var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                var suit=skin.sharedMaterials[1];var source=Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/Skin");var original=source.color;
                var materials=new HashSet<Material>();for(int i=0;i<256;i++){appearance.ApplyIdentity("roster-"+i);materials.Add(skin.sharedMaterials[3]);Assert.That(skin.sharedMaterials[1],Is.SameAs(suit));}
                Assert.That(materials.Count,Is.EqualTo(AthleteFaceCatalog.Load().skin_colors.Length));Assert.That(source.color,Is.EqualTo(original));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FaceMorphsRetainEveryJointBuildAndOnlyMoveSkinAboveTheNeck(int lod)
        {
            var mesh=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD"+lod);var vertices=mesh.vertices;
            var allowed=new bool[mesh.vertexCount];foreach(int index in mesh.GetTriangles(3))allowed[index]=true;
            var recipe=AthleteFaceCatalog.Load();Assert.That(mesh.blendShapeCount,Is.EqualTo(7+recipe.profiles.Length));
            var delta=new Vector3[mesh.vertexCount];var normals=new Vector3[mesh.vertexCount];var tangents=new Vector3[mesh.vertexCount];
            foreach(var profile in recipe.profiles)
            {
                int shape=mesh.GetBlendShapeIndex(profile.ShapeName);Assert.That(shape,Is.GreaterThanOrEqualTo(7));
                mesh.GetBlendShapeFrameVertices(shape,0,delta,normals,tangents);int affected=0;
                for(int i=0;i<delta.Length;i++)
                {
                    Assert.That(float.IsFinite(delta[i].sqrMagnitude)&&float.IsFinite(normals[i].sqrMagnitude),Is.True);
                    if(delta[i].sqrMagnitude<1e-14f)continue;affected++;
                    Assert.That(allowed[i],Is.True);Assert.That(vertices[i].y,Is.InRange(1.585f,1.710f));Assert.That(delta[i].magnitude,Is.LessThan(.02f));
                }
                Assert.That(affected,Is.GreaterThan(10),"Each LOD needs real facial geometry, not only a different material.");
            }
            for(int i=0;i<3;i++)Assert.That(mesh.GetBlendShapeIndex("OriginalBuild"+i),Is.EqualTo(4+i));
        }
        [Test] public void InvalidOrDuplicateFaceDefinitionsAreRejected()
        {
            var json=Resources.Load<TextAsset>("Biathlon/athlete_faces_v1").text;
            var value=JsonUtility.FromJson<AthleteFaceCatalog>(json);value.profiles[0].jaw_width_m=float.NaN;Assert.Throws<ArgumentException>(()=>value.Validate());
            value=JsonUtility.FromJson<AthleteFaceCatalog>(json);value.profiles[1].id=value.profiles[0].id;Assert.Throws<ArgumentException>(()=>value.Validate());
            value=JsonUtility.FromJson<AthleteFaceCatalog>(json);value.skin_colors[1]=value.skin_colors[0];Assert.Throws<ArgumentException>(()=>value.Validate());
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FaceMorphsStayContinuousAtTheMirroredMidline(int lod)
        {
            var mesh=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD"+lod);var vertices=mesh.vertices;
            var allowed=new bool[vertices.Length];foreach(int i in mesh.GetTriangles(3))allowed[i]=true;
            var delta=new Vector3[vertices.Length];int checkedVertices=0;
            for(int shape=4;shape<mesh.blendShapeCount;shape++)
            {
                mesh.GetBlendShapeFrameVertices(shape,0,delta,null,null);
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=vertices[i];if(!allowed[i]||Mathf.Abs(p.x)>.003f||p.y<1.59f||p.y>1.68f||p.z<.05f)continue;
                    checkedVertices++;Assert.That(Mathf.Abs(delta[i].x),Is.LessThan(.0003f+.25f*Mathf.Abs(p.x)),mesh.GetBlendShapeName(shape)+" must neither split nor overlap the face centre.");
                }
            }
            Assert.That(checkedVertices,Is.GreaterThan(10));
        }
    }
}

using Biathlon.Unity;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.VenueEditorTests
{
    public sealed class AthleteFaceAuthoringTests
    {
        [Test] public void NeutralFaceDoesNotReplaceAuthoredNormalsWithDifferentSmoothing()
        {
            var copy=Object.Instantiate(Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD2"));
            try
            {
                var recipe=JsonUtility.FromJson<AthleteFaceCatalog>(Resources.Load<TextAsset>("Biathlon/athlete_faces_v1").text);
                recipe.profiles[0]=new AthleteFaceProfile{id="neutral"};recipe.Validate();AthleteFaceAuthoring.Apply(copy,recipe);
                var delta=new Vector3[copy.vertexCount];var normals=new Vector3[copy.vertexCount];
                copy.GetBlendShapeFrameVertices(copy.GetBlendShapeIndex("FaceIdentity_neutral"),0,delta,normals,null);
                Assert.That(delta,Is.All.EqualTo(Vector3.zero));Assert.That(normals,Is.All.EqualTo(Vector3.zero));
            }
            finally{Object.DestroyImmediate(copy);}
        }
        [Test] public void RegenerationPreservesApprovedMeshRigAndExistingBlendShapesExactly()
        {
            var source=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD2");var copy=Object.Instantiate(source);
            try
            {
                var vertices=copy.vertices;var bones=copy.boneWeights;var bind=copy.bindposes;var indices=copy.triangles;
                var prior=new Vector3[7][];for(int i=0;i<prior.Length;i++){prior[i]=new Vector3[copy.vertexCount];copy.GetBlendShapeFrameVertices(i,0,prior[i],null,null);}
                var recipe=AthleteFaceCatalog.Load();AthleteFaceAuthoring.Apply(copy,recipe);AthleteFaceAuthoring.Apply(copy,recipe);
                Assert.That(copy.blendShapeCount,Is.EqualTo(12+recipe.profiles.Length));Assert.That(copy.vertices,Is.EqualTo(vertices));
                Assert.That(copy.boneWeights,Is.EqualTo(bones));Assert.That(copy.bindposes,Is.EqualTo(bind));Assert.That(copy.triangles,Is.EqualTo(indices));
                for(int i=0;i<prior.Length;i++){var after=new Vector3[copy.vertexCount];copy.GetBlendShapeFrameVertices(i,0,after,null,null);Assert.That(after,Is.EqualTo(prior[i]));}
            }
            finally{Object.DestroyImmediate(copy);}
        }
    }
}

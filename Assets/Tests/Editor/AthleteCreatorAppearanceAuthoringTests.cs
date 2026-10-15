using System.IO;
using Biathlon.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.VenueEditorTests
{
    public sealed class AthleteCreatorAppearanceAuthoringTests
    {
        [Test] public void RegenerationRetainsOriginalGeometrySkinningAndFirstThirteenMorphsExactly()
        {
            var source=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD2");var copy=Object.Instantiate(source);
            try
            {
                var vertices=copy.vertices;var bones=copy.boneWeights;var bind=copy.bindposes;var triangles=copy.triangles;var channels=new Vector3[13][];
                for(int i=0;i<channels.Length;i++){channels[i]=new Vector3[copy.vertexCount];copy.GetBlendShapeFrameVertices(i,0,channels[i],null,null);}
                string eyes=File.ReadAllText("artifacts/g2-creator/eyes_lod2.json");AthleteCreatorAppearanceAuthoring.Apply(copy,eyes);AthleteCreatorAppearanceAuthoring.Apply(copy,eyes);
                Assert.That(copy.vertices,Is.EqualTo(vertices));Assert.That(copy.boneWeights,Is.EqualTo(bones));Assert.That(copy.bindposes,Is.EqualTo(bind));Assert.That(copy.triangles,Is.EqualTo(triangles));Assert.That(copy.blendShapeCount,Is.EqualTo(18));
                for(int i=0;i<channels.Length;i++){var after=new Vector3[copy.vertexCount];copy.GetBlendShapeFrameVertices(i,0,after,null,null);Assert.That(after,Is.EqualTo(channels[i]),copy.GetBlendShapeName(i));}
            }
            finally{Object.DestroyImmediate(copy);}
        }
    }
}

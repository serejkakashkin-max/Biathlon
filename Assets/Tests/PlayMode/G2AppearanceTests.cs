using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2AppearanceTests
    {
        [Test] public void KitsVaryPerOrdinalWithoutSharedMaterialOrBibMutation()
        {
            var a=AthleteRigFactory.Create(null);var b=AthleteRigFactory.Create(null);
            try
            {
                var ar=a.GetComponentInChildren<SkinnedMeshRenderer>();var br=b.GetComponentInChildren<SkinnedMeshRenderer>();
                var material=ar.sharedMaterials[1];var color=material.GetColor("_BaseColor");
                a.GetComponent<AthleteBibPresentation>().SetNumber(1);b.GetComponent<AthleteBibPresentation>().SetNumber(2);
                var block=new MaterialPropertyBlock();var first=ar.sharedMaterials[1];
                Assert.That(br.sharedMaterials[1].GetColor("_KitColor"),Is.Not.EqualTo(first.GetColor("_KitColor")));Assert.That(material.GetColor("_BaseColor"),Is.EqualTo(color));
                ar.GetPropertyBlock(block,1);Assert.That(block.isEmpty,Is.True,"Surface blocks break SRP batching.");
                ar.GetPropertyBlock(block,2);Assert.That(block.GetFloat("_BibNumber"),Is.EqualTo(1));
                br.GetPropertyBlock(block,2);Assert.That(block.GetFloat("_BibNumber"),Is.EqualTo(2));
                a.GetComponent<AthleteBibPresentation>().SetNumber(7);Assert.That(ar.sharedMaterials[1],Is.SameAs(first));
                var appearance=a.GetComponent<AthleteAppearancePresentation>();long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)appearance.Apply(7);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.Throws<System.ArgumentOutOfRangeException>(()=>appearance.Apply(0));
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);}
        }
        [Test] public void EveryLodRetainsBindCoordinatesAndFittedLensEnvelope()
        {
            for(int lod=0;lod<3;lod++)
            {
                var mesh=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroLOD"+lod);var uv=new List<Vector3>();mesh.GetUVs(1,uv);
                Assert.That(uv.Count,Is.EqualTo(mesh.vertexCount));var vertices=mesh.vertices;
                for(int i=0;i<uv.Count;i++)Assert.That(Vector3.Distance(uv[i],vertices[i]),Is.LessThan(.000001f));
                var bounds=new Bounds(vertices[mesh.GetIndices(7)[0]],Vector3.zero);foreach(int i in mesh.GetIndices(7))bounds.Encapsulate(vertices[i]);
                Assert.That(bounds.size.x,Is.InRange(.13f,.155f));Assert.That(bounds.size.y,Is.InRange(.04f,.055f));
                Assert.That(bounds.min.z,Is.GreaterThan(.11f));
            }
        }
    }
}

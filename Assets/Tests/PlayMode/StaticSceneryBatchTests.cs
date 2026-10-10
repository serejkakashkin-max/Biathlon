using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class StaticSceneryBatchTests
    {
        [Test] public void FixedGeometryCombinesByMaterialAndKeepsWorldBounds()
        {
            var root=new GameObject("batch test");var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                var a=GameObject.CreatePrimitive(PrimitiveType.Cube);a.transform.SetParent(root.transform);a.transform.position=new Vector3(4,2,4);
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.transform.SetParent(root.transform);b.transform.position=new Vector3(8,2,4);
                var ar=a.GetComponent<MeshRenderer>();var br=b.GetComponent<MeshRenderer>();ar.sharedMaterial=br.sharedMaterial=material;
                var original=ar.bounds;original.Encapsulate(br.bounds);
                var batch=root.AddComponent<StaticSceneryBatches>();batch.Initialize(new[]{ar,br},32);
                Assert.That(batch.SourceRendererCount,Is.EqualTo(2));Assert.That(batch.BatchRendererCount,Is.EqualTo(1));Assert.That(ar.enabled||br.enabled,Is.False);
                var renderers=root.GetComponentsInChildren<MeshRenderer>();MeshRenderer combined=null;foreach(var r in renderers)if(r.enabled)combined=r;
                Assert.That(combined,Is.Not.Null);Assert.That(Vector3.Distance(combined.bounds.min,original.min),Is.LessThan(.0001f));Assert.That(Vector3.Distance(combined.bounds.max,original.max),Is.LessThan(.0001f));
                Assert.That(combined.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0),Is.EqualTo(72));
                Object.DestroyImmediate(batch);Assert.That(ar.enabled&&br.enabled,Is.True);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
        }
        [Test] public void PerObjectStateAndDistantCellsRemainIndependent()
        {
            var root=new GameObject("batch exclusions");var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                var rs=new MeshRenderer[3];for(int i=0;i<3;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.transform.SetParent(root.transform);go.transform.position=new Vector3(i*64,0,0);rs[i]=go.GetComponent<MeshRenderer>();rs[i].sharedMaterial=material;}
                var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",Color.red);rs[2].SetPropertyBlock(properties);
                var batch=root.AddComponent<StaticSceneryBatches>();batch.Initialize(rs,32);
                Assert.That(batch.SourceRendererCount,Is.EqualTo(2));Assert.That(batch.BatchRendererCount,Is.EqualTo(2));Assert.That(rs[2].enabled,Is.True);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
        }
    }
}

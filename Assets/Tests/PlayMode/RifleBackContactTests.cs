using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.PlayModeTests
{
    public class RifleBackContactTests
    {
        [TestCase(6f,false)]
        [TestCase(9f,false)]
        [TestCase(12f,true)]
        public void StockStaysCloseToTheActualSkinnedBackSurface(float speed,bool tuck)
        {
            var rig=AthleteRigFactory.Create(null);var baked=new Mesh();
            try
            {
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var r=rig.GetComponent<AthleteRiflePresentation>();
                var weapon=rig.GetComponent<AthleteEquipmentBinding>().Rifle;
                var gaps=new float[8];int sample=0;
                foreach(float time in new[]{0f,.2f,.5f,.8f})
                {
                    d.Sample(rig,new LocomotionInput(speed,0,tuck?.7f:speed>8?1.25f:.95f,tuck:tuck,sprint:!tuck&&speed>8),time);r.Sample(rig,0);
                    skin.BakeMesh(baked);var vertices=baked.vertices;var triangles=baked.triangles;
                    foreach(float along in new[]{.15f,.30f})
                    {
                        var origin=weapon.TransformPoint(new Vector3(-.033f,0,along));var direction=-weapon.right;
                        float gap=float.PositiveInfinity;
                        for(int i=0;i<triangles.Length;i+=3)
                        {
                            var a=skin.transform.TransformPoint(vertices[triangles[i]]);var b=skin.transform.TransformPoint(vertices[triangles[i+1]]);var c=skin.transform.TransformPoint(vertices[triangles[i+2]]);
                            var e=b-a;var f=c-a;var p=Vector3.Cross(direction,f);float determinant=Vector3.Dot(e,p);
                            if(Mathf.Abs(determinant)<.0000001f)continue;
                            float inverse=1/determinant;var t=origin-a;float u=Vector3.Dot(t,p)*inverse;
                            if(u<0||u>1)continue;var q=Vector3.Cross(t,e);float v=Vector3.Dot(direction,q)*inverse;
                            if(v<0||u+v>1)continue;float distance=Vector3.Dot(f,q)*inverse;
                            if(distance>=0)gap=Mathf.Min(gap,distance);
                        }
                        gaps[sample++]=gap;
                    }
                }
                Assert.That(gaps,Is.All.LessThan(.018f),"Stock must rest against harness/back, not float. Sampled gaps: "+string.Join(", ",gaps));
            }
            finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(rig.gameObject);}
        }
    }
}

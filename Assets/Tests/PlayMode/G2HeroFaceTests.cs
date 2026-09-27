using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class G2HeroFaceTests
    {
        [Test] public void RaceCapFitsHeadWidthAndDoesNotFormAnOversizedDome()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var face=rig.GetComponent<AthleteFaceDetailBinding>();var detail=rig.GetComponent<AthleteSkinDetailBinding>();
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                for(int lod=0;lod<3;lod++)
                {
                    detail.Prepare(lod==0?1:lod==1?20:80,false,true);
                    var bounds=face.Cap.sharedMesh.bounds;
                    Assert.That(skin.sharedMesh.GetIndexCount(6),Is.Zero,"Old cap must not overlap the fitted replacement.");
                    Assert.That(bounds.size.x,Is.LessThanOrEqualTo(face.Face.sharedMesh.bounds.size.x+.015f),"Close-fitting cap must not overhang temples by more than 7.5 mm per side.");
                    Assert.That(bounds.size.y,Is.InRange(.08f,.105f),"Race cap is a fitted textile crown, not a tall helmet.");
                    Assert.That(face.Face.sharedMesh.bounds.max.y,Is.LessThanOrEqualTo(bounds.min.y+.003f),"Scalp must terminate under the cap band instead of piercing the crown.");
                }
                var gaiter=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/NeckGaiter");var points=gaiter.vertices;
                var actual=new Bounds(points[0],Vector3.zero);foreach(var p in points)actual.Encapsulate(p);
                Assert.That(Vector3.Distance(actual.center,gaiter.bounds.center),Is.LessThan(.00001f),"Regenerated assets must update native vertex buffers as well as serialized bounds.");
                Assert.That(Vector3.Distance(actual.size,gaiter.bounds.size),Is.LessThan(.00001f));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void LicensedFaceFollowsHeadAndBodyLodWithNoWarmAllocations()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var face=rig.GetComponent<AthleteFaceDetailBinding>();Assert.That(face,Is.Not.Null);face.Validate();
                var body=rig.GetComponent<AthleteSkinDetailBinding>();var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();
                var local=face.Face.transform.localToWorldMatrix;
                rig.Rotate(AthleteBone.Head,Quaternion.Euler(0,25,0));Assert.That(face.Face.transform.localToWorldMatrix,Is.Not.EqualTo(local));
                int previous=int.MaxValue;
                for(int i=0;i<3;i++)
                {
                    body.Prepare(i==0?1:i==1?20:80,false,true);Assert.That(face.Level,Is.EqualTo(i));
                    var mesh=face.Face.sharedMesh;Assert.That(mesh.GetIndexCount(0),Is.LessThan(previous));previous=(int)mesh.GetIndexCount(0);
                    Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));Assert.That(mesh.bounds.size.x,Is.InRange(.13f,.20f));
                    Assert.That(skin.sharedMesh.GetIndexCount(3),Is.Zero,"The original anatomical head must not overlap its replacement.");
                }
                body.Prepare(80,true,true);Assert.That(face.Level,Is.Zero,"Shooting retains face detail.");
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++){body.Prepare(80,false,true);body.Prepare(1,false,true);}
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.Throws<ArgumentOutOfRangeException>(()=>face.SetLevel(3));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}

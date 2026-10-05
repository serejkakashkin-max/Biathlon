using System;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RoadsideEventTests
    {
        [Test]
        public void ExplicitStandingPosePreservesDefaultSeatedAndCapturedAnchors()
        {
            var source=new GameObject("explicit spectator source");var observer=new GameObject("roadside observer");
            try
            {
                var seating=source.AddComponent<SpectatorSeating>();Assert.That(seating.Pose,Is.EqualTo(SpectatorPose.Seated));
                var anchors=new[]{Vector3.zero,Vector3.right};seating.Configure(anchors,SpectatorPose.Standing);
                Assert.Throws<ArgumentException>(()=>seating.Configure(anchors,(SpectatorPose)27));
                Assert.That(seating.Pose,Is.EqualTo(SpectatorPose.Standing));
                var crowd=source.AddComponent<SpectatorCrowd>();
                Assert.Throws<ArgumentException>(()=>crowd.Initialize(seating,useAuditedCrowd:true));
                crowd.Initialize(seating,useSimplifiedShadows:true);Assert.That(crowd.Pose,Is.EqualTo(SpectatorPose.Standing));
                anchors[0]=Vector3.one*900;seating.Configure(new[]{new Vector3(50,0,0)});
                Assert.That(seating.Pose,Is.EqualTo(SpectatorPose.Seated));
                var camera=observer.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,1,-12);camera.transform.LookAt(Vector3.up);
                crowd.Prepare(camera,"high",0);Assert.That(crowd.Total,Is.EqualTo(2));Assert.That(crowd.Visible,Is.EqualTo(2));
                Assert.That(crowd.Pose,Is.EqualTo(SpectatorPose.Standing));
                Assert.That(crowd.SubmittedTriangles,Is.EqualTo(2L*Resources.Load<Mesh>("Biathlon/Crowd/StandingSpectatorLOD0").GetIndexCount(0)/3));
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(observer);}
        }

        [Test]
        public void StandingRoadsideCrowdUsesRealFeetAndDistinctUprightLegsAtBothLods()
        {
            var prefab=Resources.Load<GameObject>("Biathlon/Roadside/StandingCrowd16");Assert.That(prefab,Is.Not.Null);
            var source=Object.Instantiate(prefab);var observer=new GameObject("standing crowd observer");
            try
            {
                var seating=source.GetComponent<SpectatorSeating>();Assert.That(seating.Pose,Is.EqualTo(SpectatorPose.Standing));Assert.That(seating.Count,Is.EqualTo(16));
                var anchors=seating.CopyAnchors();foreach(var anchor in anchors)Assert.That(anchor.y,Is.Zero);
                for(int i=0;i<anchors.Length;i++)for(int j=0;j<i;j++)Assert.That(Vector3.Distance(anchors[i],anchors[j]),Is.GreaterThanOrEqualTo(.89f));
                Assert.That(source.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(source.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);
                Assert.That(source.GetComponentsInChildren<SpectatorCrowd>(),Is.Empty);Assert.That(source.transform.localScale,Is.EqualTo(Vector3.one));
                Mesh near=null,far=null;
                for(int lod=0;lod<2;lod++)
                {
                    var mesh=Resources.Load<Mesh>("Biathlon/Crowd/StandingSpectatorLOD"+lod);Assert.That(mesh,Is.Not.Null);
                    Assert.That(mesh.bounds.min.y,Is.EqualTo(0).Within(.000001f));Assert.That(mesh.bounds.max.y,Is.InRange(1.85f,1.90f));
                    // Seated legs extend 0.56m forward; standing boots/legs remain beneath the hips.
                    foreach(var vertex in mesh.vertices)if(vertex.y<.75f)Assert.That(Mathf.Abs(vertex.z),Is.LessThan(.24f));
                    var pivots=new List<Vector4>();mesh.GetUVs(0,pivots);int armVertices=0;
                    foreach(var pivot in pivots)if(pivot.w!=0){armVertices++;Assert.That(pivot.y,Is.EqualTo(1.45f).Within(.000001f));}
                    Assert.That(armVertices,Is.GreaterThan(0));if(lod==0)near=mesh;else far=mesh;
                }
                Assert.That(far.GetIndexCount(0),Is.LessThan(near.GetIndexCount(0)));
                var crowd=source.AddComponent<SpectatorCrowd>();crowd.Initialize(seating,useSimplifiedShadows:true);
                var camera=observer.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,1.5f,-15);camera.transform.LookAt(new Vector3(0,1,.5f));
                crowd.Render(camera,"high",12);Assert.That(crowd.Near,Is.EqualTo(16));Assert.That(crowd.Far,Is.Zero);
                Assert.That(crowd.SubmittedTriangles,Is.EqualTo(16L*near.GetIndexCount(0)/3));Assert.That(crowd.UsesAuditedCrowd,Is.False);
                camera.transform.position=new Vector3(0,1.5f,-50);camera.transform.LookAt(new Vector3(0,1,.5f));crowd.Render(camera,"high",12);
                Assert.That(crowd.Far,Is.EqualTo(16));Assert.That(crowd.SubmittedTriangles,Is.EqualTo(16L*far.GetIndexCount(0)/3));
                camera.transform.rotation=Quaternion.Euler(0,180,0);crowd.Prepare(camera,"high",12);Assert.That(crowd.Visible,Is.Zero);
            }
            finally{Object.DestroyImmediate(source);Object.DestroyImmediate(observer);}
        }

        [TestCase("RibbonTape2m",-0.30f,1.05f,2.045f,.045f)]
        [TestCase("Billboard",0f,2.56f,3.40f,.64f)]
        [TestCase("FlagStela",0f,4.06f,1.55f,.65f)]
        public void OriginalRoadsideAssetsHaveBakedNativeBoundsAndOnlyAuditedPublicMaterials(string id,float minY,float maxY,float width,float depth)
        {
            var prefab=Resources.Load<GameObject>("Biathlon/Roadside/"+id);Assert.That(prefab,Is.Not.Null);
            var source=Object.Instantiate(prefab);
            try
            {
                Assert.That(source.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(source.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);
                Assert.That(source.GetComponentsInChildren<Light>(),Is.Empty);Assert.That(source.GetComponentsInChildren<Camera>(),Is.Empty);
                var permitted=new[]{Resources.Load<Material>("Biathlon/Stadium/G2Event/Steel"),Resources.Load<Material>("Biathlon/Stadium/G2Event/EventTeal"),Resources.Load<Material>("Biathlon/Stadium/white")};
                bool first=true;Bounds bounds=default;int parts=0;
                foreach(var filter in source.GetComponentsInChildren<MeshFilter>())
                {
                    Assert.That(filter.transform.localPosition,Is.EqualTo(Vector3.zero));Assert.That(filter.transform.localRotation,Is.EqualTo(Quaternion.identity));Assert.That(filter.transform.localScale,Is.EqualTo(Vector3.one));
                    Assert.That(filter.sharedMesh,Is.Not.Null);Assert.That(filter.sharedMesh.GetIndexCount(0),Is.GreaterThan(0));
                    CollectionAssert.Contains(permitted,filter.GetComponent<MeshRenderer>().sharedMaterial);
                    if(first){bounds=filter.sharedMesh.bounds;first=false;}else bounds.Encapsulate(filter.sharedMesh.bounds);parts++;
                }
                Assert.That(parts,Is.EqualTo(3));Assert.That(bounds.min.y,Is.EqualTo(minY).Within(.006f));Assert.That(bounds.max.y,Is.EqualTo(maxY).Within(.006f));
                Assert.That(bounds.size.x,Is.LessThanOrEqualTo(width+.006f));Assert.That(bounds.size.x,Is.GreaterThan(width-.05f));Assert.That(bounds.size.z,Is.EqualTo(depth).Within(.006f));
                if(id=="RibbonTape2m")
                {
                    // Closed fabric thickness supplies both viewing faces; the stake extension is intentional.
                    foreach(string meshId in new[]{"TapeTeal","TapeWhite"})
                    {
                        var mesh=Resources.Load<Mesh>("Biathlon/Roadside/"+meshId);bool front=false,rear=false;
                        foreach(var n in mesh.normals){if(n.z<-.99f)front=true;if(n.z>.99f)rear=true;}
                        Assert.That(front&&rear,Is.True);Assert.That(mesh.bounds.center.y,Is.EqualTo(.91f).Within(.00001f));
                    }
                }
            }
            finally{Object.DestroyImmediate(source);}
        }
    }
}

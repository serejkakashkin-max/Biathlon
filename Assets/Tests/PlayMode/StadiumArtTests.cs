using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class StadiumArtTests
    {
        [Test] public void LayoutRejectsInvalidDimensionsAndKeepsLaneClearance()
        {
            var d=StadiumArtDefinition.Load();Assert.That(d.LaneX(0),Is.EqualTo(-d.LaneX(d.lane_count-1)));
            for(int i=1;i<d.lane_count;i++)Assert.That(d.LaneX(i)-d.LaneX(i-1)-d.mat_width_m,Is.GreaterThanOrEqualTo(.3f));
            Assert.Throws<ArgumentOutOfRangeException>(()=>d.LaneX(d.lane_count));d.mat_width_m=d.lane_pitch_m;Assert.Throws<ArgumentException>(()=>StadiumArtDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [Test] public void BankKeepsFullAimingMarkInBothStancesWithoutChangingScoringGeometry()
        {
            var source=Resources.Load<GameObject>("Biathlon/Stadium/TargetBank");Assert.That(source,Is.Not.Null);var go=Object.Instantiate(source);
            try
            {
                var bank=go.GetComponent<TargetBankArt>();var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);string before=g.ToJson();
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing})
                {
                    bank.Apply(g,stance,0);for(int i=0;i<5;i++)
                    {
                        var face=bank.Aperture(i);Assert.That(face.transform.localPosition.x,Is.EqualTo(g.CenterX(i)).Within(.0000001));Assert.That(face.transform.localPosition.z,Is.Zero);
                        float radius=(float)g.Radius(ShootingStance.Standing);var mesh=face.GetComponent<MeshFilter>().sharedMesh;
                        foreach(var v in mesh.vertices)Assert.That(Vector3.Scale(v,face.transform.localScale).magnitude,Is.LessThanOrEqualTo(radius+.0000001));
                        Assert.That(mesh.bounds.size.x*face.transform.localScale.x,Is.EqualTo(radius*2).Within(.0000001));
                    }
                }
                Assert.That(g.ToJson(),Is.EqualTo(before));Assert.That(go.GetComponentsInChildren<Collider>(),Is.Empty);
                foreach(var r in go.GetComponentsInChildren<Renderer>())if(r.sharedMaterial.shader.name=="Universal Render Pipeline/Lit")Assert.That(r.sharedMaterial.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"),Is.True,"Target housing must not acquire long-lens cascade acne.");
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void ClosuresChangeOnlyObservedFaceMaterialAndDoNotAllocate()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Biathlon/Stadium/TargetBank"));
            try
            {
                var bank=go.GetComponent<TargetBankArt>();var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);
                bank.Apply(g,ShootingStance.Standing,0);var open=bank.Aperture(0).sharedMaterial;bank.Apply(g,ShootingStance.Standing,5);
                Assert.That(bank.Aperture(0).sharedMaterial,Is.Not.SameAs(open));Assert.That(bank.Aperture(1).sharedMaterial,Is.SameAs(open));Assert.That(bank.Aperture(2).sharedMaterial,Is.Not.SameAs(open));
                for(int i=0;i<20;i++)bank.Apply(g,ShootingStance.Prone,i%32);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)bank.Apply(g,ShootingStance.Prone,i%32);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.Throws<ArgumentException>(()=>bank.Apply(g,ShootingStance.Prone,32));
            }
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void SectorHasAuthoredBankDistanceAndNoAuthorityOrColliders()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
            try
            {
                var d=StadiumArtDefinition.Load();var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);var banks=go.GetComponentsInChildren<TargetBankArt>();
                Assert.That(banks.Length,Is.EqualTo(d.lane_count));for(int i=0;i<banks.Length;i++){Assert.That(banks[i].transform.localPosition.x,Is.EqualTo(d.LaneX(i)));Assert.That(banks[i].transform.localPosition.z,Is.EqualTo(g.DistanceMeters));Assert.That(banks[i].transform.localPosition.y,Is.EqualTo(d.target_height_m));}
                Assert.That(go.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(go.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);
                foreach(var renderer in go.GetComponentsInChildren<Renderer>())Assert.That(renderer.sharedMaterial,Is.Not.Null);
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>())Assert.That(filter.sharedMesh,Is.Not.Null);
            }
            finally{Object.DestroyImmediate(go);}
        }
    }
}

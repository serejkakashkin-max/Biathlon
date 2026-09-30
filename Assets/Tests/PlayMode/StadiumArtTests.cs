using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class StadiumArtTests
    {
        [Test] public void InnerProneBoundaryUsesScoringRadiusAndClearsOnClosureOrStanding()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Biathlon/Stadium/TargetBank"));
            try
            {
                var bank=go.GetComponent<TargetBankArt>();var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);
                var properties=new MaterialPropertyBlock();var geometry=new TargetGeometrySystem(g);string before=g.ToJson();
                foreach(var stance in new[]{ShootingStance.Prone,ShootingStance.Standing,ShootingStance.Prone})
                for(int mask=0;mask<32;mask++)
                {
                    bank.Apply(g,stance,mask);
                    for(int i=0;i<5;i++)
                    {
                        var face=bank.Aperture(i);bool inner=stance==ShootingStance.Prone&&(mask&(1<<i))==0;
                        Assert.That(face.sharedMaterial.shader.name=="Biathlon/ProneTargetFace",Is.EqualTo(inner));
                        face.GetPropertyBlock(properties);
                        Assert.That(properties.GetFloat("_ScoringRadius"),Is.EqualTo(inner?(float)(g.Radius(stance)/(2*g.Radius(ShootingStance.Standing))):0).Within(.000001));
                    }
                }
                // The large black annulus remains a miss in prone, a hit in standing.
                var ray=new ShotRay(0,0,0,0,.04,g.DistanceMeters);
                Assert.That(geometry.Intersect(ray,ShootingStance.Prone).Hit,Is.False);
                Assert.That(geometry.Intersect(ray,ShootingStance.Standing).Hit,Is.True);
                Assert.That(g.ToJson(),Is.EqualTo(before));
            }
            finally{Object.DestroyImmediate(go);}
        }
        [UnityTest] public IEnumerator InnerBoundaryIsReadableThroughAimCameraAndClosedFaceIsUniform()
        {
            var root=new GameObject("Prone target camera verification");
            try
            {
                var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);
                var view=root.AddComponent<RangePresentation>();view.enabled=false;
                view.Initialize(root.AddComponent<RacePresentationFeed>(),"target_review",g);
                var camera=view.AimCamera;camera.transform.parent.gameObject.SetActive(true);camera.aspect=16f/9;
                Directory.CreateDirectory("artifacts/g2-target-018");
                using(var target=new BenchmarkRenderTarget(1920,1080))
                foreach(var state in new[]{0,1,2})
                {
                    view.Bank.Apply(g,state==1?ShootingStance.Standing:ShootingStance.Prone,state==2?31:0);
                    for(int i=0;i<4;i++){target.Render(camera);yield return null;}
                    string path="artifacts/g2-target-018/"+(state==0?"prone":state==1?"standing":"closed")+"-aim.png";target.SavePng(path);
                    var texture=new Texture2D(2,2);
                    try
                    {
                        ImageConversion.LoadImage(texture,File.ReadAllBytes(path));
                        var mat=Resources.Load<Material>("Biathlon/Stadium/ProneTargetFace");
                        float edge=(float)g.Radius(ShootingStance.Prone)+mat.GetFloat("_RimWidth")*(float)g.StandingDiameterMeters*.5f;
                        float center=LuminanceAt(0),rim=LuminanceAt(edge);
                        if(state==0)Assert.That(rim-center,Is.GreaterThan(.10f),"Prone scoring boundary must be visible at the actual aim-camera scale.");
                        else Assert.That(Mathf.Abs(rim-center),Is.LessThan(.04f),"Standing and closed faces must not retain an inner ring.");
                        float LuminanceAt(float x)
                        {
                            var at=camera.WorldToViewportPoint(view.Bank.transform.TransformPoint(new Vector3(x,0,0)));
                            return texture.GetPixel(Mathf.RoundToInt(at.x*1919),Mathf.RoundToInt(at.y*1079)).grayscale;
                        }
                    }
                    finally{Object.Destroy(texture);}
                }
            }
            finally{Object.Destroy(root);}
            yield return null;
        }
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

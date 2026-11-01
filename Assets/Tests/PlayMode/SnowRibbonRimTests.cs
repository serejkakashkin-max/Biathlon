using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class SnowRibbonRimTests
    {
        [Test] public void LegacyPolicyRetainsItsExplicitBaselineAndRejectsNaturalDimensionsWithoutVersion()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var legacy=ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"snow_shoulders_before_m16_1"),plan);
            Assert.That(legacy.schema_version,Is.EqualTo(1));Assert.That(legacy.natural,Is.Null);
            Assert.That(legacy.mound_height_m,Is.EqualTo(.65f));Assert.That(legacy.mound_spacing_m,Is.EqualTo(19));
            var current=ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_fork_snow_shoulders"),plan);
            Assert.That(current.schema_version,Is.EqualTo(2));Assert.That(current.natural,Is.Not.Null);
            current.schema_version=1;Assert.Throws<ArgumentException>(()=>ProductionSnowShoulderDefinition.FromJson(JsonUtility.ToJson(current),plan));
        }
        [Test] public void DirectionDiscontinuityIsReducedWithoutMovingCentersOrChangingWidths()
        {
            double[] distances={0,8,9.9,10,10.1,12,20};var p=new Vector3[distances.Length*2];
            var incoming=Vector3.forward;var outgoing=Quaternion.Euler(0,12,0)*incoming;
            for(int row=0;row<distances.Length;row++)
            {
                float d=(float)distances[row];var direction=d<10?incoming:outgoing;
                var center=d<10?incoming*d:incoming*10+outgoing*(d-10);
                var across=new Vector3(direction.z,0,-direction.x)*4;
                p[row*2]=center-across;p[row*2+1]=center+across;
            }
            var source=new Mesh{vertices=p,uv=new Vector2[p.Length],colors=new Color[p.Length]};Mesh refined=null;
            try
            {
                refined=SnowRibbonRim.Refine(source,distances,false,2.5f,.75f);var result=refined.vertices;
                CollectionAssert.AreEqual(p,source.vertices);
                for(int row=0;row<distances.Length;row++)
                {
                    Assert.That(Vector3.Distance((result[row*2]+result[row*2+1])*.5f,(p[row*2]+p[row*2+1])*.5f),Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(result[row*2],result[row*2+1]),Is.EqualTo(8).Within(.00001));
                    Assert.That(Vector3.Distance(p[row*2],result[row*2]),Is.LessThanOrEqualTo(.75001f));
                }
                float before=Vector3.Angle(p[5]-p[4],p[7]-p[6]);float after=Vector3.Angle(result[5]-result[4],result[7]-result[6]);
                Assert.That(after,Is.LessThan(before*.1f),"Near-coincident rows must not retain the abrupt 12-degree jump.");
                Assert.That(result[0],Is.EqualTo(p[0]));Assert.That(result[result.Length-1],Is.EqualTo(p[p.Length-1]));
            }
            finally{Object.DestroyImmediate(source);if(refined!=null)Object.DestroyImmediate(refined);}
        }
        [Test]public void AFixedCutKeepsItsExactCrossRowAndFadesNearbyRotation()
        {
            double[] distances={0,8,9.9,10,10.1,12,20};var p=new Vector3[distances.Length*2];var outgoing=Quaternion.Euler(0,12,0)*Vector3.forward;
            for(int row=0;row<distances.Length;row++)
            {float d=(float)distances[row];var direction=d<10?Vector3.forward:outgoing;var center=d<10?Vector3.forward*d:Vector3.forward*10+outgoing*(d-10);var across=new Vector3(direction.z,0,-direction.x)*4;p[row*2]=center-across;p[row*2+1]=center+across;}
            var source=new Mesh{vertices=p,uv=new Vector2[p.Length],colors=new Color[p.Length]};Mesh free=null,pinned=null;
            try
            {
                free=SnowRibbonRim.Refine(source,distances,false,2.5f,.75f);
                pinned=SnowRibbonRim.Refine(source,distances,false,2.5f,.75f,new[]{10d});var q=pinned.vertices;
                Assert.That(q[6],Is.EqualTo(p[6]));Assert.That(q[7],Is.EqualTo(p[7]));
                Assert.That(Vector3.Distance(q[4],p[4]),Is.LessThan(Vector3.Distance(free.vertices[4],p[4])));
                CollectionAssert.AreEqual(p,source.vertices);
                for(int row=0;row<distances.Length;row++){Assert.That(Vector3.Distance((q[row*2]+q[row*2+1])*.5f,(p[row*2]+p[row*2+1])*.5f),Is.LessThan(1e-5));Assert.That(Vector3.Distance(q[row*2],q[row*2+1]),Is.EqualTo(8).Within(1e-5));}
                Assert.Throws<ArgumentException>(()=>SnowRibbonRim.Refine(source,distances,false,2.5f,.75f,new[]{double.NaN}));
            }
            finally{Object.DestroyImmediate(source);if(free!=null)Object.DestroyImmediate(free);if(pinned!=null)Object.DestroyImmediate(pinned);}
        }
    }
}

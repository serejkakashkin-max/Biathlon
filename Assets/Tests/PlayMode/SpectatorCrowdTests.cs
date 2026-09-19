using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class SpectatorCrowdTests
    {
        [Test] public void AuthoredSeatsPreserveAisleAndDefensivelyCopyAnchors()
        {
            var d=StadiumArtDefinition.Load();var anchors=d.SeatAnchors();var go=new GameObject("seats");
            try{var s=go.AddComponent<SpectatorSeating>();s.Configure(anchors);anchors[0]=Vector3.one*999;var copy=s.CopyAnchors();Assert.That(copy[0],Is.Not.EqualTo(anchors[0]));foreach(var p in copy)Assert.That(Mathf.Abs(p.x),Is.GreaterThanOrEqualTo(d.aisle_half_width_m));copy[0]=Vector3.zero;Assert.That(s.CopyAnchors()[0],Is.Not.EqualTo(Vector3.zero));Assert.Throws<ArgumentException>(()=>s.Configure(new[]{Vector3.zero,Vector3.zero}));Assert.Throws<ArgumentException>(()=>s.Configure(new[]{new Vector3(float.NaN,0,0)}));}
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void DensityRanksAreStableNestedAndFinite()
        {
            var d=SpectatorCrowdSettings.Load();int low=0,balanced=0;
            for(int i=0;i<512;i++){float rank=d.Rank(i);Assert.That(rank,Is.InRange(0f,.99999999f));Assert.That(d.Rank(i),Is.EqualTo(rank));if(rank<d.Density("low")){low++;Assert.That(rank,Is.LessThan(d.Density("balanced")));}if(rank<d.Density("balanced"))balanced++;}
            Assert.That(low,Is.GreaterThan(0).And.LessThan(balanced));Assert.That(balanced,Is.LessThan(512));Assert.Throws<ArgumentException>(()=>d.Density("invalid"));d.max_instances=513;Assert.Throws<ArgumentException>(()=>SpectatorCrowdSettings.FromJson(JsonUtility.ToJson(d)));
        }
        [Test] public void FrustumDistanceLodAndWarmedSubmissionAreBounded()
        {
            var go=new GameObject("crowd");var observer=new GameObject("observer");
            try
            {
                var seating=go.AddComponent<SpectatorSeating>();seating.Configure(StadiumArtDefinition.Load().SeatAnchors());var crowd=go.AddComponent<SpectatorCrowd>();crowd.Initialize(seating);var camera=observer.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,3,-18);camera.transform.LookAt(new Vector3(0,2,2));
                crowd.Prepare(camera,"high",12);Assert.That(crowd.Visible,Is.EqualTo(seating.Count));Assert.That(crowd.Near,Is.EqualTo(seating.Count));int high=crowd.Visible;crowd.Prepare(camera,"low",12);Assert.That(crowd.Visible,Is.LessThan(high));
                camera.transform.position=new Vector3(0,3,-50);camera.transform.LookAt(new Vector3(0,2,2));crowd.Prepare(camera,"high",12);Assert.That(crowd.Far,Is.EqualTo(high));
                camera.transform.rotation=Quaternion.Euler(0,180,0);crowd.Prepare(camera,"high",12);Assert.That(crowd.Visible,Is.Zero);
                camera.transform.position=new Vector3(0,3,-130);camera.transform.LookAt(Vector3.zero);crowd.Prepare(camera,"high",12);Assert.That(crowd.Visible,Is.Zero);
                camera.transform.position=new Vector3(0,3,-18);camera.transform.LookAt(new Vector3(0,2,2));for(int i=0;i<20;i++)crowd.Render(camera,"high",12);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)crowd.Render(camera,"high",12);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                Assert.Throws<ArgumentException>(()=>crowd.Prepare(camera,"high",float.NaN));go.transform.localScale=Vector3.one*2;Assert.Throws<ArgumentException>(()=>crowd.Prepare(camera,"high",12));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(observer);}
        }
        [Test] public void StadiumCrowdAnchorsHaveNoCollidersOrRaceAuthority()
        {
            var go=Object.Instantiate(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
            try{var s=go.GetComponentInChildren<SpectatorSeating>();Assert.That(s,Is.Not.Null);Assert.That(s.Count,Is.EqualTo(StadiumArtDefinition.Load().SeatAnchors().Length));Assert.That(go.GetComponentsInChildren<Collider>(),Is.Empty);Assert.That(go.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);var near=Resources.Load<Mesh>("Biathlon/Crowd/SpectatorLOD0");var far=Resources.Load<Mesh>("Biathlon/Crowd/SpectatorLOD1");Assert.That(far.GetIndexCount(0),Is.LessThan(near.GetIndexCount(0)));Assert.That(Resources.Load<Material>("Biathlon/Crowd/Spectator").enableInstancing,Is.True);}
            finally{Object.DestroyImmediate(go);}
        }
        [Test] public void VenueTextSharesDepthTestedAtlasAndReleasesItsLease()
        {
            int baseline=VenueSignText.ActiveFontCount;var go=Object.Instantiate(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
            try
            {
                var labels=go.GetComponentsInChildren<VenueSignText>();Assert.That(labels.Length,Is.GreaterThan(10));var shared=labels[0].GetComponent<Renderer>().sharedMaterial;
                Assert.That(shared.shader.name,Is.EqualTo("Biathlon/VenueSignText"));Assert.That(shared,Is.Not.SameAs(Resources.Load<Material>("Biathlon/Stadium/SignText")));
                foreach(var label in labels)Assert.That(label.GetComponent<Renderer>().sharedMaterial,Is.SameAs(shared));
                go.SetActive(false);Assert.That(VenueSignText.ActiveFontCount,Is.EqualTo(baseline));go.SetActive(true);Assert.That(VenueSignText.ActiveFontCount,Is.EqualTo(baseline+1));
            }
            finally{Object.DestroyImmediate(go);}
            Assert.That(VenueSignText.ActiveFontCount,Is.EqualTo(baseline));
        }
    }
}

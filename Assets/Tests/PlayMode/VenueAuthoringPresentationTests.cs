using System.Collections;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class VenueAuthoringPresentationTests
    {
        [UnityTest] public IEnumerator GeneratedVenueBindsStableZonesCamerasAmbienceAndAllFiringLanes()
        {
#if UNITY_EDITOR
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/Venues/venue_pipeline_fixture_v1/Authoring.prefab");Assert.That(source,Is.Not.Null);
            var go=Object.Instantiate(source);
            try
            {
                var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/venue_pipeline_fixture_v1").text),id=>Resources.Load<TextAsset>(id).text);
                var ids=new HashSet<string>();int zones=0,lanes=0,ambience=0;
                foreach(var marker in go.GetComponentsInChildren<VenueAuthoringMarker>())
                {
                    Assert.That(ids.Add(marker.StableId),Is.True);if(marker.Kind=="semantic_zone")zones++;
                    if(marker.Kind=="firing_lane"){lanes++;var at=plan.Visual.Get(marker.Reference).Sample(50);Assert.That(marker.transform.position,Is.EqualTo(new Vector3((float)at.X,(float)at.Y,(float)at.Z)));}
                    if(marker.Kind=="ambience"){ambience++;Assert.That(marker.Reference,Does.StartWith("ambience_"));Assert.That(marker.Extent,Is.GreaterThan(0));}
                }
                Assert.That(zones,Is.EqualTo(plan.ZoneCount));Assert.That(lanes,Is.EqualTo(30));Assert.That(ambience,Is.EqualTo(2));
                var cameras=go.GetComponentsInChildren<Camera>();Assert.That(cameras.Length,Is.EqualTo(2));foreach(var c in cameras)Assert.That(c.enabled,Is.False);
                Assert.That(go.GetComponentsInChildren<PlayableSprintHost>(),Is.Empty);Assert.That(go.GetComponentsInChildren<Collider>(),Is.Empty);
            }
            finally{Object.Destroy(go);}
#endif
            yield return null;
        }
    }
}

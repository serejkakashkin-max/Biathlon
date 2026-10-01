using System.Collections;
using Biathlon.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class VenueValidationPresentationTests
    {
        [UnityTest] public IEnumerator ProductionResourceValidationFindsLegacyGeometryButAllSelectedPrefabsResolve()
        {
            var manifest=VenueManifest.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/venue_pipeline_fixture_v1").text);
            var plan=new VenueAuthoringPlan(manifest,id=>Resources.Load<TextAsset>(id).text);
            var policy=VenueValidationPolicy.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/production_venue_validation_v1").text);
            var report=VenueValidator.Validate(plan,policy,id=>Resources.Load<GameObject>(id)!=null);
            bool gap=false,gate=false;
            Assert.That(report.Passed,Is.False);
            for(int i=0;i<report.Count;i++){string code=report.Get(i).Code;Assert.That(code,Is.Not.EqualTo("asset.missing"));gap|=code=="distance.world";gate|=code=="range.shared_entry_gate";}
            Assert.That(gap&&gate,Is.True);yield return null;
        }
    }
}

using System;
using System.Globalization;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class VenueAuthoringTests
    {
        public static string ManifestJson()=>PhysiologyContractTests.DefinitionJson("Venues/venue_pipeline_fixture_v1.json");
        public static string Read(string resource)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+resource+".json");
        [Test] public void ManifestRoundTripsAndZonesAreDerivedFromExactTrackSegments()
        {
            var a=new VenueAuthoringPlan(VenueManifest.FromJson(ManifestJson()),Read);var b=new VenueAuthoringPlan(VenueManifest.FromJson(a.Manifest.ToJson()),Read);
            Assert.That(a.Manifest.ToJson(),Is.EqualTo(b.Manifest.ToJson()));Assert.That(a.Track.ToJson(),Is.EqualTo(b.Track.ToJson()));
            int count=0;for(int i=0;i<a.Track.LoopCount;i++)count+=a.Track.GetLoop(i).SegmentCount;
            Assert.That(a.ZoneCount,Is.EqualTo(count));Assert.That(a.Lanes.Count,Is.EqualTo(30));
            for(int i=0;i<count;i++){var z=a.Zone(i);Assert.That(z.End,Is.GreaterThan(z.Start));Assert.That(z.Kind,Is.EqualTo(b.Zone(i).Kind));}
            Assert.That(a.At(a.Manifest.Camera(0).Anchor).Z,Is.EqualTo(50));
        }
        [TestCase("../outside")][TestCase("C:/data")][TestCase("Biathlon/a.json")][TestCase("Biathlon//a")][TestCase("Biathlon\\a")]
        public void UnsafeResourceNamesFailBeforeReaderIsCalled(string bad)
        {Assert.Catch(()=>VenueManifest.FromJson(ManifestJson().Replace("Biathlon/stadium_visual_v1",bad)));}
        [TestCase("../outside")][TestCase("nested/id")][TestCase("C:/outside")][TestCase("/absolute")][TestCase("range_entry")][TestCase("range_exit")][TestCase("firing_lane_1")]
        public void VenueIdCannotEscapeGeneratedOutputFolder(string bad)
        {Assert.Catch(()=>VenueManifest.FromJson(ManifestJson().Replace("venue_pipeline_fixture_v1",bad)));}
        [Test] public void WrongVersionMissingDependenciesDuplicatesAndUnknownAnchorsFail()
        {
            string json=ManifestJson();
            Assert.Catch(()=>VenueManifest.FromJson(PhysiologyContractTests.ReplaceNumber(json,"schema_version","2")));
            Assert.Catch(()=>new VenueAuthoringPlan(VenueManifest.FromJson(json),_=>throw new InvalidOperationException("missing")));
            foreach(string changed in new[]{json.Replace("forest_follow","range_overview"),json.Replace("forest_follow","range_entry"),json.Replace("forest_follow","firing_lane_1"),json.Replace("side_entry","frontal"),json.Replace("main_long","missing"),PhysiologyContractTests.ReplaceNumber(json,"fov_deg","0"),PhysiologyContractTests.ReplaceNumber(json,"lane_pitch_m","0")})
                Assert.Catch(()=>new VenueAuthoringPlan(VenueManifest.FromJson(changed),Read));
        }
        [Test] public void ManifestCanonicalOutputIsCultureIndependentAndHasNoUnityReference()
        {
            var old=CultureInfo.CurrentCulture;
            try{var manifest=VenueManifest.FromJson(ManifestJson());string expected=manifest.ToJson();CultureInfo.CurrentCulture=new CultureInfo("ru-RU");Assert.That(VenueManifest.FromJson(expected).ToJson(),Is.EqualTo(expected));}
            finally{CultureInfo.CurrentCulture=old;}
            foreach(var reference in typeof(VenueManifest).Assembly.GetReferencedAssemblies())Assert.That(reference.Name.StartsWith("Unity"),Is.False);
        }
    }
}

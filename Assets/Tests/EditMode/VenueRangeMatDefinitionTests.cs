using System;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class VenueRangeMatDefinitionTests
    {
        public const string MatJson="{\"schema_version\":1,\"id\":\"test_physical_mat_v1\",\"width_m\":1.5,\"length_m\":2,\"thickness_m\":0.015,\"athlete_stop_depth_m\":1}";
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson("{\"schema_version\":1,\"id\":\"test_body_v1\",\"body_length_m\":1.4,\"body_width_m\":0.6,\"personal_clearance_m\":0.1,\"edge_clearance_m\":0.1}");
        [Test] public void PhysicalMatRoundTripRetainsExplicitStopAndFitsUnchangedBody()
        {
            var mat=VenueRangeMatDefinition.FromJson(MatJson);var body=Body();mat.Validate(body);
            Assert.That(mat.ToJson(),Is.EqualTo(VenueRangeMatDefinition.FromJson(mat.ToJson()).ToJson()));
            Assert.That(mat.CentreForwardFromStop,Is.Zero);
            Assert.That(mat.ContainsBody(0,0,body),Is.True);
            Assert.That(mat.ContainsBody(.45,0,body),Is.True);
            Assert.That(mat.ContainsBody(.451,0,body),Is.False);
            Assert.That(mat.ContainsBody(0,.30,body),Is.True);
            Assert.That(mat.ContainsBody(0,.301,body),Is.False);
            Assert.That(mat.ContainsBody(0,-.301,body),Is.False);
            Assert.That(body.BodyLength,Is.EqualTo(1.4));Assert.That(body.BodyWidth,Is.EqualTo(.6));
        }
        [TestCase("schema_version","2")]
        [TestCase("width_m","0")]
        [TestCase("length_m","0")]
        [TestCase("thickness_m","0")]
        [TestCase("athlete_stop_depth_m","2.001")]
        public void InvalidGeometryRejectsAtParsing(string member,string value)
        {Assert.Catch(()=>VenueRangeMatDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(MatJson,member,value)));}
        [TestCase("width_m","0.5")]
        [TestCase("length_m","1")]
        [TestCase("athlete_stop_depth_m","0.6")]
        public void ADefinitionCannotReduceTheExistingAthleteBodyToFit(string member,string value)
        {
            var mat=VenueRangeMatDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(MatJson,member,value));
            Assert.Throws<ArgumentException>(()=>mat.Validate(Body()));
        }
        [Test] public void LegacyVenueCanonicalContextOmitsTheOptionalMatAndApron()
        {
            var legacy=VenueManifest.FromJson(VenueAuthoringTests.ManifestJson());
            Assert.That(legacy.Range.MatDefinition,Is.Null);Assert.That(legacy.SharedApronResource,Is.Null);
            Assert.That(legacy.ToJson(),Does.Not.Contain("mat_definition"));
            Assert.That(legacy.ToJson(),Does.Not.Contain("shared_apron_resource"));
            string changed=legacy.ToJson().Replace("\"range_layout\":{","\"shared_apron_resource\":\"test/apron\",\"range_layout\":{");
            Assert.That(changed,Does.Contain("shared_apron_resource"));
            Assert.Catch(()=>VenueManifest.FromJson(changed));
        }
    }
}

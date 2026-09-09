using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TrackContractTests
    {
        public static string Json() => PhysiologyContractTests.DefinitionJson("track_contract_fixture_v1.json");
        public static TrackDefinition Track() => TrackDefinition.FromJson(Json());
        [Test]
        public void ExactCoordinatesLengthsAndLoopScopedIdsArePreserved()
        {
            var track = Track();
            Assert.That(track.LoopCount, Is.EqualTo(3));
            Assert.That(track.GetLoop("main_short").Length.Millimeters, Is.EqualTo(1000000));
            Assert.That(track.GetLoop("main_long").Length.Meters, Is.EqualTo(1500));
            Assert.That(track.GetLoop(track.Penalty.LoopId).Length.Meters, Is.EqualTo(150));
            Assert.That(track.GetLoop("main_short").GetSegment(2).StartDistance.Millimeters, Is.EqualTo(600000));
            Assert.That(track.GetLoop("main_short").GetSegment(1).SemanticZone, Is.EqualTo(TrackSemanticZone.ClimbLong));
            Assert.That(track.GetLoop("main_short").GetSegment(0).Id, Is.EqualTo(track.GetLoop("main_long").GetSegment(0).Id));
            Assert.That(track.Range.FiringLine.Distance.Millimeters, Is.EqualTo(850000));
            Assert.That(track.GetCheckpoint(2).Anchor.Distance.Millimeters, Is.EqualTo(track.GetLoop("main_short").Length.Millimeters));
        }
        [Test]
        public void RoundTripRetainsStableIdentityAndAllSerializedFields()
        {
            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
                var original = Track();
                var copy = TrackDefinition.FromJson(original.ToJson());
                Assert.That(copy.ToJson(), Is.EqualTo(original.ToJson()));
                Assert.That(copy.Id, Is.EqualTo(original.Id));
                Assert.That(copy.GetLoop("main_short").GetSegment(3).DraftAllowed, Is.False);
                Assert.That(copy.GetLoop("main_short").GetSegment(0).DraftAllowed, Is.True);
            }
            finally { CultureInfo.CurrentCulture = culture; }
        }
        [TestCase("schema_version", "2")]
        [TestCase("length_m", "0")]
        [TestCase("length_m", "0.0005")]
        [TestCase("length_m", "1000000")]
        [TestCase("width_m", "1")]
        [TestCase("start_grade", "0.5")]
        [TestCase("end_grade", "-0.5")]
        [TestCase("curvature_per_m", "0.3")]
        [TestCase("wind_exposure", "1.1")]
        [TestCase("risk_level", "-0.1")]
        [TestCase("distance_m", "1001")]
        [TestCase("max_abs_grade", "\"NaN\"")]
        public void InvalidNumericDataIsRejected(string field, string value)
        {
            Assert.Catch(() => TrackDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Json(), field, value)));
        }
        [Test]
        public void BrokenTopologyDuplicatesUnknownTagsAndMissingFieldsAreRejected()
        {
            string json = Json();
            foreach (string bad in new[] { "null", "{}", json.Replace("main_long", "main_short"),
                json.Replace("climb_01", "flat_01"), json.Replace("split_500", "start"),
                json.Replace("\"loop_id\": \"main_short\"", "\"loop_id\": \"unknown\""),
                json.Replace("\"kind\": \"penalty\"", "\"kind\": \"race\""),
                json.Replace("climb_long", "unrecognized_zone"),
                json.Replace("\"distance_m\": 850", "\"distance_m\": 810"),
                json.Replace("\"distance_m\": 900", "\"distance_m\": 870"),
                json.Replace("\"distance_m\": 920", "\"distance_m\": 890"),
                Regex.Replace(json, "\\s*\"draft_allowed\"\\s*:\\s*(true|false),", "") })
                Assert.Catch(() => TrackDefinition.FromJson(bad));
            Assert.Catch(() => Track().GetLoop("missing"));
            Assert.Catch(() => TrackDefinition.FromJson(null));
        }
        [Test]
        public void MillimetreEncodingRejectsNonfiniteNegativeAndSubresolutionCoordinates()
        {
            Assert.That(TrackDistance.FromMeters(123.456).Millimeters, Is.EqualTo(123456));
            Assert.That(TrackDistance.FromMeters(1000000).Millimeters, Is.EqualTo(1000000000));
            Assert.That(new TrackDistance(1).Meters, Is.EqualTo(0.001));
            Assert.That(new TrackDistance(1).CompareTo(new TrackDistance(2)), Is.LessThan(0));
            Assert.Catch(() => TrackDistance.FromMeters(double.NaN));
            Assert.Catch(() => TrackDistance.FromMeters(double.PositiveInfinity));
            Assert.Catch(() => TrackDistance.FromMeters(-0.1));
            Assert.Catch(() => TrackDistance.FromMeters(0.0005));
            Assert.Catch(() => new TrackDistance(-1));
        }
    }
}

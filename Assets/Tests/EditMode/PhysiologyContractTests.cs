using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysiologyContractTests
    {
        public static string DefinitionJson(string filename = "physiology_provisional_v1.json")
            => ProjectFile(Path.Combine("Assets/Resources/Biathlon", filename));
        public static string ProjectFile(string relativePath)
        {
            // Works both from Unity project root and standalone bin directory; no Unity API.
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                string path = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(path)) return File.ReadAllText(path);
                directory = directory.Parent;
            }
            throw new FileNotFoundException("Authoritative physiology tuning file not found.");
        }
        public static PhysiologyDefinition Definition() => PhysiologyDefinition.FromJson(DefinitionJson());
        public static string ReplaceNumber(string json, string field, string value) =>
            Regex.Replace(json, "(\"" + field + "\"\\s*:\\s*)[-+0-9.eE]+", "${1}" + value);

        [Test]
        public void AllRequiredNumericFieldsRejectMissingNonfiniteAndOutsideEncodingDomain()
        {
            string json = DefinitionJson();
            foreach (var property in typeof(PhysiologyDefinition).GetProperties())
            {
                if (property.PropertyType != typeof(double)) continue;
                var member = property.GetCustomAttribute<DataMemberAttribute>();
                Assert.That(member.IsRequired, Is.True, property.Name);
                foreach (string invalid in new[] { "-1", "1000001", "\"NaN\"", "\"INF\"", "1e999" })
                    Assert.Catch(() => PhysiologyDefinition.FromJson(ReplaceNumber(json, member.Name, invalid)), member.Name + "=" + invalid);
                string missing = Regex.Replace(json, "\\s*\"" + member.Name + "\"\\s*:\\s*[-+0-9.eE]+,?", "");
                // Removing the last field otherwise leaves a trailing comma.
                missing = Regex.Replace(missing, ",\\s*}", "}");
                Assert.Catch(() => PhysiologyDefinition.FromJson(missing), member.Name + " missing");
            }
        }

        [TestCase("schema_version", "2")]
        [TestCase("drift_saturation_s", "0.0000000011")]
        [TestCase("hr_rise_tau_s", "0")]
        [TestCase("hr_recovery_tau_s", "0")]
        [TestCase("anaerobic_capacity_joules", "0")]
        [TestCase("resting_hr_bpm", "191")]
        [TestCase("hr_safety_ceiling_bpm", "180")]
        [TestCase("aerobic_threshold", "1.4")]
        [TestCase("minimum_power_efficiency", "1.01")]
        [TestCase("fatigue_recovery_penalty", "1.01")]
        public void InconsistentDefinitionsAreRejected(string field, string value)
        {
            Assert.Catch(() => PhysiologyDefinition.FromJson(ReplaceNumber(DefinitionJson(), field, value)));
        }

        [Test]
        public void JsonRoundTripIsCultureIndependentAndPreservesEveryField()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
                var original = Definition();
                var restored = PhysiologyDefinition.FromJson(original.ToJson());
                foreach (var property in typeof(PhysiologyDefinition).GetProperties())
                    Assert.That(property.GetValue(restored), Is.EqualTo(property.GetValue(original)), property.Name);
                Assert.That(restored.ToJson(), Is.EqualTo(original.ToJson()));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void InvalidIdsNullAndMalformedDocumentsAreRejected()
        {
            Assert.Catch(() => PhysiologyDefinition.FromJson(DefinitionJson().Replace("physiology_provisional_v1", " ")));
            Assert.Catch(() => PhysiologyDefinition.FromJson("{}"));
            Assert.Catch(() => PhysiologyDefinition.FromJson("null"));
            Assert.Catch(() => PhysiologyDefinition.FromJson("not json"));
            Assert.Throws<ArgumentNullException>(() => PhysiologyDefinition.FromJson(null));
        }

        [Test]
        public void StateValidatesRangesAndRejectsDefaultBeforeUse()
        {
            var definition = Definition();
            var state = PhysiologyState.Rested(definition);
            state.Validate(definition);
            Assert.That(state.HeartRateBpm, Is.EqualTo(definition.RestingHrBpm));
            Assert.That(state.AnaerobicReserve, Is.EqualTo(1));
            Assert.That(state.MuscularFatigue, Is.Zero);
            Assert.Catch(() => default(PhysiologyState).Validate(definition));
            Assert.Catch(() => new PhysiologyState(0, 1, 0, 0));
            Assert.Catch(() => new PhysiologyState(60, 1.01, 0, 0));
            Assert.Catch(() => new PhysiologyState(60, 1, -0.01, 0));
            Assert.Catch(() => new PhysiologyState(60, 1, 0, double.NaN));
            Assert.Catch(() => new PhysiologyState(200, 1, 0, 0).Validate(definition));
            Assert.Catch(() => new PhysiologyState(60, 1, 0, definition.DriftSaturationSeconds + 1).Validate(definition));
        }

        [Test]
        public void NumericEncodingRoundsToNineDecimalsAndSaturatesOnlyExplicitly()
        {
            Assert.That(PhysiologyNumbers.Quantize(0.1234567896), Is.EqualTo(0.123456790));
            Assert.That(PhysiologyNumbers.Clamp(1.1, 0, 1), Is.EqualTo(1));
            Assert.That(PhysiologyNumbers.Clamp(-0.1, 0, 1), Is.Zero);
            Assert.Catch(() => PhysiologyNumbers.Quantize(double.PositiveInfinity));
            Assert.Catch(() => PhysiologyNumbers.Clamp(double.NaN, 0, 1));
            Assert.Catch(() => PhysiologyNumbers.Clamp(0.5, 1, 0));
        }
    }
}

using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public sealed class SkiRuntimeProfileTests
    {
        [UnityTest] public IEnumerator ControlledMovingSkiProfile()
        {
            const string folder="artifacts/m16-6/performance/native-current";
            yield return RuntimeSkiProfileReview.Collect(folder,180);
            var report=JsonUtility.FromJson<RuntimeSkiProfileReview.Report>(File.ReadAllText(Path.Combine(folder,"profile.json")));
            Assert.That(report.final_tick,Is.EqualTo(270));Assert.That(report.samples.Length,Is.EqualTo(180));
            foreach(var sample in report.samples)
            {
                Assert.That(sample.tick_calls,Is.EqualTo(1));Assert.That(sample.snapshot_calls,Is.EqualTo(1));
                Assert.That(sample.athlete_calls,Is.EqualTo(1));Assert.That(sample.environment_calls,Is.EqualTo(1));
                Assert.That(double.IsFinite(sample.step_ms)&&sample.step_ms>0,Is.True);
            }
            Assert.That(report.samples[0].player_speed,Is.GreaterThan(0));
            Assert.That(report.samples[0].player_z,Is.Not.EqualTo(report.samples[179].player_z));
        }
    }
}

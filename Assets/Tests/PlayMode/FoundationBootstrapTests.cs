using System.Collections;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class FoundationBootstrapTests
    {
        private GameObject instance;
        private float originalTimeScale;
        [SetUp] public void SetUp() { originalTimeScale = Time.timeScale; Time.timeScale = 1; }
        [TearDown] public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (instance != null) Object.DestroyImmediate(instance);
        }
        [UnityTest]
        public IEnumerator BootstrapLoadsDataAndAdvancesCoreTicks()
        {
            instance = new GameObject("Test adapter");
            var adapter = instance.AddComponent<FoundationBootstrap>();
            Assert.That(adapter.World, Is.Not.Null);
            Assert.That(adapter.World.Config.Id, Is.EqualTo("simulation_foundation"));
            double deadline = Time.realtimeSinceStartupAsDouble + 5;
            while (adapter.World.Clock.Tick.Value < 3 && Time.realtimeSinceStartupAsDouble < deadline)
                yield return null;
            Assert.That(adapter.World.Clock.Tick.Value, Is.GreaterThanOrEqualTo(3));
            Assert.That(adapter.World.Clock.ElapsedSeconds, Is.EqualTo(adapter.World.Clock.Tick.Value / (double)adapter.World.Clock.Frequency));
            Assert.That(adapter.World.GetAthlete(0).RequestedEffort, Is.Zero);
            Assert.That(adapter.Events.Count, Is.Zero);
        }
        [UnityTest]
        public IEnumerator PausedGameTimeDoesNotAdvanceSimulation()
        {
            instance = new GameObject("Test pause adapter");
            var adapter = instance.AddComponent<FoundationBootstrap>();
            yield return null;
            Time.timeScale = 0;
            yield return null;
            long tick = adapter.World.Clock.Tick.Value;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(adapter.World.Clock.Tick.Value, Is.EqualTo(tick));
        }
    }
}

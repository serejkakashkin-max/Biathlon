using System;
using System.Linq;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class FoundationTests
    {
        private static readonly AthleteId A = new AthleteId("athlete_a");
        private static readonly AthleteId B = new AthleteId("athlete_b");
        private static SimulationConfig Config(ulong seed = 12345, int hz = 20) =>
            new SimulationConfig(1, "foundation_test", hz, seed);
        private static SimulationWorld World(RaceEventLog log, ulong seed = 12345, int hz = 20) =>
            new SimulationWorld(Config(seed, hz), new[] { new AthleteState(A), new AthleteState(B) }, log);

        [TestCase(20, 10000)]
        [TestCase(30, 10000)]
        [TestCase(7, 31)]
        public void FixedClockUsesExactTickAndFrequency(int frequency, int ticks)
        {
            var world = World(new RaceEventLog(), hz: frequency);
            for (int i = 0; i < ticks; i++) world.Step();
            Assert.That(world.Clock.Tick.Value, Is.EqualTo(ticks));
            Assert.That(world.Clock.Frequency, Is.EqualTo(frequency));
            Assert.That(world.Clock.ElapsedSeconds, Is.EqualTo(ticks / (double)frequency));
        }
        public static string RunDeterministicScenario(ulong seed, bool includeRandom = true)
        {
            var log = new RaceEventLog(20000);
            var world = World(log, seed);
            var batch = new SimulationInput[2];
            for (int tick = 1; tick <= 10000; tick++)
            {
                batch[0] = new SimulationInput(new SimulationTick(tick), A, tick % 1001);
                batch[1] = new SimulationInput(new SimulationTick(tick), B, (tick * 7) % 1001);
                world.Step(batch);
            }
            Assert.That(world.Clock.Tick.Value, Is.EqualTo(10000));
            Assert.That(world.GetAthlete(0).RequestedEffort, Is.EqualTo(991));
            Assert.That(world.GetAthlete(1).RequestedEffort, Is.EqualTo(931));
            Assert.That(log.Count, Is.EqualTo(20000));
            for (int i = 0; i < log.Count; i++)
            {
                Assert.That(log[i].Tick.Value, Is.EqualTo(i / 2 + 1));
                Assert.That(log[i].AthleteId, Is.EqualTo(i % 2 == 0 ? A : B));
            }
            return SimulationFingerprint.Compute(world, log, includeRandom);
        }
        [Test]
        public void SameSeed10000TicksHasIdenticalStateAndOrderedEventHash()
        {
            string first = RunDeterministicScenario(12345);
            Assert.That(RunDeterministicScenario(12345), Is.EqualTo(first));
            Assert.That(first, Is.EqualTo("aab9e912c3ed27c93706397a6667ae7a71dbd6f97685a2a5519feb7380a3360e"),
                "Foundation v1 regression changed; review the contract before updating this baseline.");
            TestContext.WriteLine("deterministic_sha256=" + first);
        }
        [Test]
        public void DifferentSeedDoesNotAffectSystemsWhichDoNotConsumeRandom()
        {
            Assert.That(RunDeterministicScenario(12345, false), Is.EqualTo(RunDeterministicScenario(98765, false)));
            Assert.That(new SeededRandomSource(12345).NextUInt64(), Is.Not.EqualTo(new SeededRandomSource(98765).NextUInt64()));
            var world = World(new RaceEventLog());
            for (int i = 0; i < 10000; i++) world.Step();
            Assert.That(world.Random.State, Is.EqualTo(12345UL));
        }
        [Test]
        public void RandomSequenceMatchesKnownVectorAndRestoresState()
        {
            var random = new SeededRandomSource(0);
            Assert.That(random.NextUInt64(), Is.EqualTo(0xe220a8397b1dcdafUL));
            Assert.That(random.NextUInt64(), Is.EqualTo(0x6e789e6aa1b965f4UL));
            var restored = new SeededRandomSource(random.State);
            for (int i = 0; i < 10000; i++) Assert.That(restored.NextUInt64(), Is.EqualTo(random.NextUInt64()));
        }
        [Test]
        public void WorldsOwnIndependentStateAndRandomStreams()
        {
            var initial = new[] { new AthleteState(A) };
            var left = new SimulationWorld(Config(), initial, new RaceEventLog());
            var right = new SimulationWorld(Config(), initial, new RaceEventLog());
            initial[0] = new AthleteState(A, 900);
            left.Step(new[] { new SimulationInput(new SimulationTick(1), A, 700) });
            left.Random.NextUInt64();
            Assert.That(left.GetAthlete(0).RequestedEffort, Is.EqualTo(700));
            Assert.That(right.GetAthlete(0).RequestedEffort, Is.Zero);
            Assert.That(right.Clock.Tick.Value, Is.Zero);
            Assert.That(right.Random.State, Is.EqualTo(12345UL));
        }
        [Test]
        public void InvalidInputBatchIsRejectedBeforeAnyMutation()
        {
            var log = new RaceEventLog();
            var world = World(log);
            var before = SimulationFingerprint.Compute(world, log);
            Assert.Throws<ArgumentException>(() => world.Step(new[] {
                new SimulationInput(new SimulationTick(1), A, 700),
                new SimulationInput(new SimulationTick(2), B, 800) }));
            Assert.That(SimulationFingerprint.Compute(world, log), Is.EqualTo(before));
            Assert.Throws<ArgumentException>(() => world.Step(new[] {
                new SimulationInput(new SimulationTick(1), new AthleteId("unknown"), 500) }));
            Assert.Throws<ArgumentException>(() => world.Step(new[] { default(SimulationInput) }));
            Assert.Throws<ArgumentNullException>(() => world.Step(null));
            Assert.That(SimulationFingerprint.Compute(world, log), Is.EqualTo(before));
        }
        [Test]
        public void RepeatedAthleteInputsPreserveOrderAndSuppressNoOpEvents()
        {
            var log = new RaceEventLog();
            var world = World(log);
            world.Step(new[] {
                new SimulationInput(new SimulationTick(1), B, 600),
                new SimulationInput(new SimulationTick(1), A, 500),
                new SimulationInput(new SimulationTick(1), A, 500),
                new SimulationInput(new SimulationTick(1), A, 700) });
            Assert.That(log.Count, Is.EqualTo(3));
            Assert.That(log[0].AthleteId, Is.EqualTo(B));
            Assert.That(log[1].Payload, Is.EqualTo(500));
            Assert.That(log[2].Payload, Is.EqualTo(700));
            Assert.That(log[2].EventType, Is.EqualTo(RaceEventType.EffortChanged));
            Assert.That(log[2].SimulationTimeSeconds, Is.EqualTo(0.05));
            Assert.That(world.GetAthlete(0).RequestedEffort, Is.EqualTo(700));
        }
        [Test]
        public void RecordedInputsReplayIdenticalEvents()
        {
            var log = new RaceEventLog();
            var original = World(log);
            var recorded = new SimulationInput[100];
            for (int i = 0; i < recorded.Length; i++)
            {
                recorded[i] = new SimulationInput(new SimulationTick(i + 1), A, i * 3);
                original.Step(new[] { recorded[i] });
            }
            var replayLog = new RaceEventLog();
            var replay = World(replayLog);
            foreach (var input in recorded) replay.Step(new[] { input });
            Assert.That(SimulationFingerprint.Compute(replay, replayLog), Is.EqualTo(SimulationFingerprint.Compute(original, log)));
        }
        [Test]
        public void FingerprintDetectsEventOrderAndPayloadChanges()
        {
            var log = new RaceEventLog();
            var world = World(log);
            world.Step(new[] {
                new SimulationInput(new SimulationTick(1), A, 500),
                new SimulationInput(new SimulationTick(1), B, 600) });
            var reversed = new RaceEventLog();
            reversed.Record(log[1]); reversed.Record(log[0]);
            Assert.That(SimulationFingerprint.Compute(world, reversed), Is.Not.EqualTo(SimulationFingerprint.Compute(world, log)));
            var altered = new RaceEventLog();
            altered.Record(new RaceEvent(new SimulationTick(1), 20, A, RaceEventType.EffortChanged, 501));
            altered.Record(log[1]);
            Assert.That(SimulationFingerprint.Compute(world, altered), Is.Not.EqualTo(SimulationFingerprint.Compute(world, log)));
        }
        [Test]
        public void CoreAssemblyReferencesNeverIncludeUnity()
        {
            var assemblies = new[] { typeof(AthleteId).Assembly, typeof(SimulationWorld).Assembly,
                typeof(RaceEventLog).Assembly, typeof(Biathlon.AI.AssemblyMarker).Assembly, typeof(Biathlon.Career.AssemblyMarker).Assembly };
            foreach (var assembly in assemblies)
                Assert.That(assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal)), Is.False, assembly.FullName);
        }
        [Test]
        public void StepDoesNotAllocateManagedMemoryAfterWarmup()
        {
            var world = World(new RaceEventLog(10001));
            var batch = new SimulationInput[1];
            batch[0] = new SimulationInput(new SimulationTick(1), A, 1);
            world.Step(batch);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10001; i++)
            {
                batch[0] = new SimulationInput(new SimulationTick(i), A, i % 1001);
                world.Step(batch);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
        }
        [Test]
        public void InvalidDefinitionsAndIdentifiersAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Config(hz: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationConfig(2, "unsupported", 20, 1));
            Assert.Throws<ArgumentException>(() => new SimulationConfig(1, "", 20, 1));
            Assert.Throws<ArgumentException>(() => new AthleteId(" "));
            Assert.Throws<ArgumentException>(() => new AthleteState(default));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AthleteState(A, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationInput(new SimulationTick(1), A, 1001));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationTick(-1));
            Assert.Throws<OverflowException>(() => new SimulationTick(long.MaxValue).Next());
            Assert.Throws<ArgumentException>(() => new SimulationWorld(Config(), new[] { new AthleteState(A), new AthleteState(A) }, new RaceEventLog()));
            Assert.Throws<ArgumentException>(() => new SimulationWorld(Config(), new[] { default(AthleteState) }, new RaceEventLog()));
        }
    }
}

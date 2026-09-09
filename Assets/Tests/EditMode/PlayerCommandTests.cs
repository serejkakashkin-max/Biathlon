using System;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PlayerCommandTests
    {
        private static PlayerCommand Command() => new PlayerCommand(new SimulationTick(9007199254740993), new AthleteId("player_one"), 723, -431,
            EffortPreset.Attack, true, true, false, "skate_climb");
        [Test]
        public void VersionedRoundTripRetainsExactLargeTickAndEveryIntent()
        {
            var command = Command(); var restored = PlayerCommand.FromJson(command.ToJson());
            Assert.That(restored, Is.EqualTo(command)); Assert.That(restored.Tick.Value, Is.EqualTo(9007199254740993));
            Assert.That(restored.ToJson(), Is.EqualTo(command.ToJson()));
            var automatic = new PlayerCommand(new SimulationTick(1), new AthleteId("a"), 0, 1000);
            Assert.That(PlayerCommand.FromJson(automatic.ToJson()).TechniqueId, Is.Null);
        }
        [Test]
        public void RequiredFieldsSchemaAndEncodingsRejectAmbiguousCommands()
        {
            string json = Command().ToJson();
            foreach (string field in new[] { "schema_version", "id", "target_tick", "athlete_id", "analog_effort", "line_intent", "effort_preset", "tuck_held", "overtake_requested", "cancel_overtake", "technique_id" })
                Assert.Catch(() => PlayerCommand.FromJson(json.Replace("\"" + field + "\"", "\"missing_" + field + "\"")), field);
            foreach (var pair in new[] { ("schema_version", "2"), ("target_tick", "0"), ("analog_effort", "1001"), ("line_intent", "-1001"), ("effort_preset", "5") })
                Assert.Catch(() => PlayerCommand.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair.Item1, pair.Item2)));
            Assert.Catch(() => PlayerCommand.FromJson(json.Replace(PlayerCommand.SchemaId, "unknown_schema")));
            Assert.Catch(() => new PlayerCommand(new SimulationTick(1), new AthleteId("a"), 1, 0, overtakeRequested: true, cancelOvertake: true));
            Assert.Catch(() => new PlayerCommand(new SimulationTick(1), new AthleteId("a"), 1, 0, techniqueId: " "));
            Assert.That(default(PlayerCommand).IsValid, Is.False); Assert.Catch(() => default(PlayerCommand).ToJson());
        }
        [Test]
        public void TickCommandsConstructWithoutAllocationsOrFrameTime()
        {
            var id = new AthleteId("a"); long sum = 0;
            var warmup = new PlayerCommand(new SimulationTick(1), id, 500, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 1; i <= 10000; i++) sum += new PlayerCommand(new SimulationTick(i), id, 500, i % 2001 - 1000).Tick.Value;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(sum, Is.EqualTo(50005000));
        }
    }
}

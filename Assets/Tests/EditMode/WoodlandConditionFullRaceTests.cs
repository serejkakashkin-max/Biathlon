#if !UNITY_EDITOR
using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Long headless condition-flow witness. Unity PlayMode separately
    /// validates the imported visual environment for all five weather rows.</summary>
    public sealed class WoodlandConditionFullRaceTests
    {
        private const string Root="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static RaceSnowConditions Conditions(string profile)
        {
            var json=Read("Biathlon/race_snow_neutral_v1")
                .Replace("race_snow_neutral_v1","woodland_wp175_"+profile+"_v1")
                .Replace("packed_neutral",profile);
            return RaceSnowConditions.FromJson(json);
        }
        private static PackAiRaceRunner Create(string profile,IRaceEventSink events)
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
            return ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(Root+"sprint_profile")),
                RaceRoutePlan.FromJson(Read(Root+"sprint_route")),
                Conditions(profile),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),count:1);
        }

        [TestCase("packed_neutral")]
        [TestCase("fresh_soft")]
        [TestCase("icy_fast")]
        [TestCase("wet_slow")]
        [Timeout(600000)]
        public void ActualWoodlandSprintCompletesAndPersistedCommandsReplayForCanonicalSnow(string profile)
        {
            var source=Create(profile,new RaceEventLog(100000));
            var recordingField=Create(profile,new RaceEventLog(100000)).Field;
            var tape=new PackSprintRaceCommandTape("woodland_wp175_"+profile+"_commands_v1",
                recordingField,50000);
            long steps=0;
            while(!source.Field.IsComplete&&steps<50000)
            {
                source.Step();
                tape.StepAndRecord(recordingField,
                    new[]{source.GetLastInput(0)},new[]{source.GetLastLateral(0)});
                steps++;
            }
            Assert.That(source.Field.IsComplete,Is.True,"The actual condition must finish a full Sprint.");
            Assert.That(recordingField.IsComplete,Is.True);
            string result=source.Field.Result.ToJson();
            Assert.That(recordingField.Result.ToJson(),Is.EqualTo(result));
            var restored=PackSprintRaceCommandTape.FromJson(tape.ToJson());
            var replay=Create(profile,new RaceEventLog(100000)).Field;
            var cursor=restored.CreateReplay(replay);
            while(cursor.HasNext)cursor.Step();
            Assert.That(replay.IsComplete,Is.True);
            Assert.That(replay.Result.ToJson(),Is.EqualTo(result));
            Assert.That(replay.GetAthlete(0).LastFrame,Is.EqualTo(source.Field.GetAthlete(0).LastFrame));
            string otherProfile=profile=="packed_neutral"?"icy_fast":"packed_neutral";
            var wrongCondition=Create(otherProfile,new RaceEventLog(100000)).Field;
            Assert.Catch(()=>restored.CreateReplay(wrongCondition),
                "A persisted race must reject a different authoritative snow profile before stepping.");
            Assert.That(wrongCondition.Tick.Value,Is.Zero);
            TestContext.WriteLine("wp175_sprint_profile="+profile+" ticks="+steps+
                " result_chars="+result.Length);
        }
    }
}
#endif

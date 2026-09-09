using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biathlon.PlayModeTests
{
    public class SprintReplayPlayModeTests
    {
        [UnityTest] public IEnumerator ResourcesRunCompleteSprintAndPersistedReplay()
        {
            string Read(string name) => Resources.Load<TextAsset>("Biathlon/" + Path.GetFileNameWithoutExtension(name)).text;
            var suite = SprintRegressionSuite.FromJson(Read("sprint_regression_v1.json"));
            var track = TrackDefinition.FromJson(File.ReadAllText(Path.Combine(Application.dataPath, "../tests/fixtures/tracks/synthetic_flat_v1.json")));
            var events = new RaceEventLog(4000); var runner = new SprintRegressionRunner(suite, suite.Get(1), Read, track, events);
            var tape = new SprintRaceCommandTape("sprint_playmode", runner.Field, suite.MaximumCommandRuns); var inputs = new SprintAthleteInput[1];
            while (!runner.Field.IsComplete)
            {
                inputs[0] = runner.NextInput(); tape.StepAndRecord(runner.Field, inputs);
                if (runner.Field.Tick.Value % 500 == 0) yield return null;
            }
            var replayEvents = new RaceEventLog(4000); var replay = new SprintRegressionRunner(suite, suite.Get(1), Read, track, replayEvents);
            var restored = SprintRaceCommandTape.FromJson(tape.ToJson()); var cursor = restored.CreateReplay(replay.Field);
            while (cursor.HasNext) cursor.Step();
            Assert.That(replay.Field.Result.ToJson(), Is.EqualTo(runner.Field.Result.ToJson()));
            Assert.That(runner.Field.GetAthlete(0).TotalPenaltyLoops, Is.EqualTo(1));
            Assert.That(events.Count, Is.EqualTo(replayEvents.Count)); for (int i = 0; i < events.Count; i++) Assert.That(replayEvents[i], Is.EqualTo(events[i]));
        }
    }
}

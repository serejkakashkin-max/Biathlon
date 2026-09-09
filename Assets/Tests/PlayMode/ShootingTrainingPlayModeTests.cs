using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biathlon.PlayModeTests
{
    public class ShootingTrainingPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResourcesBuildCompleteReplayableShootingBout()
        {
            string Read(string name) => Resources.Load<TextAsset>("Biathlon/" + System.IO.Path.GetFileNameWithoutExtension(name)).text;
            var model = new ShootingModelDefinitions(Read);
            var athlete = ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
            var scenario = ShootingTrainingScenario.FromJson(Read("shooting_training_v1.json"));
            var events = new RaceEventLog(1000); var runner = new ShootingScenarioRunner(scenario, model, athlete, events);
            var tape = new ShootingCommandTape("playmode_shooting", runner.Session.ContextJson, runner.Session.AthleteId, scenario.MaximumTicks);
            while (!runner.IsComplete)
            {
                tape.StepAndRecord(runner.Session, runner.NextInput());
                if (tape.Count % 50 == 0) yield return null;
            }
            var replayEvents = new RaceEventLog(1000); var replay = new ShootingScenarioRunner(scenario, model, athlete, replayEvents);
            var restored = ShootingCommandTape.FromJson(tape.ToJson());
            for (int i = 0; i < restored.Count; i++) restored.ReplayNext(replay.Session);
            Assert.That(replay.Session.LastFrame, Is.EqualTo(runner.Session.LastFrame));
            Assert.That(runner.Session.ShotsFired, Is.EqualTo(5)); Assert.That(runner.Session.Tick.Value, Is.EqualTo(214));
            Assert.That(replayEvents.Count, Is.EqualTo(events.Count));
            for (int i = 0; i < events.Count; i++) Assert.That(replayEvents[i], Is.EqualTo(events[i]));
        }
    }
}

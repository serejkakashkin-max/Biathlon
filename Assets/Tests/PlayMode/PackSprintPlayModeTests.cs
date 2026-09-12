using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Biathlon.PlayModeTests
{
    public class PackSprintPlayModeTests
    {
        [UnityTest] public IEnumerator ResourcesDriveSharedBodiesThroughCompleteSprint()
        {
            string Read(string name) => Resources.Load<TextAsset>("Biathlon/" + Path.GetFileNameWithoutExtension(name)).text;
            string Fixture(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "../tests/fixtures/tracks/" + name));
            var profile = CompetitionProfile.FromJson(Read("sprint_test_v1.json").Replace("\"start_interval_s\":30", "\"start_interval_s\":2"));
            var track = TrackDefinition.FromJson(Fixture("synthetic_flat_v1.json"));
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("pack_playmode_route", track.Id, "main_long", profile.ToRouteRequirements()), profile.ToRouteRequirements());
            var ski = TimeTrialAthlete.FromJson(Read("time_trial_athlete_v1.json"));
            var shot = ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
            var entrants = new SprintEntrant[3];
            for (int i = 0; i < entrants.Length; i++)
            {
                string id = "athlete_" + i;
                entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id, id)),
                    ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id, id)), "ski_neutral");
            }
            var junction = PhysicalJunctionDefinition.FromJson(Fixture("synthetic_flat_junctions_v1.json"), track);
            var connection = PhysicalConnectionDefinition.FromJson(Fixture("synthetic_flat_connections_v1.json"), junction);
            var log = new RaceEventLog(20000);
            var body = OccupancyDefinition.FromJson(Read("occupancy_provisional_v1.json"));
            var field = new PackSprintFieldSession(new SimulationConfig(1, "pack_playmode_v1", 20, 123), profile,
                new SkiModelDefinitions(Read), new ShootingModelDefinitions(Read), track, route,
                RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")), ResultTimingDefinition.FromJson(Read("result_timing_v1.json")),
                entrants, new SimulationTick(1), "pack_playmode_result", body, DraftDefinition.FromJson(Read("draft_provisional_v1.json")),
                OvertakeDefinition.FromJson(Read("overtake_provisional_v1.json")), junction, connection, log);
            var presentationObject=new GameObject("presentation_feed_test");
            var feed=presentationObject.AddComponent<Biathlon.Unity.Presentation.RacePresentationFeed>();
            var initial=RacePresentationSnapshot.Capture(field);feed.Publish(initial);
            Assert.Catch(()=>feed.Publish(initial));
            var commands = new SprintAthleteInput[3]; var lines = new double[3];
            while (!field.IsComplete && field.Tick.Value < 100000)
            {
                var tick = field.Tick.Next();
                for (int i = 0; i < field.Count; i++)
                {
                    var id = field.GetAthlete(i).AthleteId;
                    commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, id, 700 + i * 100), 1, 1, 0), new WeaponAimCommand(tick, id, 0, 0), true);
                }
                field.Step(commands, lines);
                for (int i = 0; i < field.Count; i++) if (field.IsOnCourse(i))
                    for (int j = 0; j < i; j++) if (field.IsOnCourse(j))
                    {
                        var a = field.GetPhysicalPoint(i); var b = field.GetPhysicalPoint(j);
                        if (a.LoopId != b.LoopId) continue;
                        long gap = System.Math.Abs(a.LocalNanometers - b.LocalNanometers);
                        if (a.Closed) gap = System.Math.Min(gap, a.LoopLengthNanometers - gap);
                        Assert.That(gap / 1e9 >= body.LongitudinalSeparation || System.Math.Abs(field.GetLateral(i) - field.GetLateral(j)) >= body.LateralSeparation, Is.True);
                    }
                if (field.Tick.Value % 500 == 0) {feed.Publish(RacePresentationSnapshot.Capture(field));Assert.That(feed.Current.Tick,Is.EqualTo(field.Tick.Value));Assert.That(feed.Position(0,0.5).Interpolated,Is.False);yield return null;}
            }
            Assert.That(initial.Tick,Is.Zero);feed.ResetRace();Assert.That(feed.Current,Is.Null);Object.Destroy(presentationObject);
            Assert.That(field.IsComplete, Is.True); Assert.That(field.Result.Count, Is.EqualTo(3));
            int seams = 0;
            for (int i = 0; i < log.Count; i++) if (log[i].EventType == RaceEventType.PhysicalSeamCrossed) seams++;
            Assert.That(seams, Is.EqualTo(12));
            for (int i = 0; i < field.Count; i++) Assert.That(field.GetAthlete(i).Lifecycle.CompletedBouts, Is.EqualTo(2));
        }
    }
}

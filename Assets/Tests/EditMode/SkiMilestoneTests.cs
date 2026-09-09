using System;
using System.Diagnostics;
using System.Text;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SkiMilestoneTests
    {
        private readonly struct Result
        {
            public readonly string Id;
            public readonly double Seconds, Reserve, Fatigue, HeartRate;
            public Result(string id, TimeTrialSession session)
            { Id = id; Seconds = session.FinishTime.Seconds; Reserve = session.PhysiologyState.AnaerobicReserve;
                Fatigue = session.PhysiologyState.MuscularFatigue; HeartRate = session.PhysiologyState.HeartRateBpm; }
        }
        private static Result Run(string id, string track = "synthetic_flat_v1", int effort = 800,
            bool sprint = false, bool tuck = false, string snow = "packed_neutral", string ski = "ski_neutral",
            string stat = null, int value = 70, bool intervals = false)
        {
            var athlete = TimeTrialTests.Athlete();
            if (stat != null) athlete = TimeTrialAthlete.FromJson(PhysiologyContractTests.ReplaceNumber(athlete.ToJson(), stat, value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var session = TimeTrialTests.Create(new RaceEventLog(1000), track, snowProfile: snow, skiId: ski, athlete: athlete);
            while (!session.HasFinished && session.Clock.Tick.Value < 100000)
                session.Step(TimeTrialTests.Input(session, intervals ? (session.Clock.Tick.Value % 400 < 200 ? 1000 : 600) : effort, tuck, sprint));
            Assert.That(session.HasFinished, Is.True, id);
            return new Result(id, session);
        }
        [Test]
        public void FullCourseComparisonsRespectSkillResourcesSnowAndEquipment()
        {
            var neutral = Run("flat_steady");
            var sprint = Run("flat_final_sprint", sprint: true);
            var attack = Run("flat_attack", effort: 1000);
            var intervals = Run("flat_intervals", intervals: true);
            var low = Run("technique_30", stat: "ski_technique", value: 30);
            var high = Run("technique_90", stat: "ski_technique", value: 90);
            var climbLow = Run("climbing_30", "synthetic_climb_v1", stat: "climbing", value: 30);
            var climbHigh = Run("climbing_90", "synthetic_climb_v1", stat: "climbing", value: 90);
            var soft = Run("flat_soft", snow: "fresh_soft");
            var gear = Run("flat_glide_skis", ski: "ski_glide");
            var rolling = Run("rolling_upright", "synthetic_rolling_v1");
            var tuck = Run("rolling_tuck", "synthetic_rolling_v1", tuck: true);
            var technical = Run("technical_packed", "synthetic_technical_descent_v1");
            var ice = Run("technical_ice", "synthetic_technical_descent_v1", snow: "icy_fast");
            Assert.That(high.Seconds, Is.LessThan(low.Seconds));
            Assert.That(climbHigh.Seconds, Is.LessThan(climbLow.Seconds));
            Assert.That(soft.Seconds, Is.GreaterThan(neutral.Seconds));
            Assert.That(gear.Seconds, Is.LessThan(neutral.Seconds));
            Assert.That(neutral.Seconds - gear.Seconds, Is.LessThan(low.Seconds - high.Seconds));
            Assert.That(sprint.Seconds, Is.LessThan(neutral.Seconds));
            Assert.That(sprint.Reserve, Is.LessThan(neutral.Reserve));
            Assert.That(attack.Reserve, Is.LessThan(sprint.Reserve));
            Assert.That(tuck.Reserve, Is.GreaterThan(rolling.Reserve));
            Assert.That(tuck.Fatigue, Is.LessThan(rolling.Fatigue));
            Assert.That(ice.Seconds, Is.GreaterThan(technical.Seconds), "Technical grip loss outweighs improved glide on this curve fixture.");
            var rows = new[] { neutral, sprint, attack, intervals, low, high, climbLow, climbHigh, soft, gear, rolling, tuck, technical, ice };
            var json = new StringBuilder("{\"schema_version\":1,\"id\":\"m2_comparisons_v1\",\"scenarios\":[");
            for (int i = 0; i < rows.Length; i++)
            {
                if (i > 0) json.Append(','); var r = rows[i];
                json.Append(FormattableString.Invariant($"{{\"id\":\"{r.Id}\",\"seconds\":{r.Seconds:R},\"reserve\":{r.Reserve:R},\"fatigue\":{r.Fatigue:R},\"hr_bpm\":{r.HeartRate:R}}}"));
            }
            TestContext.WriteLine("m2_comparisons=" + json.Append("]}"));
        }
        [Test]
        public void LongIntegratedTrialRunsFasterThanRealtimeWithoutAllocations()
        {
            var session = TimeTrialTests.Create(new RaceEventLog(1000), laps: 100);
            session.Step(TimeTrialTests.Input(session));
            var timer = new Stopwatch(); long before = GC.GetAllocatedBytesForCurrentThread(); timer.Start();
            for (int i = 0; i < 100000; i++) session.Step(TimeTrialTests.Input(session));
            timer.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(5000));
            Assert.That(session.PhysiologyState.AnaerobicReserve, Is.InRange(0, 1));
            TestContext.WriteLine(FormattableString.Invariant($"m2_benchmark={{\"ticks\":100000,\"simulated_seconds\":5000,\"elapsed_ms\":{timer.Elapsed.TotalMilliseconds:R},\"allocated_bytes\":{allocated},\"distance_nm\":{session.Motion.DistanceNanometers}}}"));
        }
    }
}

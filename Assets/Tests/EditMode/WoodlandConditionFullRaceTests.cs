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
        private static string Read(string id)=>OwnerM13ExitQueueTests.Read(id);
        private static RaceSnowConditions Conditions(string profile)
        {
            var json=Read("Biathlon/race_snow_neutral_v1")
                .Replace("race_snow_neutral_v1","woodland_wp175_"+profile+"_v1")
                .Replace("packed_neutral",profile);
            return RaceSnowConditions.FromJson(json);
        }
        private static PackAiRaceRunner Create(string profile,IRaceEventSink events,
            string format="sprint",int count=1)
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
            return ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(Root+format+"_profile")),
                RaceRoutePlan.FromJson(Read(Root+format+"_route")),
                Conditions(profile),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),count:count);
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

        [TestCase("fresh_soft","sprint",40)]
        [TestCase("icy_fast","mass_start",30)]
        [Timeout(900000)]
        public void FullWoodlandFieldCompletesUnderNonNeutralSnow(
            string profile,string format,int count)
            =>VerifyFullField(profile,format,count);

        [TestCase("packed_neutral","sprint",40)]
        [TestCase("wet_slow","mass_start",30)]
        [Timeout(900000),NonParallelizable]
        public void AdditionalCanonicalFullFieldConditionsComplete(
            string profile,string format,int count)
            =>VerifyFullField(profile,format,count);

        [Test,Timeout(900000),NonParallelizable]
        public void IcyFastFullSprintQualifiesAndCompletesPursuit()
            =>VerifyFullField("icy_fast","sprint",40);

        [Test,Timeout(900000),NonParallelizable]
        public void WetSlowFullSprintQualifiesAndCompletesPursuit()
            =>VerifyFullField("wet_slow","sprint",40);

        [Test,Timeout(900000),NonParallelizable]
        public void FreshSoftFullMassStartCompletes()
            =>VerifyFullField("fresh_soft","mass_start",30);

        private static void VerifyFullField(string profile,string format,int count)
        {
            // This headless matrix runs multiple large fields sequentially in
            // one testhost; previous completed runners are not race state.
            GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
            var logA=new BalanceEventAudit(5);
            var first=Create(profile,logA,format,count);
            const int limit=50000;
            while(!first.Field.IsComplete&&first.Field.Tick.Value<limit)
            {
                first.Step();
            }
            Assert.That(first.Field.IsComplete,Is.True,
                "The actual full field must finish; a traffic queue is a failing condition.");
            Assert.That(format=="sprint"?first.Field.Result.Count:first.Field.MassStartResult.Count,
                Is.EqualTo(count));
            var firstResult=format=="sprint"?first.Field.Result.ToJson():first.Field.MassStartResult.ToJson();
            Assert.That(firstResult,Is.Not.Empty);
            TestContext.WriteLine("wp175_full_field="+format+" snow="+profile+
                " count="+count+" ticks="+first.Field.Tick.Value+" event_hash="+logA.Hash);
            if(format=="sprint")
            {
                var pursuitProfile=CompetitionProfile.FromJson(Read(Root+"pursuit_profile"));
                var qualification=PursuitQualificationSnapshot.Create(
                    "woodland_wp175_"+profile+"_qualification_v1",first.Field.Result,pursuitProfile);
                first=null;
                GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
                var pursuitEvents=new BalanceEventAudit(5);
                var pursuit=ProductionVenueRaceFactory.Create(venue,pursuitProfile,
                    RaceRoutePlan.FromJson(Read(Root+"pursuit_route")),Conditions(profile),
                    PhysiologyContractTests.DefinitionJson,pursuitEvents,new AiDecisionCounts(),
                    qualification,count:count);
                while(!pursuit.Field.IsComplete&&pursuit.Field.Tick.Value<limit)pursuit.Step();
                Assert.That(pursuit.Field.IsComplete,Is.True,
                    "A Pursuit qualified from the completed same-snow Sprint must finish.");
                Assert.That(pursuit.Field.PursuitResult.Count,Is.EqualTo(count));
                TestContext.WriteLine("wp175_qualified_pursuit snow="+profile+
                    " count="+count+" ticks="+pursuit.Field.Tick.Value+
                    " event_hash="+pursuitEvents.Hash);
            }
        }
    }
}
#endif

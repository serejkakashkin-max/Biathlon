using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitQualificationTests
    {
        public static CompetitionProfile Profile() => CompetitionProfile.FromJson(PhysiologyContractTests.DefinitionJson("pursuit_test_v1.json"));
        public static SprintResultSnapshot Source()
        {
            var p=CompetitionProfileTests.TestProfile();var events=new RaceEventLog(100);
            var dns=new CompetitionLifecycle(p,new AthleteId("dns"),20,events);dns.Step(new SimulationTick(1),CompetitionAction.MarkDidNotStart);
            var dnf=new CompetitionLifecycle(p,new AthleteId("dnf"),20,events);dnf.Step(new SimulationTick(1),CompetitionAction.Start);dnf.Step(new SimulationTick(2),CompetitionAction.MarkDidNotFinish);
            var book=new SprintResultBook(p,SprintResultsTests.Timing(),new[]{new AthleteId("z"),new AthleteId("b"),new AthleteId("a"),dns.AthleteId,dnf.AthleteId},20,events);
            book.Record(SprintResultsTests.Finisher("z",1,201,500000000));book.Record(dns);book.Record(dnf);
            book.Record(SprintResultsTests.Finisher("b",1201,202));book.Record(SprintResultsTests.Finisher("a",601,200,900000000));
            return book.Complete("qualifying_sprint");
        }
        [Test] public void RegisteredTiesAndGapsSurviveImportWithoutInventedNonfinisherTimes()
        {
            var source=Source();string original=source.ToJson();var q=PursuitQualificationSnapshot.Create("pursuit_dependency",source,Profile());
            Assert.That(q.Count,Is.EqualTo(3));Assert.That(q.GapUnitsPerSecond,Is.EqualTo(10));
            Assert.That(q.Get(0).AthleteId,Is.EqualTo("a"));Assert.That(q.Get(1).AthleteId,Is.EqualTo("z"));
            Assert.That(q.Get(0).SourceRank,Is.EqualTo(1));Assert.That(q.Get(1).SourceRank,Is.EqualTo(1));
            Assert.That(q.Get(2).SourceRank,Is.EqualTo(3));Assert.That(q.Get(2).StartOrdinal,Is.EqualTo(3));
            Assert.That(q.Get(0).StartGapUnits,Is.Zero);Assert.That(q.Get(1).StartGapUnits,Is.Zero);Assert.That(q.Get(2).StartGapUnits,Is.EqualTo(1));
            Assert.That(source.ToJson(),Is.EqualTo(original));
            Assert.That(PursuitQualificationSnapshot.FromJson(q.ToJson(),source.Id,Profile()).ToJson(),Is.EqualTo(q.ToJson()));
        }
        [Test] public void ReloadRejectsAlteredRowsTimingAndStaleCallerContext()
        {
            var source=Source();var p=Profile();string json=PursuitQualificationSnapshot.Create("dependency",source,p).ToJson();
            foreach(var pair in new[]{("schema_version","2"),("gap_units_per_second","100"),("start_ordinal","9"),("source_rank","4"),("start_gap_units","77")})
                Assert.Catch(()=>PursuitQualificationSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(json,pair.Item1,pair.Item2),source.Id,p));
            Assert.Catch(()=>PursuitQualificationSnapshot.FromJson(json,"another_sprint",p));
            var other=CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(),"distance_m","6000"));
            Assert.Catch(()=>PursuitQualificationSnapshot.FromJson(json,source.Id,other));
            string alteredSource=json.Replace("\\\"gap_units\\\":0","\\\"gap_units\\\":1");
            Assert.That(alteredSource,Is.Not.EqualTo(json));Assert.Catch(()=>PursuitQualificationSnapshot.FromJson(alteredSource,source.Id,p));
            Assert.Catch(()=>PursuitQualificationSnapshot.FromJson(json.Replace("\"source_result_id\":\"qualifying_sprint\"","\"source_result_id\":\"wrong\""),source.Id,p));
        }
        [Test] public void CapacityCategoryRulesAndEmptyQualificationFailWithoutTruncation()
        {
            var source=Source();var p=Profile();
            foreach(var pair in new[]{("field_capacity","2"),("category","0")})
            {var changed=CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(),pair.Item1,pair.Item2));Assert.Throws<ArgumentException>(()=>PursuitQualificationSnapshot.Create("q",source,changed));}
            var otherRules=CompetitionProfile.FromJson(p.ToJson().Replace("project_synthetic_v1","project_other_v1"));
            Assert.Throws<ArgumentException>(()=>PursuitQualificationSnapshot.Create("q",source,otherRules));
            Assert.Throws<ArgumentException>(()=>PursuitQualificationSnapshot.Create("q",source,CompetitionProfileTests.TestProfile()));
            var dns=new CompetitionLifecycle(CompetitionProfileTests.TestProfile(),new AthleteId("dns"),20,new RaceEventLog(10));dns.Step(new SimulationTick(1),CompetitionAction.MarkDidNotStart);
            var book=new SprintResultBook(CompetitionProfileTests.TestProfile(),SprintResultsTests.Timing(),new[]{dns.AthleteId},20,new RaceEventLog(10));book.Record(dns);
            Assert.Throws<ArgumentException>(()=>PursuitQualificationSnapshot.Create("q",book.Complete("empty"),p));
        }
        [Test] public void CompleteAiSprintProducesAReusableVersionedPursuitDependency()
        {
            var runner=StadiumFieldTests.Create(2,new RaceEventLog(30000),new AiDecisionCounts());
            while(!runner.Field.IsComplete && runner.Field.Tick.Value<20000)runner.Step();
            Assert.That(runner.Field.IsComplete,Is.True);
            var source=runner.Field.Result;var q=PursuitQualificationSnapshot.Create("real_sprint_dependency",source,Profile());
            var restored=PursuitQualificationSnapshot.FromJson(q.ToJson(),source.Id,Profile());
            Assert.That(restored.Count,Is.EqualTo(2));Assert.That(restored.Get(0).StartGapUnits,Is.Zero);
            for(int i=0;i<2;i++) {Assert.That(restored.Get(i).AthleteId,Is.EqualTo(source.Get(i).AthleteId));Assert.That(restored.Get(i).StartGapUnits,Is.EqualTo(source.Get(i).GapUnits));}
        }
    }
}

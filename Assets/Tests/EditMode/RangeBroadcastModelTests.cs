using System;
using System.Runtime.Serialization;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RangeBroadcastModelTests
    {
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
        private static RangeBroadcastDefinition Policy()=>RangeBroadcastDefinition.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/range_broadcast_v1.json"));
        [Test]public void RouteWindowOpensBeforeEntryAndStaysAcrossLapSeamAfterExit()
        {
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var field=runner.Field;var model=new RangeBroadcastModel(Policy(),field.Track,field.Route,field.Profile,field.Count);
            var section=field.Route.GetSection(0);double entry=section.RouteDistanceOf(field.Track.Range.Entry.Distance).Meters;
            double exit=section.RouteDistanceOf(field.Track.Range.Exit.Distance).Meters;
            Assert.That(model.Window(entry-250-.001,true,CompetitionPhase.Skiing),Is.EqualTo(-1));
            Assert.That(model.Window(entry-250,true,CompetitionPhase.Skiing),Is.EqualTo(0));
            Assert.That(model.Window(entry,true,CompetitionPhase.Range),Is.EqualTo(0));
            Assert.That(model.Window(exit+250,true,CompetitionPhase.Skiing),Is.EqualTo(0));
            Assert.That(model.Window(exit+250+.001,true,CompetitionPhase.Skiing),Is.EqualTo(-1));
            Assert.That(model.Window(entry,false,CompetitionPhase.Prestart),Is.EqualTo(-1));
            Assert.That(model.Window(entry,true,CompetitionPhase.Finished),Is.EqualTo(-1));
        }
        [TestCase(0,0,0)][TestCase(3,1,2)][TestCase(5,31,0)][TestCase(5,0,5)]
        public void MissDotsAreShotsMinusActualClosedTargets(int shots,int mask,int misses)
        {Assert.That(new RangeBroadcastRow("athlete",1,2,shots,mask,true).Misses,Is.EqualTo(misses));}
        [Test]public void FlashIsBoundedAndCannotReappearBeforeTheMissOnReplay()
        {
            var row=new RangeBroadcastRow("athlete",1,2,1,0,true,100);
            Assert.That(row.Flashing(100,20,1),Is.True);Assert.That(row.Flashing(119,20,1),Is.True);
            Assert.That(row.Flashing(120,20,1),Is.False);Assert.That(row.Flashing(99,20,1),Is.False);
        }
        [Test]public void InvalidPresentationPoliciesAndImpossibleRowsAreRejected()
        {
            string json=PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/range_broadcast_v1.json");
            Assert.Throws<SerializationException>(()=>RangeBroadcastDefinition.FromJson(json.Replace("\"before_m\": 250","\"before_m\": -1")));
            Assert.Throws<ArgumentException>(()=>new RangeBroadcastRow("athlete",1,2,1,31,true));
        }
        [Test]public void AcceptedSeriesPersistsAfterExitAndSeekNeverInventsMissFlashes()
        {
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var f=runner.Field;var model=new RangeBroadcastModel(Policy(),f.Track,f.Route,f.Profile,f.Count);
            model.Observe(RacePresentationSnapshot.Capture(f));RacePresentationSnapshot rewind=null;
            while(f.GetAthlete(0).Lifecycle.CompletedBouts==0&&f.Tick.Value<20000)
            {
                runner.Step();var s=RacePresentationSnapshot.Capture(f);model.Observe(s);
                if(s.Get(0).ShotsFired==1&&rewind==null)rewind=s;
                var tick=f.Tick;model.Observe(s);Assert.That(f.Tick,Is.EqualTo(tick));
            }
            Assert.That(f.GetAthlete(0).Lifecycle.CompletedBouts,Is.EqualTo(1));
            var row=model.Get(0);Assert.That(row.Bout,Is.EqualTo(1));Assert.That(row.Shots,Is.EqualTo(5));
            Assert.That(row.ClosedMask,Is.EqualTo(f.GetAthlete(0).GetBoutClosedMask(0)));
            Assert.That(rewind,Is.Not.Null);model.Observe(rewind);
            Assert.That(model.Get(0).Shots,Is.EqualTo(1));Assert.That(model.Get(0).LastMissTick,Is.EqualTo(-1));
        }
    }
}

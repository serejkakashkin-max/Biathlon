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
        private static PackAiRaceRunner Runner(CompetitionFormat format=CompetitionFormat.Sprint,int count=2)
        {
            PursuitQualificationSnapshot qualification=null;
            if(format==CompetitionFormat.Pursuit)
            {var sprint=Runner(CompetitionFormat.Sprint,count);while(!sprint.Field.IsComplete&&sprint.Field.Tick.Value<30000)sprint.Step();Assert.That(sprint.Field.IsComplete,Is.True);qualification=PursuitQualificationSnapshot.Create("broadcast_qualifier",sprint.Field.Result,CompetitionProfile.FromJson(Read("pursuit_test_v1.json")));}
            return ChampionshipRaceFactory.Create(Read,Fixture,format,new BalanceEventAudit(5),new AiDecisionCounts(),qualification:qualification,count:count);
        }
        private static RangeBroadcastModel Model(PackAiRaceRunner r,int count)=>new RangeBroadcastModel(Policy(),r.Field.Track,r.Field.Route,r.Field.Profile,count);
        private static RangeBroadcastObservation A(string id,int lane,int shots=0,int mask=0,bool active=true,int bouts=0,int last=0)=>new RangeBroadcastObservation(id,bouts,lane,shots,mask,last,active);
        [Test]public void RouteWindowOpensBeforeEntryAndStaysAcrossLapSeamAfterExit()
        {
            var runner=Runner();var f=runner.Field;var model=Model(runner,2);var section=f.Route.GetSection(0);
            double entry=section.RouteDistanceOf(f.Track.Range.Entry.Distance).Meters,exit=section.RouteDistanceOf(f.Track.Range.Exit.Distance).Meters;
            Assert.That(model.Window(entry-250-.001,true,CompetitionPhase.Skiing),Is.EqualTo(-1));Assert.That(model.Window(entry-250,true,CompetitionPhase.Skiing),Is.EqualTo(0));
            Assert.That(model.Window(entry,true,CompetitionPhase.Range),Is.EqualTo(0));Assert.That(model.Window(exit+250,true,CompetitionPhase.Skiing),Is.EqualTo(0));
            Assert.That(model.Window(exit+250+.001,true,CompetitionPhase.Skiing),Is.EqualTo(-1));Assert.That(model.Window(entry,false,CompetitionPhase.Prestart),Is.EqualTo(-1));Assert.That(model.Window(entry,true,CompetitionPhase.Finished),Is.EqualTo(-1));
        }
        [TestCase(0,0,0)][TestCase(3,1,2)][TestCase(5,31,0)][TestCase(5,0,5)]
        public void MissCountIsShotsMinusActualClosedTargets(int shots,int mask,int misses)=>Assert.That(new RangeBroadcastRow("athlete",1,2,shots,mask,true).Misses,Is.EqualTo(misses));
        [Test]public void FlashIsBoundedAndCannotReappearBeforeTheMissOnReplay()
        {
            var row=new RangeBroadcastRow("athlete",1,2,1,0,true,100);Assert.That(row.Flashing(100,20,1),Is.True);Assert.That(row.Flashing(119,20,1),Is.True);Assert.That(row.Flashing(120,20,1),Is.False);Assert.That(row.Flashing(99,20,1),Is.False);
        }
        [Test]public void InvalidPresentationPoliciesAndImpossibleRowsAreRejected()
        {
            string json=PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/range_broadcast_v1.json");
            foreach(var invalid in new[]{json.Replace("\"before_m\": 250","\"before_m\": -1"),json.Replace("\"maximum_rows\": 20","\"maximum_rows\": 31"),json.Replace("\"fade_seconds\": 0.4","\"fade_seconds\": 0.1")})Assert.Throws<SerializationException>(()=>RangeBroadcastDefinition.FromJson(invalid));
            Assert.Throws<ArgumentException>(()=>new RangeBroadcastRow("athlete",1,2,1,31,true));
            Assert.That(Policy().MaximumRows,Is.EqualTo(20));Assert.That(Policy().MaximumAthletes,Is.EqualTo(30));
        }
        [Test]public void AcceptedSeriesPersistsAsDataAfterExitAndSeekNeverInventsFlashes()
        {
            var r=Runner();var f=r.Field;var m=Model(r,2);m.Observe(RacePresentationSnapshot.Capture(f));RacePresentationSnapshot rewind=null;
            while(f.GetAthlete(0).Lifecycle.CompletedBouts==0&&f.Tick.Value<20000)
            {r.Step();var s=RacePresentationSnapshot.Capture(f);m.Observe(s);m.Compose(0,20);if(s.Get(0).ShotsFired==1&&rewind==null)rewind=s;long tick=f.Tick.Value;m.Observe(s);Assert.That(f.Tick.Value,Is.EqualTo(tick));}
            Assert.That(f.GetAthlete(0).Lifecycle.CompletedBouts,Is.EqualTo(1));var row=m.Get(0);Assert.That(row.Shots,Is.EqualTo(5));Assert.That(row.ClosedMask,Is.EqualTo(f.GetAthlete(0).GetBoutClosedMask(0)));
            Assert.That(row.Shot(4),Is.Not.EqualTo(RangeShotMark.Pending),"Final-shot release in the same accepted state retains its outcome.");
            Assert.That(rewind,Is.Not.Null);m.Observe(rewind);Assert.That(m.Get(0).Shots,Is.EqualTo(1));Assert.That(m.Get(0).LastMissTick,Is.EqualTo(-1));Assert.That(m.Get(0).CompletionObserved,Is.False);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void EveryChronologicalPositionCanMissEvenWhenTargetsCloseInReverseOrder(int missedShot)
        {
            var r=Runner();var m=Model(r,2);var a=new[]{A("a",7),A("b",3)};string p=r.Field.Profile.Id;m.ObserveAccepted(0,20,p,a);m.Compose(0,20);int mask=0;
            for(int shot=0;shot<5;shot++)
            {if(shot!=missedShot)mask|=1<<(4-shot);a[0]=A("a",7,shot+1,mask);m.ObserveAccepted(shot+1,20,p,a);Assert.That(m.Get(0).Shot(shot),Is.EqualTo(shot==missedShot?RangeShotMark.Miss:RangeShotMark.Hit));}
            var row=m.Get(0);Assert.That(row.Misses,Is.EqualTo(1));Assert.That(row.CompletedTick,Is.EqualTo(5));Assert.That(row.CompletionObserved,Is.True);m.ObserveAccepted(5,20,p,a);Assert.That(m.Get(0).CompletedTick,Is.EqualTo(5));
        }
        [Test]public void ThirtyAthletesKeepStableSlotsAndOverflowWithoutPagesOrPlayerPriority()
        {
            var r=Runner();var m=Model(r,30);var a=new RangeBroadcastObservation[30];for(int i=0;i<30;i++)a[i]=A("athlete_"+i,30-i);string p=r.Field.Profile.Id;
            m.ObserveAccepted(0,20,p,a);Assert.That(m.Compose(0,20),Is.EqualTo(20));Assert.That(m.OverflowCount,Is.EqualTo(10));Assert.That(m.ActiveCount,Is.EqualTo(30));
            Assert.That(m.Displayed(0),Is.EqualTo(29));Assert.That(m.Displayed(19),Is.EqualTo(10));
            for(int t=1;t<=120;t++){m.ObserveAccepted(t,20,p,a);m.Compose(0,20);Assert.That(m.Displayed(0),Is.EqualTo(29));Assert.That(m.Displayed(19),Is.EqualTo(10));}
            for(int i=0;i<Count30;i++)Assert.That(m.Get(i).Active,Is.True,"Overflow never deletes active data.");
            for(int t=121;t<=125;t++)
            {a[29]=A("athlete_29",1,t-120,(1<<(t-120))-1);m.ObserveAccepted(t,20,p,a);m.Compose(0,20);}
            long done=m.Get(29).CompletedTick;
            for(int t=126;t<=done+68;t++){m.ObserveAccepted(t,20,p,a);m.Compose(0,20);if(t<done+68)Assert.That(m.Displayed(0),Is.EqualTo(29));}
            Assert.That(m.Get(29).Phase(done+64,20,Policy()),Is.EqualTo(RangeRowPhase.Fading));Assert.That(m.Get(29).Opacity(done+64,20,Policy()),Is.EqualTo(.5).Within(.001));
            Assert.That(m.Displayed(0),Is.EqualTo(28));Assert.That(m.Displayed(19),Is.EqualTo(9));Assert.That(m.OverflowCount,Is.EqualTo(9));
            m.Compose(0,12);Assert.That(m.ActiveCount,Is.EqualTo(29));m.Compose(0,20);Assert.That(m.Displayed(19),Is.EqualTo(9));
        }
        private const int Count30=30;
        [Test]public void SequentialArrivalsKeepOrderAndCompletedQueuedResultGetsItsOwnHold()
        {
            var r=Runner();var m=Model(r,3);string p=r.Field.Profile.Id;var a=new[]{A("a",9),A("b",1,active:false),A("c",2,active:false)};
            m.ObserveAccepted(0,20,p,a);m.Compose(0,1);a[1]=A("b",1);m.ObserveAccepted(1,20,p,a);m.Compose(0,1);Assert.That(m.Displayed(0),Is.Zero);
            for(int t=2;t<=6;t++){a[1]=A("b",1,t-1,(1<<(t-1))-1);m.ObserveAccepted(t,20,p,a);m.Compose(0,1);}
            for(int t=7;t<=100;t++){m.ObserveAccepted(t,20,p,a);m.Compose(0,1);}Assert.That(m.OverflowCount,Is.EqualTo(1));
            for(int t=101;t<=105;t++){a[0]=A("a",9,t-100,(1<<(t-100))-1);m.ObserveAccepted(t,20,p,a);m.Compose(0,1);}
            for(int t=106;t<=173;t++){m.ObserveAccepted(t,20,p,a);m.Compose(0,1);}
            Assert.That(m.Displayed(0),Is.EqualTo(1));Assert.That(m.Get(1).AdmissionTick,Is.EqualTo(173));Assert.That(m.Get(1).Phase(173,20,Policy()),Is.EqualTo(RangeRowPhase.Retained));
        }
        [Test]public void SeekRebasesMixedUnknownShotOrderAndNeverResurrectsHistoricalCompletion()
        {
            var r=Runner();var m=Model(r,2);string p=r.Field.Profile.Id;var a=new[]{A("a",1),A("b",2)};m.ObserveAccepted(0,20,p,a);m.Compose(0,20);
            a[0]=A("a",1,3,5);a[1]=A("b",2,active:false,bouts:1,last:31);m.ObserveAccepted(100,20,p,a);m.Compose(0,20);
            Assert.That(m.Get(0).Shot(0),Is.EqualTo(RangeShotMark.Unobserved));Assert.That(m.Get(0).Shot(2),Is.EqualTo(RangeShotMark.Unobserved));Assert.That(m.Get(0).Shot(3),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(m.Get(0).LastMissTick,Is.EqualTo(-1));Assert.That(m.Get(1).Eligible,Is.False);Assert.That(m.VisibleCount,Is.EqualTo(1));
            a[0]=A("a",1,5,31);m.ObserveAccepted(50,20,p,a);Assert.That(m.Get(0).CompletionObserved,Is.False);Assert.That(m.Get(0).LastHitTick,Is.EqualTo(-1));
        }
        [TestCase(15,RangeShotMark.Miss)][TestCase(31,RangeShotMark.Hit)]
        public void FinalAcceptedShotAndSessionReleaseShareOneCompletionAndKeepTheExactFinalMark(int finalMask,RangeShotMark expected)
        {
            var r=Runner();var m=Model(r,2);string p=r.Field.Profile.Id;var a=new[]{A("a",5,4,15),A("b",2)};
            m.ObserveAccepted(0,20,p,a);m.Compose(0,20);a[0]=A("a",0,active:false,bouts:1,last:finalMask);m.ObserveAccepted(1,20,p,a);
            Assert.That(m.Get(0).Lane,Is.EqualTo(5));Assert.That(m.Get(0).Shot(4),Is.EqualTo(expected));Assert.That(m.Get(0).CompletedTick,Is.EqualTo(1));
            m.ObserveAccepted(1,20,p,a);m.ObserveAccepted(2,20,p,a);Assert.That(m.Get(0).CompletedTick,Is.EqualTo(1));
            Assert.That(m.Get(0).LastMissTick,Is.EqualTo(expected==RangeShotMark.Miss?1:-1));
        }
        [Test]public void FiveAcceptedMissesRemainFiveCrossesAndAZeroScore()
        {
            var r=Runner();var m=Model(r,2);string p=r.Field.Profile.Id;var a=new[]{A("a",1),A("b",2)};m.ObserveAccepted(0,20,p,a);
            for(int t=1;t<=5;t++){a[0]=A("a",1,t);m.ObserveAccepted(t,20,p,a);}
            for(int shot=0;shot<5;shot++)Assert.That(m.Get(0).Shot(shot),Is.EqualTo(RangeShotMark.Miss));
            Assert.That(m.Get(0).Misses,Is.EqualTo(5));Assert.That(RangeBroadcastRow.Hits(m.Get(0).ClosedMask),Is.Zero);
        }
        [Test]public void VisitsAreIndependentAndSteadyQueueObservationDoesNotAllocate()
        {
            var r=Runner();var m=Model(r,2);string p=r.Field.Profile.Id;var a=new[]{A("a",1,4,15),A("b",2)};m.ObserveAccepted(0,20,p,a);m.Compose(0,20);
            a[0]=A("a",1,active:false,bouts:1,last:31);m.ObserveAccepted(1,20,p,a);Assert.That(m.Get(0,0).Shot(4),Is.EqualTo(RangeShotMark.Hit));a[0]=A("a",8,bouts:1);m.ObserveAccepted(2,20,p,a);m.Compose(1,20);
            Assert.That(m.Get(0,0).CompletedTick,Is.EqualTo(1));Assert.That(m.Get(0,1).Shots,Is.Zero);Assert.That(m.Get(0,1).Lane,Is.EqualTo(8));
            for(int t=3;t<100;t++){m.ObserveAccepted(t,20,p,a);m.Compose(1,20);}long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int t=100;t<500;t++){m.ObserveAccepted(t,20,p,a);m.Compose(1,20);}long allocated=GC.GetAllocatedBytesForCurrentThread()-bytes;Assert.That(allocated,Is.Zero);
        }
        [TestCase(CompetitionFormat.Sprint)][TestCase(CompetitionFormat.Pursuit)][TestCase(CompetitionFormat.MassStart)]
        public void AllCurrentlySupportedFormatsBindTheSameObserver(CompetitionFormat format)
        {var r=Runner(format);var m=Model(r,2);m.Observe(RacePresentationSnapshot.Capture(r.Field));Assert.That(m.Compose(0,20),Is.Zero);}
    }
}

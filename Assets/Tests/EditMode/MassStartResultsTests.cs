using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class MassStartResultsTests
    {
        private static CompetitionLifecycle Finish(string id,long start,long finish,int fraction)
        {
            var race=new CompetitionLifecycle(StartApproachTests.Profile(),new AthleteId(id),20,new RaceEventLog(100));
            for(long t=1;t<start;t++)race.Step(new SimulationTick(t));
            race.Step(new SimulationTick(start),CompetitionAction.Start);
            for(int bout=0;bout<4;bout++)
            {
                race.Step(race.Tick.Next(),CompetitionAction.EnterRange);
                race.Step(race.Tick.Next(),CompetitionAction.CompleteBout,31,5);
                race.Step(race.Tick.Next(),CompetitionAction.ReachLapBoundary);
            }
            long last=finish+(fraction>0?1:0);
            while(race.Tick.Value<last-1)race.Step(race.Tick.Next());
            race.Step(race.Tick.Next(),CompetitionAction.Finish,finish:new FinishTime(finish,fraction,20));
            return race;
        }
        private static MassStartGrid Qualification()=>new MassStartGrid(StartApproachTests.Profile(),MassStartGridTests.Definition(),OccupancyTests.Definition(),new[]{new AthleteId("a"),new AthleteId("z"),new AthleteId("b")},6,30);
        [Test] public void ExactCrossingOrdersFinishersInsideOneRegisteredDisplayUnit()
        {
            var q=Qualification();var a=Finish("a",1,100,100);var z=Finish("z",1,100,200);var b=Finish("b",1,100,300);
            Assert.That(a.GlobalFinishTime.CompareTo(b.GlobalFinishTime),Is.LessThan(0));
            Assert.That(SprintResultsTests.Timing().Register(a.GlobalFinishTime),Is.EqualTo(SprintResultsTests.Timing().Register(b.GlobalFinishTime)));
            var result=MassStartResultSnapshot.Create("finish",q,20,new SimulationTick(1),new[]{b,z,a});
            Assert.That(result.Get(0).AthleteId,Is.EqualTo("a"));Assert.That(result.Get(1).AthleteId,Is.EqualTo("z"));Assert.That(result.Get(2).AthleteId,Is.EqualTo("b"));
            Assert.That(result.Get(2).Rank,Is.EqualTo(3));
            Assert.That(MassStartResultSnapshot.FromJson(result.ToJson(),q,20,new SimulationTick(1)).ToJson(),Is.EqualTo(result.ToJson()));
        }
        [Test] public void ExactTiesShareRankAndMalformedResultsOrWrongContextAreRejected()
        {
            var q=Qualification();var a=Finish("a",1,100,100);var z=Finish("z",1,100,100);var b=Finish("b",1,100,200);
            var result=MassStartResultSnapshot.Create("finish",q,20,new SimulationTick(1),new[]{b,z,a});
            Assert.That(result.Get(0).Rank,Is.EqualTo(1));Assert.That(result.Get(1).Rank,Is.EqualTo(1));Assert.That(result.Get(2).Rank,Is.EqualTo(3));
            string json=result.ToJson();
            foreach(var pair in new[]{("schema_version","2"),("rank","7"),("start_tick","9"),("finish_fraction_parts","1000000000"),("finish_whole_ticks","0")})
                Assert.Catch(()=>MassStartResultSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(json,pair.Item1,pair.Item2),q,20,new SimulationTick(1)));
            Assert.Catch(()=>MassStartResultSnapshot.FromJson(json,q,10,new SimulationTick(1)));
            Assert.Catch(()=>MassStartResultSnapshot.FromJson(json,q,20,new SimulationTick(2)));
            Assert.Catch(()=>MassStartResultSnapshot.Create("r",q,20,new SimulationTick(1),new[]{a,a,b}));
            Assert.Catch(()=>MassStartResultSnapshot.Create("r",q,20,new SimulationTick(1),new[]{a,b}));
            var pending=new CompetitionLifecycle(StartApproachTests.Profile(),new AthleteId("z"),20,new RaceEventLog(10));
            Assert.Catch(()=>MassStartResultSnapshot.Create("r",q,20,new SimulationTick(1),new[]{a,pending,b}));
        }
    }
}

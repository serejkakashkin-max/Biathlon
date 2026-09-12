using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class RaceHudTests
    {
        [Test] public void SprintHudTracksSplitsAndFinalRegisteredResults()
        {
            string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
            string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            Assert.That(RacePresentationSnapshot.Capture(runner.Field).Get(0).Hud.Rank,Is.Zero);
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<50000)
            {
                runner.Step();if(runner.Field.Tick.Value%100!=0)continue;
                var snapshot=RacePresentationSnapshot.Capture(runner.Field);
                for(int i=0;i<2;i++){var a=runner.Field.GetAthlete(i);var h=snapshot.Get(i).Hud;if(!h.Final&&h.CheckpointOrdinal>=0)Assert.That(h.GapSeconds,Is.EqualTo(runner.Field.Timing.GapSeconds(a.AthleteId,h.CheckpointOrdinal)));}
            }
            Assert.That(runner.Field.IsComplete,Is.True);var final=RacePresentationSnapshot.Capture(runner.Field);
            var timing=ResultTimingDefinition.FromJson(runner.Field.Result.TimingDefinitionJson);
            for(int i=0;i<2;i++)
            {
                var h=final.Get(i).Hud;Assert.That(h.Final,Is.True);Assert.That(h.NextRangeAfterLap,Is.Zero);Assert.That(h.Lap,Is.EqualTo(h.LapCount));Assert.That(h.ShootingResults,Does.Not.Contain("-"));
                for(int j=0;j<2;j++){var row=runner.Field.Result.Get(j);if(row.AthleteId!=final.Get(i).AthleteId)continue;Assert.That(h.Rank,Is.EqualTo(row.Rank));Assert.That(h.GapSeconds,Is.EqualTo(row.GapUnits/(double)timing.UnitsPerSecond));}
            }
        }
    }
}

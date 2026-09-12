using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class RacePresentationTests
    {
        [Test] public void DetachedSnapshotsAndInterpolationCannotChangeRaceOrPriorFrames()
        {
            string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
            var audit=new BalanceEventAudit(5);var controlAudit=new BalanceEventAudit(5);
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,audit,new AiDecisionCounts(),count:2);
            var control=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,controlAudit,new AiDecisionCounts(),count:2);
            var original=RacePresentationSnapshot.Capture(runner.Field);var previous=original;
            Assert.That(original.Tick,Is.Zero);Assert.That(original.Get(0).Phase,Is.EqualTo(CompetitionPhase.Prestart));
            bool interpolated=false;
            for(int tick=0;tick<100;tick++)
            {
                runner.Step();control.Step();var current=RacePresentationSnapshot.Capture(runner.Field);
                for(int i=0;i<current.Count;i++)
                {
                    Assert.That(current.Get(i).Effort.DeliveredPowerWatts,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame.Physiology.Effort.DeliveredPowerWatts));
                    Assert.That(current.Get(i).Draft.DragReduction,Is.EqualTo(runner.Field.GetDraft(i).DragReduction));
                    var p=RacePresentationSampling.Position(previous,current,i,0.5);interpolated|=p.Interpolated;
                    Assert.That(current.Get(i).AthleteId,Is.EqualTo(runner.Field.GetAthlete(i).AthleteId.Value));
                    Assert.That(current.Get(i).Physiology.HeartRateBpm,Is.EqualTo(runner.Field.GetAthlete(i).PhysiologyState.HeartRateBpm));
                    Assert.That(runner.Field.GetAthlete(i).Motion.DistanceNanometers,Is.EqualTo(control.Field.GetAthlete(i).Motion.DistanceNanometers));
                    if(p.Interpolated)Assert.That(p.LocalMeters,Is.InRange(previous.Get(i).LocalNanometers/1e9,current.Get(i).LocalNanometers/1e9));
                }
                previous=current;
            }
            Assert.That(interpolated,Is.True);Assert.That(original.Tick,Is.Zero);Assert.That(original.Get(0).Phase,Is.EqualTo(CompetitionPhase.Prestart));Assert.That(audit.Hash,Is.EqualTo(controlAudit.Hash));
            Assert.That(RacePresentationSampling.Position(original,previous,0,0.5).Interpolated,Is.False);
            Assert.Catch(()=>RacePresentationSampling.Position(original,previous,0,double.NaN));
        }
    }
}

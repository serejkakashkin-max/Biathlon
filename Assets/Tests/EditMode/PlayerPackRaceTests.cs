using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class PlayerPackRaceTests
    {
        [Test] public void PlayerCommandsFinishAgainstAiAndRepeatExactEvents()
        {
            string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
            string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
            var logA=new BalanceEventAudit(5);var logB=new BalanceEventAudit(5);
            var a=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,logA,new AiDecisionCounts(),count:2);
            var b=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,logB,new AiDecisionCounts(),count:2);
            var id=a.Field.GetAthlete(0).AthleteId;
            while(!a.Field.IsComplete&&a.Field.Tick.Value<50000)
            {
                var tick=a.Field.Tick.Next();
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,800),1,1,0),new WeaponAimCommand(tick,id,1000,1000),true);
                a.StepWithPlayer(0,input,0);b.StepWithPlayer(0,input,0);
            }
            Assert.That(a.Field.IsComplete,Is.True);Assert.That(b.Field.Result.ToJson(),Is.EqualTo(a.Field.Result.ToJson()));Assert.That(logB.Hash,Is.EqualTo(logA.Hash));
            Assert.That(a.Field.GetAthlete(0).TotalPenaltyLoops,Is.EqualTo(10));Assert.That(a.TacticalDecisions,Is.GreaterThan(0));
        }
        [Test] public void InvalidIdentityAndModeSwitchDoNotAdvanceRace()
        {
            string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
            string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
            var a=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var id=a.Field.GetAthlete(0).AthleteId;var t=new SimulationTick(1);
            var c=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(t,id,800),1,1,0),new WeaponAimCommand(t,id,0,0));
            Assert.Catch(()=>a.StepWithPlayer(1,c,0));Assert.That(a.Field.Tick.Value,Is.Zero);Assert.That(a.IsFaulted,Is.False);
            a.StepWithPlayer(0,c,0);Assert.Catch(()=>a.Step());Assert.That(a.Field.Tick.Value,Is.EqualTo(1));Assert.That(a.IsFaulted,Is.False);
        }
    }
}

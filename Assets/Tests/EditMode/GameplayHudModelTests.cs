using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class GameplayHudModelTests
    {
        [TestCase(0,"00:00.0")]
        [TestCase(61.25,"01:01.2")]
        [TestCase(3601.5,"01:00:01.5")]
        public void ClockFormatsAthleteElapsedTime(double seconds,string expected)
        {Assert.That(GameplayHudModel.FormatClock(seconds),Is.EqualTo(expected));}

        [TestCase(ShootingPhase.Setup,0,true,"weapon_setup")]
        [TestCase(ShootingPhase.Aiming,0,true,"weapon_reload")]
        [TestCase(ShootingPhase.BoltCycle,1,true,"weapon_cycling")]
        [TestCase(ShootingPhase.Aiming,1,false,"weapon_ready")]
        [TestCase(ShootingPhase.Aiming,5,true,"weapon_done")]
        public void WeaponPromptMatchesTheAcceptedPhase(ShootingPhase phase,int shots,bool bolt,string expected)
        {Assert.That(GameplayHudModel.WeaponKey(phase,shots,bolt),Is.EqualTo(expected));}

        [TestCase(false,false,1,"breath_ready")]
        [TestCase(false,true,.5,"breath_hold")]
        [TestCase(false,true,1.2,"breath_strained")]
        [TestCase(true,true,.5,"breath_release")]
        public void BreathPromptsDistinguishBenefitStrainAndRecovery(bool blocked,bool holding,double multiplier,string expected)
        {Assert.That(GameplayHudModel.BreathKey(blocked,holding,multiplier),Is.EqualTo(expected));}

        [Test] public void LocalizedSnapshotsUseIndividualStartAndRemainReadOnly()
        {
            string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
            string Fixture(string n)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n);
            var runner=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:2);
            var text=UiTextCatalog.FromJson(PhysiologyContractTests.ProjectFile("Assets/Resources/Biathlon/UI/front_end_text_v1.json"));
            for(int i=0;i<100;i++)runner.Step();
            var snapshot=RacePresentationSnapshot.Capture(runner.Field);var tick=runner.Field.Tick;
            for(int i=0;i<2;i++)
            {
                var a=snapshot.Get(i);var actual=runner.Field.GetAthlete(i);
                Assert.That(a.ElapsedSeconds,Is.EqualTo(actual.Lifecycle.LastFrame.ElapsedSeconds));
                Assert.That(a.StartCountdownSeconds,Is.EqualTo(System.Math.Max(0,(actual.ScheduledStart.Value-tick.Value)/(double)runner.Field.TickFrequency)));
                foreach(string locale in new[]{"ru","en"})
                {
                    var model=new GameplayHudModel(a,text,locale);
                    Assert.That(model.Clock,Is.Not.Empty);Assert.That(model.Timing,Is.Not.Empty);
                    if(a.Hud.Rank==0)Assert.That(model.Timing,Is.EqualTo(text.Text("hud.timing_pending",locale)));
                    Assert.That(model.ReservePercent,Is.InRange(0,100));
                }
            }
            Assert.That(snapshot.Get(1).StartCountdownSeconds,Is.GreaterThan(0));
            Assert.That(runner.Field.Tick,Is.EqualTo(tick));
        }
    }
}

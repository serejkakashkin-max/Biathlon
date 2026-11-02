using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Paired production-route calibration before the first firing bout.
    /// The same athlete, player commands, snow and seed isolate climbing skill.</summary>
    public sealed class WoodlandClimbCalibrationTests
    {
        private const string Root="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string id)
        {
            // These110/650/1000m calibration witnesses predate course shortening.
            string name=id.StartsWith("Biathlon/",StringComparison.Ordinal)?id:"Biathlon/"+id;
            if(name.EndsWith(".json",StringComparison.Ordinal))name=name.Substring(0,name.Length-5);
            return PhysiologyContractTests.ProjectFile(name.StartsWith(Root,StringComparison.Ordinal)?"dotnet/Biathlon.Tests/Fixtures/woodland_d518195/"+name.Substring(Root.Length)+".json":"Assets/Resources/"+name+".json");
        }

        private static (long stadium,long afterClimb,long afterDescent) Run(int climbing)
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
            var ski=TimeTrialAthlete.FromJson(PhysiologyContractTests.ReplaceNumber(
                Read("Biathlon/time_trial_athlete_v1"),"climbing",climbing.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            const string id="woodland_climb_pair_athlete";
            ski=TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id));
            var shooting=ShootingAthleteDefinition.FromJson(Read("Biathlon/shooting_athlete_v1"));
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,id));
            var runner=ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(Root+"sprint_profile")),
                RaceRoutePlan.FromJson(Read(Root+"sprint_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),Read,
                new BalanceEventAudit(5),new AiDecisionCounts(),count:1,seed:177,
                suppliedEntrants:new[]{new SprintEntrant(ski,shooting,"ski_neutral")},
                suppliedPersonalities:new[]{AiPersonalityDefinition.FromJson(Read("Biathlon/ai_personality_attacker_v1"))});
            long stadium=0,afterClimb=0,afterDescent=0;
            while(afterDescent==0&&runner.Field.Tick.Value<12000)
            {
                var actor=runner.Field.GetAthlete(0);var tick=runner.Field.Tick.Next();
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,700),1,1,0),
                    new WeaponAimCommand(tick,actor.AthleteId,0,0));
                runner.StepWithPlayer(0,input,0);
                var point=runner.Field.GetPhysicalPoint(0);
                if(point.LoopId!="main")continue;
                if(stadium==0&&point.LocalNanometers>=110*SkiState.NanometersPerMeter)
                    stadium=tick.Value;
                if(afterClimb==0&&point.LocalNanometers>=650*SkiState.NanometersPerMeter)
                    afterClimb=tick.Value;
                if(afterDescent==0&&point.LocalNanometers>=1000*SkiState.NanometersPerMeter)
                    afterDescent=tick.Value;
            }
            Assert.That(stadium,Is.GreaterThan(0));
            Assert.That(afterClimb,Is.GreaterThan(stadium));
            Assert.That(afterDescent,Is.GreaterThan(afterClimb));
            Assert.That(runner.Field.GetAthlete(0).ActiveShooting,Is.Null,
                "Paired timing must end before shooting can affect the comparison.");
            return(stadium,afterClimb,afterDescent);
        }

        [Test]
        public void ProductionWoodlandClimbSkillAffectsFirstLapWithoutShootingConfound()
        {
            var low=Run(30);var high=Run(90);
            TestContext.WriteLine("wp177_climbing_30_ticks="+low.stadium+","+low.afterClimb+","+low.afterDescent);
            TestContext.WriteLine("wp177_climbing_90_ticks="+high.stadium+","+high.afterClimb+","+high.afterDescent);
            Assert.That(high.afterClimb,Is.LessThan(low.afterClimb),
                "The same commands and venue should reward higher climbing skill by the first long climb.");
        }

        [Test]
        public void ProductionWoodlandDifficultyChangesDecisionCadenceWithoutChangingAthlete()
        {
            (long routeTick,long tactical,long effort) RunAi(bool developing)
            {
                string Select(string id)=>developing&&id=="ai_difficulty_expert_v1.json"?
                    Read("Biathlon/ai_difficulty_developing_v1"):Read(id);
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
                var runner=ProductionVenueRaceFactory.Create(venue,
                    CompetitionProfile.FromJson(Read(Root+"sprint_profile")),
                    RaceRoutePlan.FromJson(Read(Root+"sprint_route")),
                    RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),Select,
                    new BalanceEventAudit(5),new AiDecisionCounts(),count:1,seed:177);
                long effort=0;
                while(runner.Field.Tick.Value<12000)
                {
                    runner.Step();
                    effort+=runner.GetLastInput(0).Ski.Effort.RequestedEffort;
                    var point=runner.Field.GetPhysicalPoint(0);
                    if(point.LoopId=="main"&&point.LocalNanometers>=1000*SkiState.NanometersPerMeter)
                    {
                        Assert.That(runner.Field.GetAthlete(0).ActiveShooting,Is.Null);
                        return(runner.Field.Tick.Value,runner.TacticalDecisions,effort);
                    }
                }
                Assert.Fail("Production AI did not reach the first-lap descent under the selected difficulty.");
                return default;
            }
            var expert=RunAi(false);var developing=RunAi(true);
            TestContext.WriteLine("wp177_ai_expert_ticks="+expert.routeTick+
                " tactical="+expert.tactical+" total_requested_effort="+expert.effort);
            TestContext.WriteLine("wp177_ai_developing_ticks="+developing.routeTick+
                " tactical="+developing.tactical+" total_requested_effort="+developing.effort);
            Assert.That(expert.tactical,Is.GreaterThan(developing.tactical),
                "Selected production difficulty must change AI decision cadence on Woodland.");
        }
    }
}

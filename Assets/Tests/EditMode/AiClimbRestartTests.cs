using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class AiClimbRestartTests
    {
        private static AiClimbRestartController Controller()=>new AiClimbRestartController(ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")).ClimbRestart,20);
        internal static CareerPreparedRace Witness()
        {
            var save=CareerSave.FromJson(PhysiologyContractTests.ProjectFile("Assets/Tests/Fixtures/M16/second_season_mass_v1.json"));
            var d=CareerDefinitions.ForSave(PhysiologyContractTests.DefinitionJson,save);
            return new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,d,n=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json")).Prepare(save,d.Calendar.AtWeek(save.Week),17667951796587433799UL);
        }
        [Test] public void ActualWeakRivalNeedsMoreThanHistoricalPaceAndRestartPaysOrdinaryReserve()
        {
            var race=Witness();SprintAthleteSession actor=null;for(int i=0;i<race.Runner.Field.Count;i++)if(race.Runner.Field.GetAthlete(i).AthleteId.Value=="career_m16_soak_athlete_98")actor=race.Runner.Field.GetAthlete(i);
            Assert.That(actor,Is.Not.Null);
            var state=new PhysiologyState(188.600119957,.330331556,.893744561,0);var effort=new EffortSystem(actor.PhysiologyDefinition);
            var ordinary=effort.Evaluate(613,state,.99202228992);var restarted=effort.Evaluate(1000,state,.99202228992);
            // The retained frame reports interval power187.316636W; evaluating
            // its observed end-tick fatigue for the next command gives this value.
            Assert.That(ordinary.DeliveredPowerWatts,Is.EqualTo(187.31515459893242).Within(1e-6));
            var surface=new TrackSampler(race.Runner.Field.Track.GetLoop("main")).Sample(TrackDistance.FromMeters(179.458)).WithTravelGeometry(.10037949617391967,0);
            var technique=new SkiTechniqueSystem(SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json")));
            var stats=new SkiTechniqueStats(35,37);var forces=new SkiForceSystem(SkiPhysicsDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_physics_provisional_v1.json")),20);
            var oldMotion=forces.Step(default,technique.Evaluate(stats,surface,state.MuscularFatigue,ordinary.DeliveredPowerWatts).PropulsivePowerWatts,surface);
            var newMotion=forces.Step(default,technique.Evaluate(stats,surface,state.MuscularFatigue,restarted.DeliveredPowerWatts).PropulsivePowerWatts,surface);
            Assert.That(oldMotion.State.SpeedMetersPerSecond,Is.Zero,"The observed ordinary effort cannot overcome gravity, even without the adverse race snow.");
            Assert.That(newMotion.State.SpeedMetersPerSecond,Is.GreaterThan(0));
            var cost=new AnaerobicSystem(actor.PhysiologyDefinition,20).Step(state.AnaerobicReserve,restarted.DeliveredRelativeEffort,state.MuscularFatigue);
            Assert.That(cost.Value,Is.LessThan(state.AnaerobicReserve),"Restart must pay the same ordinary anaerobic resource cost.");
        }
        [Test] public void ObservedExhaustedUphillStallRequestsPaidRestartInsteadOfRepeatingInadequatePace()
        {
            var controller=Controller();for(int i=0;i<9;i++)Assert.That(controller.Step(true,.100379496,0,.330331556,613),Is.EqualTo(613));
            Assert.That(controller.Step(true,.100379496,0,.330331556,613),Is.EqualTo(1000));
            Assert.That(controller.Phase,Is.EqualTo(AiClimbRestartPhase.Restart));
        }
        [Test] public void RestartRecoversRealReserveBeforeTryingAgainAndUsesHysteresis()
        {
            var controller=Controller();for(int i=0;i<10;i++)controller.Step(true,.1,0,.1,613);
            Assert.That(controller.Phase,Is.EqualTo(AiClimbRestartPhase.Recover));Assert.That(controller.Step(true,.1,0,.64,613),Is.EqualTo(100));
            Assert.That(controller.Step(true,.1,0,.65,613),Is.EqualTo(1000));Assert.That(controller.Phase,Is.EqualTo(AiClimbRestartPhase.Restart));
            Assert.That(controller.Step(true,.1,1.9,.4,613),Is.EqualTo(1000));Assert.That(controller.Step(true,.1,2,.4,613),Is.EqualTo(613));
            Assert.That(controller.Phase,Is.EqualTo(AiClimbRestartPhase.Ordinary));
        }
        [Test] public void MovingFlatDescendingPrivateRangeOrPlayerIntentDoesNotAcquireRestart()
        {
            var controller=Controller();for(int i=0;i<100;i++)
            {
                Assert.That(controller.Step(true,.1,5,.5,750),Is.EqualTo(750));Assert.That(controller.Step(true,0,0,.5,450),Is.EqualTo(450));
                Assert.That(controller.Step(true,-.1,0,.5,450),Is.EqualTo(450));Assert.That(controller.Step(false,.1,0,.5,0),Is.Zero);
            }
            for(int i=0;i<10;i++)controller.Step(true,.1,0,.5,613);
            Assert.That(controller.Step(false,.1,0,.5,0),Is.Zero);Assert.That(controller.Phase,Is.EqualTo(AiClimbRestartPhase.Ordinary));
        }
        [Test] public void OptionalPolicyPreservesLegacyDefinitionsAndInvalidPolicyIsRejected()
        {
            string legacy=PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json");int property=legacy.IndexOf("\"climb_restart\"",System.StringComparison.Ordinal);int at=legacy.LastIndexOf(',',property);
            Assert.That(at,Is.GreaterThan(0));legacy=legacy.Substring(0,at)+"\n}";
            var old=ManualAiSteeringDefinition.FromJson(legacy);Assert.That(old.ClimbRestart,Is.Null);Assert.That(old.ToJson(),Does.Not.Contain("climb_restart"));
            string current=PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json");
            Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(current,"restart_above_reserve","0.01")));
            Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(current,"restart_effort","1001")));
        }
    }
}

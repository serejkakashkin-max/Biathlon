using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerFatigueTests
    {
        public static CareerFatigueDefinition Definition()=>CareerFatigueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_fatigue_v1.json"));
        [Test] public void RepeatedLoadCarriesThroughSaveAndRestRestoresReadiness()
        {
            var save=CareerFoundationTests.Create();var a=save.Athlete(0);var d=Definition();var intensity=CareerTrainingTests.Definition().Get(CareerTrainingKind.Intensity);
            for(int week=0;week<4;week++){for(int i=0;i<3;i++)CareerFatigue.Training(a,intensity);CareerFatigue.Rest(a,d,0);}
            Assert.That(a.Fatigue,Is.GreaterThan(5000));Assert.That(CareerSave.FromJson(save.ToJson()).Athlete(0).Fatigue,Is.EqualTo(a.Fatigue));
            int before=a.Fatigue;CareerFatigue.Race(a,d,1200000);Assert.That(a.Fatigue,Is.GreaterThanOrEqualTo(before));
            for(int week=0;week<4;week++)CareerFatigue.Rest(a,d,3);Assert.That(a.Fatigue,Is.Zero);Assert.That(a.TrainingSessions,Is.EqualTo(12));
        }
        [Test] public void RecoverySessionReducesLoadAndInvalidRaceDoesNotMutate()
        {
            var a=CareerFoundationTests.Create().Athlete(0);var d=Definition();var training=CareerTrainingTests.Definition();CareerFatigue.Training(a,training.Get(CareerTrainingKind.Intensity));
            int before=a.Fatigue;CareerFatigue.Training(a,training.Get(CareerTrainingKind.Recovery));Assert.That(a.Fatigue,Is.LessThan(before));
            string json=a.ToJson();Assert.Catch(()=>CareerFatigue.Race(a,d,-1));Assert.Catch(()=>CareerFatigue.Rest(a,d,15));Assert.That(a.ToJson(),Is.EqualTo(json));
            var rested=CareerFoundationTests.Create().Athlete(0);var tired=CareerFoundationTests.Create().Athlete(0);CareerFatigue.Race(tired,d,1200000);
            var session=training.Get(CareerTrainingKind.Endurance);var adapt=CareerAdaptationTests.Definition();
            Assert.That(CareerAdaptation.Apply(tired,session,adapt,10000).TotalGain,Is.LessThan(CareerAdaptation.Apply(rested,session,adapt,10000).TotalGain));
        }
    }
}

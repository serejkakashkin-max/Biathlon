using Biathlon.Career;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerAiTrainingTests
    {
        public static CareerTrainingPolicy Policy()=>CareerTrainingPolicy.FromJson(PhysiologyContractTests.DefinitionJson("career_training_policy_v1.json"));
        private static int Apply(CareerAthlete a,CareerWeekPlan plan)=>CareerTrainingWeek.Apply(a,plan,CareerTrainingTests.Definition(),CareerAdaptationTests.Definition(),CareerFatigueTests.Definition(),CareerFormTests.Definition(),CareerMentalTests.Definition(),CareerHealthTests.Definition(),10000,false);
        [Test] public void AiPlansProgressThroughTheSameFunctionsAsCopiedPlayerPlans()
        {
            var a=CareerFoundationTests.Create().Athlete(1);var b=CareerFoundationTests.Create().Athlete(1);var training=CareerTrainingTests.Definition();var calendar=CareerFoundationTests.Calendar();
            int total=0;
            for(int week=1;week<=52;week++)
            {
                var ai=CareerAiTraining.Plan(a,training,calendar,Policy(),week);var choices=new CareerTrainingKind[ai.Count];for(int i=0;i<choices.Length;i++)choices[i]=ai.Get(i);
                var player=new CareerWeekPlan(training,calendar,week,choices);total+=Apply(a,ai);Apply(b,player);
                CareerFatigue.Rest(a,CareerFatigueTests.Definition(),calendar.TrainingSlots(week)-ai.UsedSlots);CareerFatigue.Rest(b,CareerFatigueTests.Definition(),calendar.TrainingSlots(week)-player.UsedSlots);
                Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));
            }
            Assert.That(total,Is.GreaterThan(0));Assert.That(a.TrainingSessions,Is.GreaterThan(0));
        }
        [Test] public void HighFatigueSelectsRecoveryWithinCalendarBudget()
        {
            var a=CareerFoundationTests.Create().Athlete(1);var training=CareerTrainingTests.Definition();for(int i=0;i<5;i++)CareerFatigue.Training(a,training.Get(CareerTrainingKind.Intensity));
            var plan=CareerAiTraining.Plan(a,training,CareerFoundationTests.Calendar(),Policy(),8);Assert.That(plan.Count,Is.EqualTo(1));Assert.That(plan.Get(0),Is.EqualTo(CareerTrainingKind.Recovery));
        }
    }
}

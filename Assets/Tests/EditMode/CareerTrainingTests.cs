using System;
using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerTrainingTests
    {
        public static CareerTrainingDefinition Definition()=>CareerTrainingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_training_v1.json"));
        [Test] public void SixSessionTypesHaveExplicitAdaptationsCostsAndRecovery()
        {
            var d=Definition();Assert.That(CareerTrainingDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
            foreach(CareerTrainingKind kind in Enum.GetValues(typeof(CareerTrainingKind)))
            {
                var session=d.Get(kind);Assert.That(session.TimeSlots,Is.GreaterThan(0));
                if(kind!=CareerTrainingKind.Recovery){Assert.That(session.FatigueCost,Is.GreaterThan(0));Assert.That(session.RecoveryNeed,Is.GreaterThan(0));}
            }
            Assert.That(d.Get(CareerTrainingKind.Prone).Gain(CareerStat.ProneStability),Is.GreaterThan(0));
            Assert.That(d.Get(CareerTrainingKind.Prone).Gain(CareerStat.StandingStability),Is.Zero);
            Assert.That(d.Get(CareerTrainingKind.Recovery).RecoveryGain,Is.GreaterThan(0));
        }
        [Test] public void PlansEnforceRaceWeekTimeAndOwnTheirSessionList()
        {
            var list=new[]{CareerTrainingKind.Endurance,CareerTrainingKind.Prone,CareerTrainingKind.Recovery};
            var plan=new CareerWeekPlan(Definition(),CareerFoundationTests.Calendar(),1,list);list[0]=CareerTrainingKind.Intensity;
            Assert.That(plan.Get(0),Is.EqualTo(CareerTrainingKind.Endurance));Assert.That(plan.UsedSlots,Is.EqualTo(3));
            Assert.Catch(()=>new CareerWeekPlan(Definition(),CareerFoundationTests.Calendar(),8,list));
            Assert.That(new CareerWeekPlan(Definition(),CareerFoundationTests.Calendar(),8,Array.Empty<CareerTrainingKind>()).Count,Is.Zero);
            Assert.Catch(()=>new CareerWeekPlan(Definition(),CareerFoundationTests.Calendar(),1,new[]{(CareerTrainingKind)99}));
            Assert.Catch(()=>CareerTrainingDefinition.FromJson(Definition().ToJson().Replace("\"kind\":1","\"kind\":0")));
        }
    }
}

using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualProductionFieldTests
    {
        internal static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) Initial(string format,int count)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_profile"));
            var events=new RaceEventLog(100000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json")),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),count:count);
            var stepper=ManualProductionFieldComposition.Create(runner.Field,venue,PhysiologyContractTests.DefinitionJson);
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")));
            return(runner,stepper,events);
        }
        [TestCase("mass_start",30)][TestCase("sprint",40)]
        public void ProductionSizedSignedFieldFinishesEveryAthleteWithoutLegacyFallback(string format,int count)
        {
            var f=Initial(format,count);var field=f.runner.Field;
            while(!field.IsComplete&&field.Tick.Value<40000)
            {
                try{f.runner.Step();}
                catch(Exception e){Assert.Fail("format="+format+" actors="+count+" tick="+field.Tick.Next().Value+"; "+ManualScopedFieldStepperTests.Explain(f.stepper)+"; "+e);}
                if(field.Tick.Value%1000==0)TestContext.WriteLine(format+" actors="+count+" accepted_tick="+field.Tick.Value);
            }
            Assert.That(field.IsComplete,Is.True);
            Assert.That(f.stepper.World.Tick,Is.EqualTo(field.Tick));
            Assert.That(f.runner.UsesManualScopedCandidate,Is.True);
            for(int i=0;i<count;i++)
            {
                Assert.That(field.GetAthlete(i).HasFinished,Is.True);
                Assert.That(field.GetAthlete(i).Lifecycle.CompletedBouts,Is.EqualTo(field.Profile.BoutCount));
                Assert.That(f.stepper.World.GetCatalog(i).Get(f.stepper.World.GetCursor(i).OccurrenceIndex).Kind,Is.EqualTo(ManualRacePathKind.FinishRunout));
            }
            TestContext.WriteLine(format+" complete actors="+count+" tick="+field.Tick.Value);
        }
    }
}

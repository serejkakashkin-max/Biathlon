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
        internal static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) Initial(string format,int count,PursuitQualificationSnapshot qualification=null,string snowProfile="packed_neutral")
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ManualScopedFieldStepperTests.Read(root+"manifest")),ManualScopedFieldStepperTests.Read);
            var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_profile"));
            var events=new RaceEventLog(100000);
            var runner=ProductionVenueRaceFactory.Create(venue,profile,RaceRoutePlan.FromJson(ManualScopedFieldStepperTests.Read(root+format+"_route")),
                RaceSnowConditions.FromJson(PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json").Replace("packed_neutral",snowProfile)),PhysiologyContractTests.DefinitionJson,
                events,new AiDecisionCounts(),qualification,count:count);
            var stepper=ManualProductionFieldComposition.Create(runner.Field,venue,PhysiologyContractTests.DefinitionJson);
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")));
            return(runner,stepper,events);
        }
        [TestCase("packed_neutral")][TestCase("fresh_soft")][TestCase("icy_fast")][TestCase("wet_slow")]
        public void ManualProductionSnowConditionsKeepTheSharedClockAndActualProgress(string snowProfile)
        {
            var f=Initial("mass_start",1,snowProfile:snowProfile);
            for(int t=0;t<1200;t++)f.runner.Step();
            Assert.That(f.runner.Field.Tick.Value,Is.EqualTo(1200));
            Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
            Assert.That(f.runner.UsesManualScopedCandidate,Is.True);
            Assert.That(f.runner.Field.GetAthlete(0).RouteNanometers,Is.GreaterThan(200*SkiState.NanometersPerMeter));
            TestContext.WriteLine(snowProfile+" actual_route_m="+f.runner.Field.GetAthlete(0).RouteNanometers/(double)SkiState.NanometersPerMeter);
        }
        [TestCase("mass_start",30)][TestCase("sprint",30)]
        public void ProductionSizedSignedFieldFinishesEveryAthleteWithoutLegacyFallback(string format,int count)
        {
            var f=Initial(format,count);var field=f.runner.Field;
            Complete(f,format,count);
            if(format=="sprint")
            {
                var profile=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/Venues/woodland_basin_v1/pursuit_profile"));
                var qualification=PursuitQualificationSnapshot.Create("woodland_manual_sprint_to_pursuit_v1",field.Result,profile);
#if !UNITY_EDITOR
                System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"biathlon-m13-actual-pursuit-qualification.json"),qualification.ToJson());
#endif
                f=default;field=null;
                GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
                Complete(Initial("pursuit",count,qualification),"pursuit",count);
            }
        }
        private static void Complete((PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) f,string format,int count)
        {
            var field=f.runner.Field;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!field.IsComplete&&field.Tick.Value<50000)
            {
                try{f.runner.Step();}
                catch(Exception e){Assert.Fail("format="+format+" actors="+count+" tick="+field.Tick.Next().Value+"; "+ManualScopedFieldStepperTests.Explain(f.stepper)+"; "+e);}
                if(field.Tick.Value%1000==0)TestContext.WriteLine(format+" actors="+count+" accepted_tick="+field.Tick.Value);
#if !UNITY_EDITOR
                if(field.Tick.Value%1000==0)
                {
                    int finished=0,slowest=0;long lowest=long.MaxValue;
                    for(int a=0;a<count;a++){if(field.GetAthlete(a).HasFinished)finished++;if(field.GetAthlete(a).RouteNanometers<lowest){lowest=field.GetAthlete(a).RouteNanometers;slowest=a;}}
                    var body=f.stepper.World.GetActor(slowest);var cursor=f.stepper.World.GetCursor(slowest);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"biathlon-m13-"+format+"-progress.txt"),
                        $"tick={field.Tick.Value} finished={finished}/{count} lowest_route_m={lowest/(double)SkiState.NanometersPerMeter:R} pieces={f.stepper.LastStep.Intervals} actor={slowest} loop={body.Observation.LoopId} arc={cursor.ObservedArcNanometers} phase={field.GetAthlete(slowest).Lifecycle.Phase} v=({body.Velocity.X:R},{body.Velocity.Z:R}) yaw={body.Pose.HeadingRadians:R} lat={body.Observation.LateralMeters:R}");
                }
#endif
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
            TestContext.WriteLine("wall_seconds="+timer.Elapsed.TotalSeconds+" realtime_factor="+(field.Tick.Value/(double)field.TickFrequency/timer.Elapsed.TotalSeconds));
        }
    }
}

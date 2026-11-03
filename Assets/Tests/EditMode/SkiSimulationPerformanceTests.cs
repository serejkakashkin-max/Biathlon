using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class SkiSimulationPerformanceTests
    {
        [TestCase(false),TestCase(true),Category("Performance")]
        public void ProductionThirtyAthleteSkiSimulationProfile(bool manual)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            Func<string,string> read=OwnerM13ExitQueueTests.Read;
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(read(root+"manifest")),read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(read(root+"mass_start_profile")),RaceRoutePlan.FromJson(read(root+"mass_start_route")),RaceSnowConditions.FromJson(read("Biathlon/race_snow_neutral_v1")),PhysiologyContractTests.DefinitionJson,new BalanceEventAudit(5),new AiDecisionCounts(),count:30,seed:123);
            var stepper=ManualProductionFieldComposition.Create(runner.Field,venue,PhysiologyContractTests.DefinitionJson);
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),completeSnowSearch:true);
            var rows=new double[2400,3];double ms=1000d/Stopwatch.Frequency;
            ulong stateHash=14695981039346656037UL;
            for(int t=0;t<2400;t++)
            {
                long bytes=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();
                if(manual){var tick=runner.Field.Tick.Next();var id=runner.Field.GetAthlete(0).AthleteId;runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,700),1,1,0),new WeaponAimCommand(tick,id,0,0)),0);}else runner.Step();
                rows[t,0]=(Stopwatch.GetTimestamp()-start)*ms;rows[t,1]=GC.GetAllocatedBytesForCurrentThread()-bytes;
                rows[t,2]=runner.Field.GetAthlete(0).RouteNanometers/(double)SkiState.NanometersPerMeter;
                for(int i=0;i<30;i++)
                {
                    var a=stepper.World.GetActor(i);var c=stepper.World.GetCursor(i);var race=runner.Field.GetAthlete(i);
                    Add(BitConverter.DoubleToInt64Bits(a.Pose.X));Add(BitConverter.DoubleToInt64Bits(a.Pose.Z));Add(a.Pose.HeadingNanoradians);Add(BitConverter.DoubleToInt64Bits(a.Pose.YawRateRadiansPerSecond));
                    Add(BitConverter.DoubleToInt64Bits(a.Velocity.X));Add(BitConverter.DoubleToInt64Bits(a.Velocity.Z));Add(c.OccurrenceIndex);Add(c.ObservedArcNanometers);Add(c.CreditedArcNanometers);Add(race.RouteNanometers);
                    Add(BitConverter.DoubleToInt64Bits(race.PhysiologyState.AnaerobicReserve));Add(BitConverter.DoubleToInt64Bits(race.PhysiologyState.MuscularFatigue));Add(runner.GetLastManualSteering(i));Add(BitConverter.DoubleToInt64Bits(runner.GetLastManualBrake(i)));
                }
            }
            var output=new StringBuilder("tick,total_ms,alloc_bytes,route_m\n");
            for(int t=0;t<2400;t++){output.Append(t+1);for(int c=0;c<3;c++)output.Append(',').Append(rows[t,c].ToString("R",CultureInfo.InvariantCulture));output.Append('\n');}
            var project=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(project!=null&&!Directory.Exists(Path.Combine(project.FullName,"Assets/Resources/Biathlon")))project=project.Parent;
            string folder=Path.Combine(project.FullName,"artifacts/m16-6/performance");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,manual?"simulation-manual.csv":"simulation-auto.csv"),output.ToString());
            File.WriteAllText(Path.Combine(folder,manual?"simulation-manual-hash.txt":"simulation-auto-hash.txt"),stateHash.ToString("x16",CultureInfo.InvariantCulture));
            Assert.That(runner.Field.Tick.Value,Is.EqualTo(2400));if(!manual)Assert.That(rows[2399,2],Is.GreaterThan(100));
#if !UNITY_EDITOR
            Assert.That(stateHash.ToString("x16",CultureInfo.InvariantCulture),Is.EqualTo(manual?"f8169cb1f5e027d1":"1889c670a7c3587a"),"Preserve every sampled physical state, cursor, physiology and AI command from before the optimization.");
#endif
            void Add(long value){unchecked{stateHash^=(ulong)value;stateHash*=1099511628211UL;}}
        }
    }
}

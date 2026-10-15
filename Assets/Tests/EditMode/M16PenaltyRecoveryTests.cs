using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class M16PenaltyRecoveryTests
    {
        private const ulong RaceSeed=17150764420261043623UL;
        private const string Fixture="dotnet/Biathlon.Tests/Fixtures/m16_penalty_yaw/prepared-save.json";
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        private static string Venue(string name)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+name+".json");
        private static CareerSave Save()=>CareerSave.FromJson(PhysiologyContractTests.ProjectFile(Fixture));

        // Observation entry point for qualifying a runtime's historical policy.
        // It is deliberately not a passing test and supplies no fallback result.
        // The root calls it off the editor thread, retaining the exact receipt
        // before writing a runtime-specific assertion below.
        public static string CaptureHistoricalRuntimeTrajectory(string projectRoot)
        {
            string ReadFile(string name)
            {
                string json=System.IO.File.ReadAllText(System.IO.Path.Combine(projectRoot,"Assets/Resources/Biathlon",name));
                return name=="manual_ai_steering_v2.json"?PhysiologyContractTests.ReplaceNumber(json,"penalty_yaw_tracking_error_radps","0"):json;
            }
            string VenueFile(string name)=>System.IO.File.ReadAllText(System.IO.Path.Combine(projectRoot,"Assets/Resources",name+".json"));
            var save=CareerSave.FromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(projectRoot,Fixture)));
            var definitions=CareerDefinitions.ForSave(ReadFile,save);
            var prepared=new CareerRaceProvider(ReadFile,definitions,VenueFile).Prepare(save,definitions.Calendar.AtWeek(save.Week),RaceSeed);
            var runner=prepared.Runner;
            var movement=ManualProductionFieldComposition.Create(runner.Field,prepared.Venue,prepared.ManualRead,prepared.CourseReactionPolicy);
            runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(ReadFile("manual_ai_steering_v2.json")),
                ManualSkiSteeringDefinition.FromJson(ReadFile("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(ReadFile("player_course_assistance_v1.json")));
            var receipt=new HistoricalRuntimeReceipt();
            try
            {
                while(!runner.Field.IsComplete&&runner.Field.Tick.Value<200000)runner.Step();
                receipt.Outcome=runner.Field.IsComplete?"Completed":"BudgetReached";
                if(runner.Field.IsComplete)receipt.AuthoritySha256=prepared.CollectResult().AuthoritySha256;
            }
            catch(InvalidOperationException)
            {
                receipt.Outcome="Rejected";
                receipt.StepStatus=movement.LastStep.Status.ToString();
                receipt.WorldStatus=movement.LastStep.WorldAttempt.Status.ToString();
                receipt.CourseStatus=movement.LastStep.WorldAttempt.Course.Status.ToString();
            }
            receipt.AcceptedTick=runner.Field.Tick.Value;receipt.WorldTick=movement.World.Tick.Value;
            using(var stream=new System.IO.MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(HistoricalRuntimeReceipt)).WriteObject(stream,receipt);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        [System.Runtime.Serialization.DataContract]
        public sealed class HistoricalRuntimeReceipt
        {
            [System.Runtime.Serialization.DataMember(Name="schema_version")] public int SchemaVersion=1;
            [System.Runtime.Serialization.DataMember(Name="id")] public string Id="m16_historical_yaw_runtime_observation_v1";
            [System.Runtime.Serialization.DataMember(Name="outcome")] public string Outcome;
            [System.Runtime.Serialization.DataMember(Name="accepted_tick")] public long AcceptedTick;
            [System.Runtime.Serialization.DataMember(Name="world_tick")] public long WorldTick;
            [System.Runtime.Serialization.DataMember(Name="authority_sha256")] public string AuthoritySha256;
            [System.Runtime.Serialization.DataMember(Name="step_status")] public string StepStatus;
            [System.Runtime.Serialization.DataMember(Name="world_status")] public string WorldStatus;
            [System.Runtime.Serialization.DataMember(Name="course_status")] public string CourseStatus;
        }

        [Test]
        public void PenaltyYawTrackingIsOptionalValidatedDataAndRetainsHistoricalPolicyEncoding()
        {
            string json=Read("manual_ai_steering_v2.json");
            var current=ManualAiSteeringDefinition.FromJson(json);
            Assert.That(current.PenaltyYawTrackingError,Is.EqualTo(.35));
            Assert.That(ManualAiSteeringDefinition.FromJson(current.ToJson()).PenaltyYawTrackingError,Is.EqualTo(.35));
            var historical=ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"penalty_yaw_tracking_error_radps","0"));
            Assert.That(historical.ToJson(),Does.Not.Contain("penalty_yaw_tracking_error_radps"));
            Assert.That(ManualAiSteeringDefinition.FromJson(historical.ToJson()).PenaltyYawTrackingError,Is.Zero);
            foreach(string invalid in new[]{"-0.1","10.01","1e309"})
                Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"penalty_yaw_tracking_error_radps",invalid)));
        }

        [Test]
        public void CapturedSecondSeasonPursuitClearsPenaltyAndFinishesTwiceWithIdenticalAuthority()
        {
            var save=Save();string source=save.ToJson();
            var definitions=CareerDefinitions.ForSave(Read,save);
            var calendar=definitions.Calendar.AtWeek(save.Week);
            Assert.That(calendar.Id,Is.EqualTo("junior_open_pursuit"));
            var first=new CareerRaceProvider(Read,definitions,Venue);
            var second=new CareerRaceProvider(Read,definitions,Venue);
            var a=first.Run(save,calendar,RaceSeed);
            var b=second.Run(Save(),calendar,RaceSeed);
            Assert.That(first.LastTicks,Is.LessThan(30000));
            Assert.That(second.LastTicks,Is.EqualTo(first.LastTicks));
            Assert.That(b.AuthoritySha256,Is.EqualTo(a.AuthoritySha256));
            Assert.That(b.AuthorityResultJson,Is.EqualTo(a.AuthorityResultJson));
            Assert.That(second.LastEventHash,Is.EqualTo(first.LastEventHash));
            Assert.That(second.LastShots,Is.EqualTo(first.LastShots));
            Assert.That(save.ToJson(),Is.EqualTo(source),"Running a race cannot mutate its prepared career snapshot.");
            TestContext.WriteLine("captured_pursuit_ticks="+first.LastTicks+" authority_sha256="+a.AuthoritySha256+" event_hash="+first.LastEventHash);
        }

        [Test]
        public void CapturedFirstSeasonMassStartRetainsItsOrdinaryRangeArrival()
        {
            var save=CareerSave.FromJson(PhysiologyContractTests.ProjectFile("dotnet/Biathlon.Tests/Fixtures/m16_penalty_yaw/first-mass-prepared-save.json"));
            string source=save.ToJson();
            var definitions=CareerDefinitions.ForSave(Read,save);
            var calendar=definitions.Calendar.AtWeek(save.Week);
            Assert.That(calendar.Id,Is.EqualTo("junior_first_mass"));
            var provider=new CareerRaceProvider(Read,definitions,Venue);
            var result=provider.Run(save,calendar,3246096777024755018UL);
            var repeated=new CareerRaceProvider(Read,definitions,Venue);
            var again=repeated.Run(CareerSave.FromJson(source),calendar,3246096777024755018UL);
            Assert.That(repeated.LastTicks,Is.EqualTo(provider.LastTicks),"The exact prepared race must repeat within the current simulation runtime.");
            Assert.That(again.AuthorityResultJson,Is.EqualTo(result.AuthorityResultJson));
            Assert.That(again.AuthoritySha256,Is.EqualTo(result.AuthoritySha256));
            Assert.That(repeated.LastEventHash,Is.EqualTo(provider.LastEventHash));
            Assert.That(repeated.LastShots,Is.EqualTo(provider.LastShots));
            Assert.That(save.ToJson(),Is.EqualTo(source));
            Assert.That(result.AuthorityResultJson,Is.Not.Empty);
            TestContext.WriteLine("captured_first_mass_ticks="+provider.LastTicks+" authority_sha256="+result.AuthoritySha256);
        }

        [Test]
        public void CapturedOutsidePenaltySourceRejectsCompleteBodyAndNeverPaysClock()
        {
            var f=new ManualRacePathCatalogTests.Fixture("pursuit",25);var catalog=f.Catalog;
            var policy=ManualNumericalToleranceDefinition.FromJson(Read("manual_gameplay_tolerance_v1.json"));
            typeof(ManualRacePathCatalog).GetMethod("BindPortalTolerance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .Invoke(catalog,new object[]{policy.PortalPositionTolerance,policy.UseRoundSnowCorridor,policy.ConnectedSnowWindowMultiplier,policy.OrdinaryPortalRetreat,policy.OrdinaryHandoffWindow});
            int occurrence=catalog.GetPenaltyOccurrence(0,1);var row=catalog.Get(occurrence);
            var id=new AthleteId("captured_outside_penalty_source");
            // CoreCLR's accepted tick15576 witness is used as an explicit
            // invalid-source geometry fixture on every runtime. Native Mono
            // follows a different full-race trajectory; it need not visit this pose.
            var pose=new ManualSkiHeadingState(64.173374212,-18.420337801,3.624016864);
            Assert.That(catalog.TryProject(pose,occurrence,0,0,out var observation),Is.True);
            var cursor=new ManualRacePathProgression(catalog).Reconstitute(id,occurrence,0,26944494283L,row.RouteStartNanometers);
            var world=new ManualScopedWorldRoster(new[]{catalog},f.Venue.Occupancy,ManualScopedFieldStepperTests.Contact(),
                ManualScopedFieldStepperTests.Work(),new[]{cursor},new[]{pose},new[]{default(SkiPlanarVelocity)},new[]{new SkiState(123,0)},20,
                numericalTolerance:policy);
            var before=world.GetActor(0);var motion=world.GetPhysicalMotion(0);
            var raw=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.Venue.Occupancy,20).PreviewTrajectory(
                new ManualSkiContactBody(id,pose,default,75),default,0,0,.05));
            var candidates=new[]{new SkiWorldMotionCandidate(id,raw)};
            var windows=new[]{new ManualScopedWorldWindow(id,occurrence,0,1,row.LengthNanometers)};
            Assert.That(world.TryPrepare(candidates,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{0d},out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.Course));
            Assert.That(world.LastAttempt.Course.Status,Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
            Assert.That(world.GetRibbonDiagnostics(0).LastStartClearance,Is.LessThan(-.2),"The captured whole-body failure must remain a real exclusion, not a tolerance pass.");
            Assert.That(world.Tick.Value,Is.Zero);
            Assert.That(world.GetActor(0),Is.EqualTo(before));
            Assert.That(world.GetCursor(0),Is.EqualTo(cursor));
            Assert.That(world.GetPhysicalMotion(0),Is.EqualTo(motion));
            Assert.Throws<ArgumentException>(()=>world.Commit(rejected));
        }

        [Test]
        public void HistoricalYawPolicyRetainsItsRecordedRuntimeOutcome()
        {
            var directory=new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(directory!=null&&!System.IO.File.Exists(System.IO.Path.Combine(directory.FullName,Fixture)))directory=directory.Parent;
            Assert.That(directory,Is.Not.Null,"The pinned prepared career must be available.");
            string json=CaptureHistoricalRuntimeTrajectory(directory.FullName);
            HistoricalRuntimeReceipt receipt;
            using(var stream=new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                receipt=(HistoricalRuntimeReceipt)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(HistoricalRuntimeReceipt)).ReadObject(stream);
            Assert.That(receipt.SchemaVersion,Is.EqualTo(1));
            Assert.That(receipt.Id,Is.EqualTo("m16_historical_yaw_runtime_observation_v1"));
            Assert.That(receipt.WorldTick,Is.EqualTo(receipt.AcceptedTick));
#if UNITY_EDITOR
            // Independent Mono observation is retained in the quality receipt.
            // Pin its real completion, not a CoreCLR trajectory it does not visit.
            Assert.That(receipt.Outcome,Is.EqualTo("Completed"));
            Assert.That(receipt.AcceptedTick,Is.EqualTo(22876));
            Assert.That(receipt.AuthoritySha256,Is.EqualTo("615446a0df90459ddf637aca89909ba79fe538de91449e6a359be1b274096a89"));
            Assert.That(receipt.StepStatus,Is.Null);
            Assert.That(receipt.WorldStatus,Is.Null);
            Assert.That(receipt.CourseStatus,Is.Null);
#else
            // The original captured CoreCLR control remains a strict no-clock
            // rejection oracle for precisely the historical failing trajectory.
            Assert.That(receipt.Outcome,Is.EqualTo("Rejected"));
            Assert.That(receipt.AcceptedTick,Is.EqualTo(15576));
            Assert.That(receipt.StepStatus,Is.EqualTo("WorldRejected"));
            Assert.That(receipt.WorldStatus,Is.EqualTo("Course"));
            Assert.That(receipt.CourseStatus,Is.EqualTo("UnresolvedStart"));
            Assert.That(receipt.AuthoritySha256,Is.Null);
#endif
            TestContext.WriteLine("historical_runtime_receipt="+json);
        }
    }
}

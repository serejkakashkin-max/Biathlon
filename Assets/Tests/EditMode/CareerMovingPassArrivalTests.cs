#if !UNITY_EDITOR
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerMovingPassArrivalTests
    {
        [DataContract] private sealed class Case
        {
            [DataMember] public string snapshot_json {get;set;}
            [DataMember] public string event_id {get;set;}
            [DataMember] public ulong seed {get;set;}
        }
        // Captured before the first Mass Start in the original sixteen-event
        // season test, after real Sprint/Pursuit results and weekly development.
        // v3 failed at tick11770, athlete21, mat30: lateral arrival0.915m outside
        // the existing0.8m plant circle. No sporting result is fabricated here.
        [TestCase("first_mass_moving_v3.json")][TestCase("second_mass_moving_v4.json")]
        [Timeout(900000)] public void CurrentCareerMassAdmitsOnlyStationaryFiringArrivals(string fixture)
        {
            string json=PhysiologyContractTests.ProjectFile("Assets/Tests/Fixtures/M16/"+fixture);
            Case value;
            using(var input=new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
                value=(Case)new DataContractJsonSerializer(typeof(Case)).ReadObject(input);
            var definitions=new CareerDefinitions(PhysiologyContractTests.DefinitionJson,"woodland_career_calendar_v1.json");
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,definitions,Read);
            CareerCalendarEvent race=null;
            for(int i=0;i<definitions.Calendar.Count;i++)
                if(definitions.Calendar.Get(i).Id==value.event_id)race=definitions.Calendar.Get(i);
            Assert.That(race,Is.Not.Null);
            var prepared=provider.Prepare(CareerSave.FromJson(value.snapshot_json),race,value.seed);
            Assert.That(prepared.MovingOvertakeVersion,Is.EqualTo(2));
            var runner=prepared.Runner;
            var movement=ManualProductionFieldComposition.Create(runner.Field,prepared.Venue,prepared.ManualRead,prepared.CourseReactionPolicy);
            runner.BindManualScopedCandidate(movement,
                ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson(ManualAiSteeringDefinition.ResourceForVersion(prepared.MovingOvertakeVersion))),
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),
                PlayerCourseAssistanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("player_course_assistance_v1.json")),true);
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<200000)runner.Step();
            Assert.That(runner.Field.IsComplete,Is.True);
            Assert.That(prepared.CollectResult().Count,Is.EqualTo(runner.Field.Count));
            TestContext.WriteLine("first_mass_complete_tick="+runner.Field.Tick.Value);
        }
    }
}
#endif

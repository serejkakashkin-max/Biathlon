using System.Reflection;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class M166TrafficVersionTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        [TestCase(0)][TestCase(1)][TestCase(2)]
        public void StartProofUnionCapturesContactGeometryWithoutAwardingPaidRouteScopeAndPreservesVersionOne(int version)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Read(root+"sprint_profile")),
                RaceRoutePlan.FromJson(Read(root+"sprint_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,new RaceEventLog(200000),new AiDecisionCounts(),count:30,
                completeStartCorridorProof:version!=0,startContinuationVersion:version);
            var field=runner.Field;var point=field.GetPhysicalPoint(0);
            var world=(PhysicalRangeWorldTrafficMap)typeof(PackSprintFieldSession).GetField("rangeWorldTraffic",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(field);
            Assert.That(field.StartContinuationVersion,Is.EqualTo(version));
            Assert.That(world.Contains(point),Is.EqualTo(version==2));
            Assert.That(field.HasPaidRouteWorldGeometry(point),Is.False,"Start contact geometry must not become paid race distance/bypass scope.");
            if(version==1)
            {
#if UNITY_EDITOR
                const string original="a448e76b075145fc2f8f3e8308620a461e8f4cfecf29afc944deb1670c924a8f";
#else
                const string original="b262bcadf0e1c03c35459a69f6262169f4c6841aa7a09ab6c5680f82b3a7a3a8";
#endif
                Assert.That(BalanceRegression.Hash(field.ContextJson),Is.EqualTo(original),"Captured before the union change on both runtimes.");
            }
        }
    }
}

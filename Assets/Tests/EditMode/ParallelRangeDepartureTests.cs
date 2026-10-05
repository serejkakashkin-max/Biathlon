using System;
using System.Reflection;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ParallelRangeDepartureTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static PackAiRaceRunner Create(bool enabled)
        {
            const string v="Biathlon/Venues/woodland_basin_v1/";
            string Resource(string id)
            {
                string json=Read(id);if(id==v+"shared_apron")
                {
                    json=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"parallel_post_shoot_departures\"\\s*:\\s*(true|false)","");
                    json=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"penalty_world_merge_junctions\"\\s*:\\s*\\[[^\\]]*\\]","");
                    json=json.Insert(json.IndexOf('{')+1,"\"parallel_post_shoot_departures\":"+(enabled?"true":"false")+",");
                }
                return json;
            }
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Resource(v+"manifest")),Resource);
            return ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Resource(v+"mass_start_profile")),
                RaceRoutePlan.FromJson(Resource(v+"mass_start_route")),RaceSnowConditions.FromJson(Resource("Biathlon/race_snow_neutral_v1")),
                name=>PhysiologyContractTests.DefinitionJson(name),new NullEvents(),new AiDecisionCounts(),count:6);
        }
        private sealed class NullEvents:IRaceEventSink {public void Record(RaceEvent value){}}
        private static PhysicalSharedApronMapV2 Map(PackAiRaceRunner runner)=>(PhysicalSharedApronMapV2)typeof(PackSprintFieldSession).GetField("sharedApron",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(runner.Field);
        private static PhysicalApronTraceV2 Departure(PackAiRaceRunner runner,int lane,string id,double line=0,double shift=0)
        {
            var map=Map(runner);var path=runner.Field.GetAthlete(0).GetRangePathChoice(lane);
            long from=path.FiringRouteNanometers(0)+(long)Math.Round(shift*SkiState.NanometersPerMeter),to=from+1000000;
            var trace=map.CreateTrace(lane,new AthleteId(id),4);trace.SetAllBodyHeadings(true);
            var segment=(RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment),BindingFlags.Instance|BindingFlags.NonPublic,null,
                new object[]{from,to,0L,to-from,0.0,.05,false},null);
            trace.Prepare(from,to,path.MapRoute(from),path.MapRoute(to),line,line,default,default,
                new[]{new PhysicalApronMotionV2(segment,trace.Projection.Project(segment,path.MapRoute(from)))});
            return trace;
        }
        [Test] public void NeighboringMatsCanDepartWithFullBodiesDespiteOverlappingLeaseGuards()
        {
            var runner=Create(true);var map=Map(runner);var scratch=new PhysicalApronPairScratchV2(4);
            Assert.That(map.DepartureBodiesConflict(Departure(runner,0,"a"),Departure(runner,1,"b"),scratch),Is.False);
            Assert.That(map.DepartureBodiesConflict(Departure(runner,1,"b"),Departure(runner,0,"a"),scratch),Is.False);
        }
        [Test] public void ContactAndForeignOrUnpreparedEvidenceRemainBlockedAndLegacyIsExplicit()
        {
            var runner=Create(true);var map=Map(runner);var scratch=new PhysicalApronPairScratchV2(4);
            Assert.That(map.DepartureBodiesConflict(Departure(runner,0,"a"),Departure(runner,0,"b"),scratch),Is.True);
            Assert.That(map.DepartureBodiesConflict(Departure(runner,0,"a"),map.CreateTrace(1,new AthleteId("b"),4),scratch),Is.True);
            var other=Create(true);Assert.That(map.DepartureBodiesConflict(Departure(runner,0,"a"),Departure(other,1,"b"),scratch),Is.True);
            var legacy=Create(false);Assert.That(Map(legacy).DepartureBodiesConflict(Departure(legacy,0,"a"),Departure(legacy,1,"b"),scratch),Is.True);
        }
    }
}

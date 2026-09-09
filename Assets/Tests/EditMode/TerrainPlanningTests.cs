using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class TerrainPlanningTests
    {
        public static TerrainPlanningDefinition Definition()=>TerrainPlanningDefinition.FromJson(PhysiologyContractTests.DefinitionJson("terrain_planning_provisional_v1.json"));
        public static AiRouteLookahead[] Ahead(string trackId, double offset=0)
        {
            var track=SyntheticTrackTests.Generated(trackId); var requirements=CompetitionProfileTests.TestProfile().ToRouteRequirements();
            var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("preview",track.Id,"main_long",requirements),requirements);
            var distance=TrackDistance.FromMeters(offset);
            return new[]{new AiRouteLookahead(distance,route.Sample(TrackDistance.FromMeters(100+offset)))};
        }
        [Test] public void UpcomingClimbSpendsReserveAndTurnReducesEffortBeforeArrival()
        {
            var planner=new TerrainPacePlanner(Definition()); var rested=new PhysiologyState(100,1,0,0);
            var near=planner.Evaluate(10,4,rested,Ahead("synthetic_climb_v1",20));
            Assert.That(near.PreviewMeters,Is.EqualTo(40)); Assert.That(near.ClimbDemand,Is.EqualTo(0.5));
            Assert.That(near.EffortAdjustment,Is.EqualTo(50));
            Assert.That(planner.Evaluate(10,4,new PhysiologyState(190,0,1,0),Ahead("synthetic_climb_v1",20)).EffortAdjustment,Is.Zero);
            Assert.That(planner.Evaluate(10,4,rested,Ahead("synthetic_climb_v1",40)).EffortAdjustment,Is.Zero);
            var turn=planner.Evaluate(10,4,rested,Ahead("synthetic_technical_descent_v1",20));
            Assert.That(turn.TurnDemand,Is.EqualTo(0.5)); Assert.That(turn.EffortAdjustment,Is.EqualTo(-100));
            Assert.That(planner.Evaluate(10,4,rested,Ahead("synthetic_flat_v1")).EffortAdjustment,Is.Zero);
            Assert.That(planner.Evaluate(0,4,rested,Ahead("synthetic_climb_v1")).PreviewMeters,Is.EqualTo(20));
        }
        [Test] public void InvalidPreviewAndSchemaFailAndWarmedPlanningDoesNotAllocate()
        {
            var definition=Definition(); Assert.That(TerrainPlanningDefinition.FromJson(definition.ToJson()).ToJson(),Is.EqualTo(definition.ToJson()));
            Assert.Catch(()=>TerrainPlanningDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(definition.ToJson(),"full_climb_grade","0")));
            var planner=new TerrainPacePlanner(definition); var state=new PhysiologyState(100,1,0,0); var ahead=Ahead("synthetic_climb_v1",20);
            Assert.Catch(()=>planner.Evaluate(double.NaN,4,state,ahead)); Assert.Catch(()=>planner.Evaluate(10,4,default,ahead));
            Assert.Catch(()=>planner.Evaluate(10,4,state,new AiRouteLookahead[1]));
            planner.Evaluate(10,4,state,ahead); long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++) planner.Evaluate(10,4,state,ahead);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

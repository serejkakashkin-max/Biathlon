using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class OvertakeDecisionTests
    {
        public static OvertakeDecisionDefinition Definition()=>OvertakeDecisionDefinition.FromJson(PhysiologyContractTests.DefinitionJson("overtake_decision_provisional_v1.json"));
        private static OvertakeDecisionEvaluator Evaluator()=>new OvertakeDecisionEvaluator(Definition(),OvertakeTests.Definition(),new OccupancySystem(OccupancyTests.Definition()));
        [Test] public void ReadyDecisionAndSharedControllerChooseSameTargetSideAndPayCosts()
        {
            var self=DraftingTests.Participant("self",10,speed:12); var rows=new[]{DraftingTests.Participant("b",15,speed:10)};
            var evaluator=Evaluator(); var decision=evaluator.Evaluate(self,rows,new PhysiologyState(150,1,0,0),1,6,true,0,0,TrackDistance.FromMeters(200));
            Assert.That(decision.Request,Is.True); Assert.That(decision.EstimatedPassSeconds,Is.GreaterThan(0));
            var log=new RaceEventLog(10); var controller=new OvertakeController(OvertakeTests.Definition(),new OccupancySystem(OccupancyTests.Definition()),self.AthleteId,20,log);
            var output=controller.Step(new SimulationInput(new SimulationTick(1),self.AthleteId,800),self,DraftingTests.Flat(),rows,decision.Request);
            Assert.That(output.TargetId,Is.EqualTo(decision.Opportunity.Target.AthleteId));
            Assert.That(Math.Sign(output.DesiredLateralMeters),Is.EqualTo(Math.Sign(decision.Opportunity.PassingLaneMeters)));
            Assert.That(output.EffectiveEffortControl,Is.GreaterThan(800)); Assert.That(output.PropulsionMultiplier,Is.LessThan(1));
            Assert.That(log[0].EventType,Is.EqualTo(RaceEventType.OvertakeStarted));
        }
        [Test] public void ResourcesValueTerrainSpaceSpeedAndPathCanEachDeclinePass()
        {
            var self=DraftingTests.Participant("self",10,speed:12); var target=DraftingTests.Participant("b",15,speed:10); var rows=new[]{target};
            var normal=new PhysiologyState(150,1,0,0); var evaluator=Evaluator();
            OvertakeDecisionFrame Run(PhysiologyState state,double value=1,double width=6,bool allowed=true,double grade=0,double curvature=0,double distance=200)=>
                evaluator.Evaluate(self,rows,state,value,width,allowed,grade,curvature,TrackDistance.FromMeters(distance));
            Assert.That(Run(new PhysiologyState(190,0.1,0,0)).Reason,Is.EqualTo(OvertakeDecisionReason.Energy));
            Assert.That(Run(new PhysiologyState(190,1,0.8,0)).Reason,Is.EqualTo(OvertakeDecisionReason.Energy));
            Assert.That(Run(normal,value:0.1).Reason,Is.EqualTo(OvertakeDecisionReason.RaceValue));
            Assert.That(Run(normal,allowed:false).Reason,Is.EqualTo(OvertakeDecisionReason.Terrain));
            Assert.That(Run(normal,grade:0.2).Reason,Is.EqualTo(OvertakeDecisionReason.Terrain));
            Assert.That(Run(normal,curvature:0.04).Reason,Is.EqualTo(OvertakeDecisionReason.Terrain));
            Assert.That(Run(normal,width:1.5).Reason,Is.EqualTo(OvertakeDecisionReason.Space));
            Assert.That(Run(normal,distance:5).Reason,Is.EqualTo(OvertakeDecisionReason.PathEnds));
            rows[0]=DraftingTests.Participant("b",15,speed:12);
            Assert.That(Run(normal).Reason,Is.EqualTo(OvertakeDecisionReason.ClosingSpeed));
            rows[0]=DraftingTests.Participant("b",15,speed:11.7);
            Assert.That(Run(normal).Reason,Is.EqualTo(OvertakeDecisionReason.Duration));
            rows[0]=DraftingTests.Participant("b",15,corridor:"other"); Assert.That(Run(normal).Reason,Is.EqualTo(OvertakeDecisionReason.NoTarget));
        }
        [Test] public void OccupiedSidesAndInvalidDataFailAndRepeatedQueriesDoNotAllocate()
        {
            var self=DraftingTests.Participant("self",10,speed:12); var target=DraftingTests.Participant("b",15,speed:10);
            var rows=new[]{target,DraftingTests.Participant("left",12,-0.8),DraftingTests.Participant("right",12,0.8)};
            var evaluator=Evaluator(); var state=new PhysiologyState(150,1,0,0); var path=TrackDistance.FromMeters(200);
            Assert.That(evaluator.Evaluate(self,rows,state,1,6,true,0,0,path).Reason,Is.EqualTo(OvertakeDecisionReason.Space));
            Assert.Catch(()=>evaluator.Evaluate(self,rows,state,double.NaN,6,true,0,0,path));
            Assert.Catch(()=>OvertakeDecisionDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(),"minimum_closing_speed_mps","0")));
            var open=new[]{target}; evaluator.Evaluate(self,open,state,1,6,true,0,0,path);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++) evaluator.Evaluate(self,open,state,1,6,true,0,0,path);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

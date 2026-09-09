using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class DraftSelectionTests
    {
        public static DraftSelectionDefinition Definition()=>DraftSelectionDefinition.FromJson(PhysiologyContractTests.DefinitionJson("draft_selection_provisional_v1.json"));
        [Test] public void HoldAndMarginPreventOscillationButMissingTargetReleasesImmediately()
        {
            var self=DraftingTests.Participant("self",10); var selector=new DraftTargetSelector(Definition(),DraftingTests.Definition(),self.AthleteId,20);
            var rows=new[]{self,DraftingTests.Participant("a",14),DraftingTests.Participant("b",15)}; var track=DraftingTests.Flat();
            var first=selector.Evaluate(new SimulationTick(1),0,rows,track); Assert.That(first.Selected.LeaderId.Value,Is.EqualTo("a"));
            rows[2]=DraftingTests.Participant("b",12);
            var held=selector.Evaluate(new SimulationTick(10),0,rows,track); Assert.That(held.Selected.LeaderId.Value,Is.EqualTo("a"));
            Assert.That(held.Reason,Is.EqualTo(DraftSelectionReason.HoldDuration));
            rows[2]=DraftingTests.Participant("b",13.9);
            var margin=selector.Evaluate(new SimulationTick(41),0,rows,track); Assert.That(margin.Reason,Is.EqualTo(DraftSelectionReason.ImprovementTooSmall));
            rows[2]=DraftingTests.Participant("b",12);
            var switched=selector.Evaluate(new SimulationTick(42),0,rows,track); Assert.That(switched.Selected.LeaderId.Value,Is.EqualTo("b"));
            Assert.That(switched.Reason,Is.EqualTo(DraftSelectionReason.Switched));
            rows[2]=DraftingTests.Participant("b",12,corridor:"other");
            Assert.That(selector.Evaluate(new SimulationTick(43),0,rows,track).Selected.LeaderId.Value,Is.EqualTo("a"));
            rows[1]=DraftingTests.Participant("a",9);
            var lost=selector.Evaluate(new SimulationTick(44),0,rows,track); Assert.That(lost.Selected.Strength,Is.Zero);
            Assert.That(lost.Reason,Is.EqualTo(DraftSelectionReason.LostTarget));
        }
        [Test] public void StableTieOrderInvalidStateAndAllocationChecks()
        {
            var self=DraftingTests.Participant("self",10); var a=DraftingTests.Participant("a",12); var b=DraftingTests.Participant("b",12);
            var left=new DraftTargetSelector(Definition(),DraftingTests.Definition(),self.AthleteId,20);
            var right=new DraftTargetSelector(Definition(),DraftingTests.Definition(),self.AthleteId,20); var track=DraftingTests.Flat();
            var rows=new[]{self,b,a}; var first=left.Evaluate(new SimulationTick(1),0,rows,track);
            Assert.That(first,Is.EqualTo(right.Evaluate(new SimulationTick(1),1,new[]{a,self,b},track)));
            Assert.That(first.Selected.LeaderId.Value,Is.EqualTo("a"));
            Assert.Catch(()=>left.Evaluate(new SimulationTick(2),0,new[]{self,a,a},track)); Assert.That(left.Tick.Value,Is.EqualTo(1));
            Assert.Catch(()=>left.Evaluate(new SimulationTick(2),0,rows,track,headwind:double.NaN)); Assert.That(left.LastFrame,Is.EqualTo(first));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=2;i<1000;i++) left.Evaluate(new SimulationTick(i),0,rows,track);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

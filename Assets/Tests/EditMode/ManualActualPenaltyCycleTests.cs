using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualActualPenaltyCycleTests
    {
        private static ManualScopedAiMovement Mover(ManualScopedFieldStepper stepper)=>new ManualScopedAiMovement(stepper,
            ManualAiSteeringDefinition.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/manual_ai_steering_v2")),
            ManualScopedFieldStepperTests.Steering());
        private static void Step(ManualScopedAiMovement mover,PackSprintFieldSession field,SprintAthleteInput[] inputs,double[] lines)
        {
            var tick=field.Tick.Next();
            for(int i=0;i<field.Count;i++)
            {
                var a=field.GetAthlete(i);bool miss=i==0;
                inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,600),1,1,0),
                    new WeaponAimCommand(tick,a.AthleteId,miss?1000:0,miss?1000:0),true);
                lines[i]=mover.PreferredLine(i);
            }
            mover.Step(inputs,lines);
        }
        [TestCase(1)][TestCase(3)]
        public void FiveGeometricMissesPayAllRealCircuitsBeforeTheManualLapResumes(int count)
        {
            var f=ManualScopedFieldStepperTests.InitialCandidate(count);
            var mover=Mover(f.stepper);
            var inputs=new SprintAthleteInput[count];var lines=new double[count];
            void Advance()=>Step(mover,f.runner.Field,inputs,lines);
            while(f.runner.Field.GetAthlete(0).ActiveShooting==null&&f.runner.Field.Tick.Value<7000)Advance();
            var athlete=f.runner.Field.GetAthlete(0);
            Assert.That(athlete.ActiveShooting,Is.Not.Null);
            while(athlete.Lifecycle.CompletedBouts==0&&f.runner.Field.Tick.Value<8000)
            {
                // Deliberate maximum aim away from the targets; the actual ray,
                // sway, trigger lifecycle and target geometry resolve every shot.
                Advance();
            }
            Assert.That(athlete.Lifecycle.CompletedBouts,Is.EqualTo(1));
            Assert.That(athlete.GetBoutClosedMask(0),Is.Zero);
            Assert.That(athlete.TotalPenaltyLoops,Is.EqualTo(5));
            bool seenCircuit=false,returned=false;long circuitRoute=0,firstTravel=0,lastTravel=0;int maximumOrdinal=0;
            while(!returned&&f.runner.Field.Tick.Value<15000)
            {
                try {Advance();}
                catch(Exception e)
                {
                    Assert.Fail("Actual penalty cycle tick="+f.runner.Field.Tick.Next().Value+"; status="+f.stepper.LastStep.Status+
                    "; world="+f.stepper.LastStep.WorldAttempt.Status+"; actor="+f.stepper.LastStep.WorldAttempt.Athlete+
                    "; working="+f.stepper.LastRejectedWorkingActor.Observation.OccurrenceIndex+":"+
                    f.stepper.LastRejectedWorkingActor.Observation.PhysicalArcNanometers+"; "+e);
                }
                var cursor=f.stepper.World.GetCursor(0);var row=f.stepper.World.GetCatalog(0).Get(cursor.OccurrenceIndex);
                if(row.Kind==ManualRacePathKind.PenaltyLoop)
                {
                    if(!seenCircuit){circuitRoute=athlete.RouteNanometers;firstTravel=athlete.Motion.DistanceNanometers;}
                    seenCircuit=true;maximumOrdinal=Math.Max(maximumOrdinal,row.PenaltyOrdinal);lastTravel=athlete.Motion.DistanceNanometers;
                    Assert.That(athlete.RouteNanometers,Is.EqualTo(circuitRoute));
                }
                returned=seenCircuit&&row.Kind==ManualRacePathKind.BaseRoute;
                Assert.That(f.runner.Field.Tick,Is.EqualTo(f.stepper.World.Tick));
            }
            Assert.That(returned,Is.True);Assert.That(maximumOrdinal,Is.EqualTo(5));
            Assert.That(athlete.GetBoutRangeFrame(0).CompletedLoops,Is.EqualTo(5));
            Assert.That(lastTravel-firstTravel,Is.GreaterThan(740*SkiState.NanometersPerMeter));
            int completed=0;for(int i=0;i<f.events.Count;i++)
                if(f.events[i].AthleteId.Equals(athlete.AthleteId)&&f.events[i].EventType==RaceEventType.PenaltyLoopCompleted)completed++;
            Assert.That(completed,Is.EqualTo(5));
            TestContext.WriteLine("Actual five-miss penalty cycle: actors="+count+"; returned tick="+f.runner.Field.Tick.Value);
        }

        [Test] public void TheCompleteFiveCircuitDebtRepeatsEveryActualWorldStateAndEventExactly()
        {
            var a=ManualScopedFieldStepperTests.InitialCandidate(3);var b=ManualScopedFieldStepperTests.InitialCandidate(3);
            var am=Mover(a.stepper);var bm=Mover(b.stepper);
            var ai=new SprintAthleteInput[3];var bi=new SprintAthleteInput[3];var al=new double[3];var bl=new double[3];bool returned=false;
            while(!returned&&a.runner.Field.Tick.Value<15000)
            {
                Step(am,a.runner.Field,ai,al);Step(bm,b.runner.Field,bi,bl);
                for(int i=0;i<3;i++)
                {
                    Assert.That(a.stepper.World.GetActor(i).Pose,Is.EqualTo(b.stepper.World.GetActor(i).Pose));
                    Assert.That(a.stepper.World.GetActor(i).Velocity,Is.EqualTo(b.stepper.World.GetActor(i).Velocity));
                    // Catalog ownership is intentionally a different object in
                    // independently reconstructed races; compare every value.
                    var ac=a.stepper.World.GetCursor(i);var bc=b.stepper.World.GetCursor(i);
                    Assert.That(ac.Athlete,Is.EqualTo(bc.Athlete));Assert.That(ac.OccurrenceIndex,Is.EqualTo(bc.OccurrenceIndex));
                    Assert.That(ac.ObservedArcNanometers,Is.EqualTo(bc.ObservedArcNanometers));
                    Assert.That(ac.CreditedArcNanometers,Is.EqualTo(bc.CreditedArcNanometers));
                    Assert.That(ac.CreditedRouteNanometers,Is.EqualTo(bc.CreditedRouteNanometers));
                    Assert.That(a.runner.Field.GetAthlete(i).Motion,Is.EqualTo(b.runner.Field.GetAthlete(i).Motion));
                    Assert.That(a.runner.Field.GetAthlete(i).PhysiologyState,Is.EqualTo(b.runner.Field.GetAthlete(i).PhysiologyState));
                    Assert.That(a.runner.Field.GetDraft(i),Is.EqualTo(b.runner.Field.GetDraft(i)));
                }
                Assert.That(a.stepper.LastStep.Intervals,Is.EqualTo(b.stepper.LastStep.Intervals));
                var c=a.stepper.World.GetCursor(0);
                returned=a.runner.Field.GetAthlete(0).GetBoutRangeFrame(0).CompletedLoops==5&&
                    a.stepper.World.GetCatalog(0).Get(c.OccurrenceIndex).Kind==ManualRacePathKind.BaseRoute;
            }
            Assert.That(returned,Is.True);Assert.That(a.events.Count,Is.EqualTo(b.events.Count));
            for(int i=0;i<a.events.Count;i++)Assert.That(a.events[i],Is.EqualTo(b.events[i]));
        }

        [TestCase("mass_start",0)][TestCase("mass_start",29)][TestCase("sprint",0)][TestCase("pursuit",29)]
        public void EveryPenaltyBranchSharesThePaidPlaneWithoutMovingAnOffCentreBody(string format,int lane)
        {
            var f=new ManualRacePathCatalogTests.Fixture(format,lane);var c=f.Catalog;
            for(int bout=0;bout<f.Profile.BoutCount;bout++)foreach(double side in new[]{-.8,.8})
            {
                int gate=c.GetPenaltyEntryOccurrence(bout);var row=c.Get(gate);
                var pose=f.At(gate,row.LengthNanometers,side);var original=pose;
                Assert.That(c.TryProject(pose,gate,row.LengthNanometers,0,out var source),Is.True);
                Assert.That(source.PhysicalArcNanometers,Is.EqualTo(row.LengthNanometers));
                for(int ordinal=1;ordinal<=f.Profile.ShotsPerBout;ordinal++)
                {
                    int index=c.GetPenaltyOccurrence(bout,ordinal);var circuit=c.Get(index);
                    Assert.That(c.TryProject(pose,index,0,0,out var start),Is.True);
                    Assert.That(start.PhysicalArcNanometers,Is.Zero);
                    Assert.That(c.TryProject(pose,index,circuit.LengthNanometers,0,out var end),Is.True);
                    Assert.That(end.PhysicalArcNanometers,Is.EqualTo(circuit.LengthNanometers));
                }
                Assert.That(c.TryProject(pose,c.GetPenaltyReturnOccurrence(bout),0,0,out var rejoin),Is.True);
                Assert.That(rejoin.PhysicalArcNanometers,Is.Zero);Assert.That(pose,Is.EqualTo(original));
                var progression=new ManualRacePathProgression(c);
                var cursor=progression.Reconstitute(new AthleteId("portal_control"),gate,row.LengthNanometers,row.LengthNanometers,row.RouteEndNanometers);
                Assert.That(progression.TryPreviewOrdinaryHandoff(cursor,new ManualRacePathActorFrame(cursor.Athlete,pose,default,source),out _),Is.False,
                    "A shared station plane must never select a branch or authorize penalty debt.");
            }
        }
    }
}

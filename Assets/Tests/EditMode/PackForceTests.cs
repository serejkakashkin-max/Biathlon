using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class PackForceTests
    {
        private static PackForceSession Create(SkiPlacement[] initial,RaceEventLog log)=>new PackForceSession(SkiForceTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),OccupancyTests.Definition(),20,initial,log);
        private static void Fill(PackForceSession s,PackForceCommand[] commands)
        { for(int i=0;i<s.Count;i++) commands[i]=new PackForceCommand(s.Tick.Next(),s.GetPlacement(i).AthleteId,DraftingTests.Flat(),350,0,6); }
        [Test] public void SharedDraftAndForcesUseOneSnapshotAndReplayDenseCatchupWithoutCrossing()
        {
            var initial=new SkiPlacement[12]; for(int i=0;i<12;i++) initial[i]=DraftingTests.Participant("a"+i,100+i*3,speed:12-i*0.3);
            var log=new RaceEventLog(4000); var replayLog=new RaceEventLog(4000); var a=Create(initial,log); var b=Create(initial,replayLog);
            var commands=new PackForceCommand[12]; var old=new SkiPlacement[12]; var occupancy=new OccupancySystem(OccupancyTests.Definition());
            var draftSystem=new DraftingSystem(DraftingTests.Definition()); var forceSystem=new SkiForceSystem(SkiForceTests.Definition(),20); var track=DraftingTests.Flat(); bool constrained=false;
            for(int tick=1;tick<=1000;tick++)
            {
                for(int i=0;i<12;i++) old[i]=a.GetPlacement(i);
                Fill(a,commands); a.Step(commands); b.Step(commands);
                for(int i=0;i<12;i++)
                {
                    var frame=a.GetFrame(i); Assert.That(frame,Is.EqualTo(b.GetFrame(i))); constrained|=frame.Contact.Constrained;
                    var expectedDraft=draftSystem.Evaluate(old[i],old,track);
                    Assert.That(frame.Draft,Is.EqualTo(expectedDraft));
                    Assert.That(frame.UnconstrainedForces,Is.EqualTo(forceSystem.Step(old[i].Motion,350,track,dragAreaMultiplier:expectedDraft.DragAreaMultiplier)));
                    Assert.That(frame.Placement.Motion.DistanceNanometers,Is.LessThanOrEqualTo(frame.UnconstrainedForces.State.DistanceNanometers));
                    for(int j=0;j<i;j++) Assert.That(occupancy.QueryRelativeMove(old[i],a.GetPlacement(i),old[j],a.GetPlacement(j)).Blocked,Is.False);
                }
            }
            Assert.That(constrained,Is.True); Assert.That(log.Count,Is.EqualTo(replayLog.Count));
            for(int i=0;i<log.Count;i++) Assert.That(log[i],Is.EqualTo(replayLog[i]));
        }
        [Test] public void LateralMovesPayLossAndBadLastRowCannotAdvanceStateOrEvents()
        {
            var initial=new[]{DraftingTests.Participant("a",10,-1),DraftingTests.Participant("b",10,1)}; var log=new RaceEventLog(100); var s=Create(initial,log);
            var commands=new PackForceCommand[2]; Fill(s,commands);
            commands[0]=new PackForceCommand(new SimulationTick(1),initial[0].AthleteId,DraftingTests.Flat(),350,1,6);
            commands[1]=new PackForceCommand(new SimulationTick(1),initial[1].AthleteId,DraftingTests.Flat(),double.NaN,-1,6);
            Assert.Catch(()=>s.Step(commands)); Assert.That(s.Tick.Value,Is.Zero); Assert.That(log.Count,Is.Zero); Assert.That(s.GetPlacement(0),Is.EqualTo(initial[0]));
            commands[1]=new PackForceCommand(new SimulationTick(1),initial[1].AthleteId,DraftingTests.Flat(),350,-1,6); s.Step(commands);
            Assert.That(s.GetFrame(0).LateralPropulsionMultiplier,Is.LessThan(1));
            Assert.That(new OccupancySystem(OccupancyTests.Definition()).QueryRelativeMove(initial[0],s.GetPlacement(0),initial[1],s.GetPlacement(1)).Blocked,Is.False);
        }
        [Test] public void ReversedRosterProducesSamePerAthleteMotion()
        {
            var initial=new[]{DraftingTests.Participant("a",10,speed:12),DraftingTests.Participant("b",13,speed:8),DraftingTests.Participant("c",16,speed:10)};
            var a=Create(initial,new RaceEventLog(1000)); var b=Create(new[]{initial[2],initial[1],initial[0]},new RaceEventLog(1000));
            var ca=new PackForceCommand[3]; var cb=new PackForceCommand[3]; var track=DraftingTests.Flat();
            for(int tick=1;tick<=300;tick++)
            {
                for(int i=0;i<3;i++)
                {
                    ca[i]=new PackForceCommand(a.Tick.Next(),a.GetPlacement(i).AthleteId,track,350,0,6);
                    cb[i]=new PackForceCommand(b.Tick.Next(),b.GetPlacement(i).AthleteId,track,350,0,6);
                }
                a.Step(ca); b.Step(cb);
                for(int i=0;i<3;i++) Assert.That(a.GetPlacement(i),Is.EqualTo(b.GetPlacement(2-i)));
            }
        }
        [Test] public void WarmPackStepIsAllocationFree()
        {
            var initial=new[]{DraftingTests.Participant("a",10),DraftingTests.Participant("b",15)}; var s=Create(initial,new RaceEventLog(4000));
            var commands=new PackForceCommand[2]; var track=DraftingTests.Flat();
            void FillFast(){for(int i=0;i<2;i++) commands[i]=new PackForceCommand(s.Tick.Next(),s.GetPlacement(i).AthleteId,track,350,0,6);}
            FillFast(); s.Step(commands); long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){FillFast();s.Step(commands);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}



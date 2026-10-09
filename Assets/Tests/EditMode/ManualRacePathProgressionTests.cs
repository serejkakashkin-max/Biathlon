using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualRacePathProgressionTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private static ManualRacePathActorFrame Frame(ManualRacePathCatalogTests.Fixture f,int index,long arc,string id="actor")
        {
            var pose=f.At(index,arc);
            Assert.That(f.Catalog.TryProject(pose,index,arc,.001,out var observation),Is.True);
            return new ManualRacePathActorFrame(new AthleteId(id),pose,new SkiPlanarVelocity(0,0),observation);
        }
        private static ManualRacePathCursor Cursor(ManualRacePathCatalogTests.Fixture f,ManualRacePathProgression p,
            int index,long observed,long highWater,string id="actor")
        {
            var row=f.Catalog.Get(index);
            return p.Reconstitute(new AthleteId(id),index,observed,highWater,
                row.RouteStartNanometers+(row.CountsTowardsRoute?highWater:0));
        }
        [TestCase(ManualRacePathKind.StartApproach)]
        [TestCase(ManualRacePathKind.RangeApproach)]
        [TestCase(ManualRacePathKind.RangeExit)]
        [TestCase(ManualRacePathKind.BaseRoute)]
        [TestCase(ManualRacePathKind.PenaltyLoop)]
        public void BackwardMotionAndReturnNeverDoubleCountArcOrBaseDistance(ManualRacePathKind kind)
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);
            int index=0;while(f.Catalog.Get(index).Kind!=kind||f.Catalog.Get(index).LengthNanometers<4*N)index++;
            var row=f.Catalog.Get(index);long high=2*N;
            var accepted=Cursor(f,p,index,high,high);
            Assert.That(p.TryPreview(Frame(f,index,N),accepted,row.LengthNanometers,out var backward),Is.True);
            Assert.That(backward.Cursor.ObservedArcNanometers,Is.EqualTo(N));
            Assert.That(backward.Cursor.CreditedArcNanometers,Is.EqualTo(high));
            Assert.That(backward.NewlyCreditedArcNanometers,Is.Zero);Assert.That(backward.NewlyCreditedRouteNanometers,Is.Zero);
            Assert.That(p.TryPreview(Frame(f,index,high),backward.Cursor,row.LengthNanometers,out var returned),Is.True);
            Assert.That(returned.NewlyCreditedArcNanometers,Is.Zero);Assert.That(returned.NewlyCreditedRouteNanometers,Is.Zero);
            Assert.That(p.TryPreview(Frame(f,index,3*N),returned.Cursor,row.LengthNanometers,out var advanced),Is.True);
            Assert.That(advanced.NewlyCreditedArcNanometers,Is.EqualTo(N));
            Assert.That(advanced.NewlyCreditedRouteNanometers,Is.EqualTo(row.CountsTowardsRoute?N:0));
            Assert.That(accepted.CreditedArcNanometers,Is.EqualTo(high),"Preview never mutates accepted cursor.");
        }
        [Test] public void EveryOrdinaryWorldSeamPreservesPoseAndCreditAndCannotLeaveFiringPenaltyOrFinishGates()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);int crossed=0,gated=0;
            for(int i=0;i<f.Catalog.Count;i++)
            {
                var row=f.Catalog.Get(i);var current=Cursor(f,p,i,row.LengthNanometers,row.LengthNanometers);
                var frame=Frame(f,i,row.LengthNanometers);var pose=frame.Pose;
                bool handoff=p.TryPreviewOrdinaryHandoff(current,frame,out var next);
                if(row.End!=ManualRacePathEnd.Continue)
                {Assert.That(handoff,Is.False,$"Gate {row.End} {row.Id}");gated++;continue;}
                Assert.That(handoff,Is.True,row.Id);crossed++;
                Assert.That(next.OccurrenceIndex,Is.EqualTo(f.Catalog.GetBaseFlowSuccessor(i)));
                Assert.That(next.ObservedArcNanometers,Is.Zero);Assert.That(next.CreditedArcNanometers,Is.Zero);
                Assert.That(next.CreditedRouteNanometers,Is.EqualTo(current.CreditedRouteNanometers));Assert.That(frame.Pose,Is.EqualTo(pose));
                Assert.That(p.TryPreview(frame,next,f.Catalog.Get(next.OccurrenceIndex).LengthNanometers,out _),Is.False,
                    "Old occurrence frame cannot be silently re-labelled as admitted successor.");
            }
            Assert.That(crossed,Is.GreaterThan(5));Assert.That(gated,Is.EqualTo(f.Profile.BoutCount*2+f.Profile.BoutCount*f.Profile.ShotsPerBout+1));
        }
        [Test] public void HighWaterAtEndDoesNotHandoffWhileTheActualBodyIsBehindIt()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);
            var row=f.Catalog.Get(0);var retreated=Cursor(f,p,0,row.LengthNanometers-N,row.LengthNanometers);
            Assert.That(p.TryPreviewOrdinaryHandoff(retreated,Frame(f,0,row.LengthNanometers-N),out _),Is.False);
            Assert.That(p.TryPreviewOrdinaryHandoff(retreated,Frame(f,0,row.LengthNanometers),out _),Is.False,
                "Even a valid proposed endpoint needs an accepted world/phase commit before handoff.");
        }
        [TestCase(-1.6)][TestCase(1.6)]
        public void ObliqueOrdinarySeamUsesOneTransverseSectionForBothOccurrences(double side)
        {
            var f=new ManualRacePathCatalogTests.Fixture();var c=f.Catalog;
            int next=c.GetFiringOccurrence(0),source=next-1;
            var row=c.Get(source);var successor=c.Get(next);
            Assert.That(row.End,Is.EqualTo(ManualRacePathEnd.Continue));
            Assert.That(c.TrySample(source,row.LengthNanometers,out var a),Is.True);
            Assert.That(c.TrySample(next,0,out var b),Is.True);
            double fx=a.ForwardX+b.ForwardX,fz=a.ForwardZ+b.ForwardZ;
            double norm=Math.Sqrt(fx*fx+fz*fz);fx/=norm;fz/=norm;
            var pose=new ManualSkiHeadingState(a.X+fz*side,a.Z-fx*side,Math.Atan2(fx,fz),.17);
            Assert.That(c.TryProject(pose,source,row.LengthNanometers,.5,out var before),Is.True);
            Assert.That(before.PhysicalArcNanometers,Is.EqualTo(row.LengthNanometers));
            Assert.That(c.TryProject(pose,next,0,0,out var after),Is.True);
            Assert.That(after.PhysicalArcNanometers,Is.Zero);
            var p=new ManualRacePathProgression(c);
            var accepted=p.Reconstitute(new AthleteId("a"),source,row.LengthNanometers,row.LengthNanometers,row.RouteEndNanometers);
            var frame=new ManualRacePathActorFrame(accepted.Athlete,pose,new SkiPlanarVelocity(3,1),before);
            Assert.That(p.TryPreviewOrdinaryHandoff(accepted,frame,out var entered),Is.True);
            Assert.That(entered.ObservedArcNanometers,Is.Zero);
            Assert.That(entered.CreditedArcNanometers,Is.Zero);
            Assert.That(entered.CreditedRouteNanometers,Is.EqualTo(accepted.CreditedRouteNanometers));
            Assert.That(frame.Pose,Is.EqualTo(pose));
        }
        [Test] public void UnadmittedLastRowRejectsTheWholeProgressProposalWithoutWritingEarlierSlots()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);int index=1;
            var rows=new[]{Cursor(f,p,index,N,N,"a"),Cursor(f,p,index,N,N,"b")};
            var frames=new[]{Frame(f,index,2*N,"a"),Frame(f,index,3*N,"b")};
            var proposals=new ManualRacePathProgressFrame[2];var caps=new[]{2*N,2*N};
            Assert.That(p.TryPreviewGroup(frames,rows,caps,2,proposals),Is.False);
            Assert.That(proposals[0].IsValid,Is.False);Assert.That(proposals[1].IsValid,Is.False);
            caps[1]=3*N;Assert.That(p.TryPreviewGroup(frames,rows,caps,2,proposals),Is.True);
            Assert.That(proposals[0].NewlyCreditedArcNanometers,Is.EqualTo(N));
            Assert.That(proposals[1].NewlyCreditedArcNanometers,Is.EqualTo(2*N));
            var a=proposals[0];var b=proposals[1];caps[1]=2*N;
            Assert.That(p.TryPreviewGroup(frames,rows,caps,2,proposals),Is.False);
            Assert.That(proposals[0],Is.EqualTo(a));Assert.That(proposals[1],Is.EqualTo(b));
        }
        [Test] public void ForeignCatalogActorOccurrenceOrWorldPositionCannotBecomeAcceptedProgress()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);
            var foreign=new ManualRacePathCatalogTests.Fixture(lane:29);var other=new ManualRacePathProgression(foreign.Catalog);
            var cursor=Cursor(f,p,1,N,N);var safe=Frame(f,1,2*N);
            Assert.That(p.TryPreview(safe,cursor,N,out _),Is.False);
            Assert.That(p.TryPreview(Frame(f,1,2*N,"different"),cursor,3*N,out _),Is.False);
            Assert.That(p.TryPreview(Frame(f,2,2*N),cursor,3*N,out _),Is.False);
            Assert.That(p.TryPreview(Frame(foreign,1,2*N),cursor,3*N,out _),Is.False);
            Assert.That(p.TryPreview(safe,Cursor(foreign,other,1,N,N),3*N,out _),Is.False);
            var displaced=new ManualSkiHeadingState(safe.Pose.X+2,safe.Pose.Z+2,safe.Pose.HeadingRadians);
            Assert.That(p.TryPreview(new ManualRacePathActorFrame(safe.Athlete,displaced,safe.Velocity,safe.Observation),cursor,3*N,out _),Is.False);
            Assert.That(p.TryPreviewOrdinaryHandoff(Cursor(foreign,other,1,N,N),safe,out _),Is.False);
            Assert.That(p.TryPreview(default,cursor,3*N,out _),Is.False);
            Assert.That(p.TryPreview(safe,default,3*N,out _),Is.False);
        }
        [Test] public void InvalidRestoreAndRosterShapesFailAndWarmProgressAndHandoffAllocateNothing()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var p=new ManualRacePathProgression(f.Catalog);
            Assert.Throws<ArgumentException>(()=>p.Reconstitute(default,0,0,0,0));
            Assert.Throws<ArgumentException>(()=>p.Reconstitute(new AthleteId("a"),0,N,0,0));
            Assert.Throws<ArgumentException>(()=>p.Reconstitute(new AthleteId("a"),0,0,0,N));
            var row=f.Catalog.Get(0);var atEnd=Cursor(f,p,0,row.LengthNanometers,row.LengthNanometers);
            var frame=Frame(f,0,row.LengthNanometers);
            var duplicate=new[]{atEnd,atEnd};var frames=new[]{frame,frame};var caps=new[]{row.LengthNanometers,row.LengthNanometers};
            Assert.Throws<ArgumentException>(()=>p.TryPreviewGroup(frames,duplicate,caps,2,new ManualRacePathProgressFrame[2]));
            Assert.Throws<ArgumentException>(()=>p.TryPreviewGroup(null,duplicate,caps,2,new ManualRacePathProgressFrame[2]));
            for(int i=0;i<128;i++)
            {p.TryPreview(frame,atEnd,row.LengthNanometers,out _);p.TryPreviewOrdinaryHandoff(atEnd,frame,out _);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {p.TryPreview(frame,atEnd,row.LengthNanometers,out _);p.TryPreviewOrdinaryHandoff(atEnd,frame,out _);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.That(p.HandoffContextJson,Does.Contain("race_phase_gates_closed"));
        }
    }
}

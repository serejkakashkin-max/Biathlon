using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ShotImpactLedgerTests
    {
        [TestCase(ShootingStance.Prone)][TestCase(ShootingStance.Standing)]
        public void ExactEdgeClosesButOneNanometerOutsideRemainsAMissWithItsActualMark(ShootingStance stance)
        {
            var g=TargetGeometryTests.Definition();var geometry=new TargetGeometrySystem(g);var ledger=new ShotImpactLedger(g,.0056,2);
            double x=g.CenterX(2)+g.Radius(stance);
            var edge=geometry.IntersectPlanePoint(x,0,stance);
            Assert.That(ledger.TryObserve("player",100,0,3,1,stance,ShotOutcome.TargetClosed,edge,out var hit),Is.True);
            Assert.That(hit.ClosedTarget&&hit.NearEdge,Is.True);Assert.That(hit.EdgeClearanceMeters,Is.EqualTo(0).Within(1e-12));
            var outside=geometry.IntersectPlanePoint(x+1e-9,0,stance);
            Assert.That(ledger.TryObserve("player",130,0,3,2,stance,ShotOutcome.Miss,outside,out var miss),Is.True);
            Assert.That(miss.ClosedTarget,Is.False);Assert.That(miss.NearEdge,Is.True);Assert.That(miss.EdgeClearanceMeters,Is.LessThan(0));
            Assert.That(miss.Impact.X,Is.EqualTo(outside.X));Assert.That(ledger.Shot("player",1).Impact.X,Is.EqualTo(edge.X));
        }
        [Test]
        public void DuplicateCatchupRejectedTriggerAndGapsCannotInventMarks()
        {
            var g=TargetGeometryTests.Definition();var ledger=new ShotImpactLedger(g,.0056,2);var impact=new TargetGeometrySystem(g).IntersectPlanePoint(0,0,ShootingStance.Prone);
            Assert.That(ledger.TryObserve("a",100,0,1,1,ShootingStance.Prone,ShotOutcome.TargetClosed,impact,out _),Is.True);
            Assert.That(ledger.TryObserve("a",100,0,1,1,ShootingStance.Prone,ShotOutcome.TargetClosed,impact,out _),Is.False);
            Assert.That(ledger.TryObserve("a",101,0,1,1,ShootingStance.Prone,ShotOutcome.TargetClosed,impact,out _),Is.False);
            Assert.That(ledger.TryObserve("a",102,0,1,2,ShootingStance.Prone,ShotOutcome.NoShot,impact,out _),Is.False);
            Assert.That(ledger.TryObserve("a",140,0,1,5,ShootingStance.Prone,ShotOutcome.AlreadyClosed,impact,out _),Is.True);
            Assert.That(ledger.Shot("a",2).Valid,Is.False);Assert.That(ledger.AcceptedCount,Is.EqualTo(2));
            Assert.That(ledger.TryObserve("b",141,0,1,1,ShootingStance.Prone,ShotOutcome.Miss,new TargetIntersection(false,-1,0,0),out var absent),Is.True);
            Assert.That(absent.NearEdge,Is.False);Assert.That(absent.NearestTarget,Is.EqualTo(-1));
            Assert.That(ledger.TryObserve("a",150,1,7,1,ShootingStance.Standing,ShotOutcome.TargetClosed,impact,out _),Is.True);
            Assert.That(ledger.Shot("a",5).Valid,Is.False);Assert.That(ledger.Last("b").Valid,Is.True);
            ledger.Clear();Assert.That(ledger.Last("a").Valid,Is.False);Assert.That(ledger.AcceptedCount,Is.Zero);
        }
        [Test]
        public void WarmObservationAndDuplicateRejectionAllocateNothing()
        {
            var ledger=new ShotImpactLedger(TargetGeometryTests.Definition(),.0056,1);var impact=new TargetIntersection(true,-1,.07,.02);
            ledger.TryObserve("a",1,0,1,1,ShootingStance.Prone,ShotOutcome.Miss,impact,out _);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=1;i<=10000;i++)ledger.TryObserve("a",i+1,i,1,1,ShootingStance.Prone,ShotOutcome.Miss,impact,out _);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero);
        }
    }
}

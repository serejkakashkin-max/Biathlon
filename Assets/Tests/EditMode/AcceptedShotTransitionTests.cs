using System;
using System.Runtime.CompilerServices;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class AcceptedShotTransitionTests
    {
        private static RangeBroadcastObservation A(int shots,int mask,int completed=0,bool active=true,int last=0,string id="player")=>
            new RangeBroadcastObservation(id,completed,1,shots,mask,last,active);
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void EachHitUsesAcceptedClosureRatherThanTargetOrdinal(int ordinal)
        {
            int mask=31&~((1<<(5-ordinal))-1);
            Assert.That(AcceptedShotTransition.Result(A(ordinal,mask),A(ordinal+1,mask|(1<<(4-ordinal)))),Is.EqualTo(RangeShotMark.Hit));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)]
        public void EachMissUsesAcceptedShotWithoutClosure(int ordinal)
        {
            int mask=(1<<ordinal)-1;
            Assert.That(AcceptedShotTransition.Result(A(ordinal,mask),A(ordinal+1,mask)),Is.EqualTo(RangeShotMark.Miss));
        }
        [TestCase(30,31,RangeShotMark.Hit)][TestCase(15,15,RangeShotMark.Miss)]
        public void FifthShotAndMatReleaseInOneAcceptedStateRetainsItsResult(int before,int final,RangeShotMark result)
        {Assert.That(AcceptedShotTransition.Result(A(4,before),A(0,0,1,false,final)),Is.EqualTo(result));}
        [Test]public void DuplicateBootstrapNewBoutOrForeignAthleteNeverInventsAReaction()
        {
            Assert.That(AcceptedShotTransition.Result(A(1,1),A(1,1)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(0,0,0,false),A(1,1)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(4,15),A(1,1,1)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(0,0),A(1,1,id:"bot")),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(5,31),A(0,0,1,false,31)),Is.EqualTo(RangeShotMark.Pending));
        }
        [Test]public void GapsRegressionsAndImpossibleMasksDoNotGuessOneResult()
        {
            Assert.That(AcceptedShotTransition.Result(A(0,0),A(2,1)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(2,3),A(1,1)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(1,1),A(2,2)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(0,0),A(1,3)),Is.EqualTo(RangeShotMark.Pending));
            Assert.That(AcceptedShotTransition.Result(A(0,0),A(1,32)),Is.EqualTo(RangeShotMark.Pending));
        }
        [Test]public void AcceptedResultObservationAllocatesNoManagedMemory()
        {
            var before=A(0,0);var after=A(1,1);int warm=0;
            for(int i=0;i<1000;i++)if(AcceptedShotTransition.Result(before,after)==RangeShotMark.Hit)warm++;
            Assert.That(warm,Is.EqualTo(1000));GC.GetAllocatedBytesForCurrentThread();
            // Keep NUnit's boxed constraints outside the measured method. CoreCLR
            // may otherwise hoist their allocations across the final counter read.
            Measure(before,after,out _);long allocated=Measure(before,after,out int hits);
            Assert.That(hits,Is.EqualTo(10000));Assert.That(allocated,Is.Zero);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]private static long Measure(in RangeBroadcastObservation before,in RangeBroadcastObservation after,out int hits)
        {
            long start=GC.GetAllocatedBytesForCurrentThread();hits=0;
            for(int i=0;i<10000;i++)if(AcceptedShotTransition.Result(before,after)==RangeShotMark.Hit)hits++;
            return GC.GetAllocatedBytesForCurrentThread()-start;
        }
    }
}

using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class AiRangeBypassSideTests
    {
        private static SkiPlacement At(string id,double station,double lateral)=>new SkiPlacement(
            new AthleteId(id),"exit_aisle",new SkiState((long)(station*SkiState.NanometersPerMeter),4),lateral);

        [Test]
        public void NearestForwardConflictDeterminesSafeSearchOrderIndependentOfRosterOrder()
        {
            var self=At("self",100,0);var left=At("left",103,-.4);
            var right=At("right",105,.4);var behind=At("behind",99,-.4);
            Assert.That(AiRangeBypassSide.PreferRight(new[]{self,left,right,behind},6,.8),Is.True);
            Assert.That(AiRangeBypassSide.PreferRight(new[]{self,right,behind,left},6,.8),Is.True);
            Assert.That(AiRangeBypassSide.PreferRight(new[]{self,behind,right},6,.8),Is.False);
            Assert.That(AiRangeBypassSide.PreferRight(new[]{self,At("far_side",102,-1.5)},6,.8),Is.False);
        }

        [Test]
        public void EqualDistanceUsesStableAthleteIdentityAndWarmChoiceAllocatesNothing()
        {
            var self=At("self",100,0);var left=At("a_left",104,-.2);var right=At("b_right",104,.2);
            var first=new[]{self,left,right};var reversed=new[]{self,right,left};
            Assert.That(AiRangeBypassSide.PreferRight(first,5,.8),Is.True);
            Assert.That(AiRangeBypassSide.PreferRight(reversed,5,.8),Is.True);
            for(int i=0;i<100;i++)AiRangeBypassSide.PreferRight(first,5,.8);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)AiRangeBypassSide.PreferRight(first,5,.8);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.Throws<ArgumentException>(()=>AiRangeBypassSide.PreferRight(new[]{self,self},5,.8));
        }
    }
}

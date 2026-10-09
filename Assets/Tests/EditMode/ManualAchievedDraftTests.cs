using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualAchievedDraftTests
    {
        private static ManualSkiContactBody Body(string id,double x,double z,double vx=10,double vz=0)
            =>new ManualSkiContactBody(new AthleteId(id),new ManualSkiHeadingState(x,z,Math.Atan2(vx,vz)),new SkiPlanarVelocity(vx,vz),75);
        [Test] public void AchievedWorldWakeMatchesExistingStraightDraftLaw()
        {
            var system=new DraftingSystem(DraftingTests.Definition());var surface=DraftingTests.Flat();
            var old=system.Evaluate(DraftingTests.Participant("f",10),new[]{DraftingTests.Participant("a",12)},surface);
            var world=system.EvaluateWorld(Body("f",10,0),new[]{Body("a",12,0)},surface);
            Assert.That(world.LeaderId,Is.EqualTo(old.LeaderId));Assert.That(world.Strength,Is.EqualTo(old.Strength));
            Assert.That(world.DragReduction,Is.EqualTo(old.DragReduction));Assert.That(world.GapMeters,Is.EqualTo(2));
            var rotated=system.EvaluateWorld(Body("f",0,10,0,10),new[]{Body("a",0,12,0,10)},surface);
            Assert.That(rotated.Strength,Is.EqualTo(world.Strength));
        }
        [Test] public void RealSidewaysMotionAndOpposingTrafficCannotUseAFictitiousRouteWake()
        {
            var system=new DraftingSystem(DraftingTests.Definition());var surface=DraftingTests.Flat();var f=Body("f",0,0);
            foreach(var leader in new[]{Body("a",2,3),Body("a",2,0,-10,0),Body("a",2,0,0,0),Body("a",-2,0),Body("f",2,0)})
                Assert.That(system.EvaluateWorld(f,new[]{leader},surface).Strength,Is.Zero);
            Assert.That(system.EvaluateWorld(Body("f",0,0,0,10),new[]{Body("a",2,0)},surface).Strength,Is.Zero);
            Assert.That(system.EvaluateWorld(f,new[]{Body("a",2,0)},TuckTests.Descent()).Strength,Is.Zero);
        }
        [Test] public void ActualWorldWindShiftsTheWakeAndRetainsStableLeaderTies()
        {
            var system=new DraftingSystem(DraftingTests.Definition());var surface=DraftingTests.Flat();var f=Body("f",0,0);
            var leaders=new[]{Body("b",4,0),Body("a",4,0)};
            var calm=system.EvaluateWorld(f,leaders,surface);
            Assert.That(calm.LeaderId.Value,Is.EqualTo("a"));
            Assert.That(system.EvaluateWorld(f,leaders,surface,windX:-10).Strength,Is.GreaterThan(calm.Strength));
            Assert.That(system.EvaluateWorld(f,leaders,surface,windX:40).Strength,Is.Zero);
            var cross=system.EvaluateWorld(f,leaders,surface,windZ:-10);
            double shift=10*surface.WindExposure*4/10*DraftingTests.Definition().CrosswindDisplacementFactor;
            Assert.That(system.EvaluateWorld(Body("f",0,-shift),leaders,surface,windZ:-10).Strength,Is.GreaterThan(cross.Strength));
        }
        [Test] public void WarmWorldWakeIsAllocationFreeAndRejectsMalformedBodiesAndWind()
        {
            var system=new DraftingSystem(DraftingTests.Definition());var surface=DraftingTests.Flat();var f=Body("f",0,0);
            var leaders=new[]{Body("a",2,0),Body("b",3,0)};
            Assert.Throws<ArgumentException>(()=>system.EvaluateWorld(default,leaders,surface));
            Assert.Throws<ArgumentException>(()=>system.EvaluateWorld(f,new ManualSkiContactBody[1],surface));
            Assert.Catch(()=>system.EvaluateWorld(f,leaders,surface,windX:double.NaN));
            Assert.Catch(()=>system.EvaluateWorld(f,leaders,surface,windX:40,windZ:40));
            for(int i=0;i<100;i++)system.EvaluateWorld(f,leaders,surface);
            long allocated=Measure(system,f,leaders,surface,out double sum);
            Assert.That(sum,Is.GreaterThan(0));Assert.That(allocated,Is.Zero);
        }
        [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static long Measure(DraftingSystem system,ManualSkiContactBody f,ManualSkiContactBody[] leaders,TrackSample surface,out double sum)
        {
            sum=0;long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)sum+=system.EvaluateWorld(f,leaders,surface).Strength;
            return GC.GetAllocatedBytesForCurrentThread()-before;
        }
        [Test] public void SharedManualFieldPublishesDraftFromItsActualMovingWorldBodies()
        {
            var f=ManualScopedFieldStepperTests.InitialCandidate(6);bool sheltered=false;
            for(int tick=0;tick<200;tick++)
            {
                f.runner.Step();
                for(int i=0;i<6;i++)
                {
                    var draft=f.runner.Field.GetDraft(i);sheltered|=draft.Strength>0;
                    Assert.That(draft.DragReduction,Is.InRange(0,DraftingTests.Definition().MaximumDragReduction));
                }
                Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
            }
            Assert.That(sheltered,Is.True,"Moving followers must receive their achieved wake after leaving the draft-prohibited start approach.");
        }
    }
}

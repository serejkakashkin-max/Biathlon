using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
using System;
using System.Runtime.Serialization;

namespace Biathlon.Tests
{
    public sealed class ManualActualFinishTests
    {
        [TestCase(1)][TestCase(3)] public void ARealManualRaceCompletesAllBoutsAndCrossesTheFinish(int count)
        {
            // Production Woodland only has intermediate checkpoints. Add a
            // colocated finish witness to independently check the precise clock.
            var f=ManualScopedFieldStepperTests.InitialCandidate(count,withFinish:true,withFinishCheckpoint:true);
            var finished=new bool[count];var times=new FinishTime[count];var finishSpeed=new double[count];bool continued=false,braked=false;
            while(!f.runner.Field.IsComplete&&f.runner.Field.Tick.Value<30000)
            {
                try{f.runner.Step();}
                catch(Exception e){Assert.Fail("tick="+f.runner.Field.Tick.Next().Value+"; "+ManualScopedFieldStepperTests.Explain(f.stepper)+"; actor="+f.stepper.LastStep.Athlete+"; "+e);}
                for(int i=0;i<count;i++)
                {
                    var athlete=f.runner.Field.GetAthlete(i);if(!athlete.HasFinished)continue;
                    var cursor=f.stepper.World.GetCursor(i);var row=f.stepper.World.GetCatalog(i).Get(cursor.OccurrenceIndex);
                    Assert.That(row.Kind,Is.EqualTo(ManualRacePathKind.FinishRunout));
                    Assert.That(athlete.RouteNanometers,Is.EqualTo(f.runner.Field.Route.Length.Millimeters*SkiState.NanometersPerMillimeter));
                    if(!finished[i])
                    {
                        finished[i]=true;times[i]=athlete.Lifecycle.GlobalFinishTime;
                        finishSpeed[i]=athlete.Motion.SpeedMetersPerSecond;
                        Assert.That(cursor.ObservedArcNanometers,Is.GreaterThan(0),"The real remainder after crossing must move onto the runway.");
                        Assert.That(athlete.Motion.SpeedMetersPerSecond,Is.GreaterThan(0),"Finish cannot reset actual velocity.");
                        var split=athlete.Checkpoints.Get(athlete.Checkpoints.Count-1).GlobalTime;
                        Assert.That(split,Is.EqualTo(times[i]),"A finish checkpoint and the lifecycle must record the same owned crossing.");
                    }
                    else
                    {
                        continued=true;
                        Assert.That(athlete.Lifecycle.GlobalFinishTime,Is.EqualTo(times[i]));
                        Assert.That(athlete.LastFrame.Physiology.Effort.DeliveredPowerWatts,Is.Zero);
                        braked|=athlete.Motion.SpeedMetersPerSecond<finishSpeed[i];
                    }
                    Assert.That(athlete.Tick,Is.EqualTo(f.runner.Field.Tick));
                }
            }
            Assert.That(f.runner.Field.IsComplete,Is.True);
            int recorded=0,finishedEvents=0;
            for(int i=0;i<f.events.Count;i++)
            {if(f.events[i].EventType==RaceEventType.RaceFinished)finishedEvents++;if(f.events[i].EventType==RaceEventType.CompetitionResultRecorded)recorded++;}
            Assert.That(recorded,Is.EqualTo(count));Assert.That(finishedEvents,Is.EqualTo(count));
            for(int i=0;i<count;i++)
            {Assert.That(finished[i],Is.True);Assert.That(f.runner.Field.GetAthlete(i).Lifecycle.CompletedBouts,Is.EqualTo(4));}
            if(count>1){Assert.That(continued,Is.True);Assert.That(braked,Is.True,"Finished actors must apply physical braking while the race continues.");}
            Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
            TestContext.WriteLine("Complete manual race: actors="+count+"; tick="+f.runner.Field.Tick.Value);
        }

        [TestCase("mass_start")][TestCase("sprint")][TestCase("pursuit")]
        public void RunoutSharesTheIncomingPortalButCannotAwardRaceDistance(string format)
        {
            var f=new ManualRacePathCatalogTests.Fixture(format);
            var definition=ManualFinishRunoutDefinition.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/manual_finish_runout_v1"));
            var c=new ManualRacePathCatalog(f.Venue.Track,f.Route,f.Profile,f.Venue.Visual,f.Path,f.Start,definition);
            var incoming=c.Get(c.BaseFlowCount-1);var runway=c.Get(c.FinishRunoutIndex);
            Assert.That(runway.Kind,Is.EqualTo(ManualRacePathKind.FinishRunout));Assert.That(runway.CountsTowardsRoute,Is.False);
            Assert.That(runway.RouteStartNanometers,Is.EqualTo(incoming.RouteEndNanometers));
            Assert.That(runway.RouteEndNanometers,Is.EqualTo(incoming.RouteEndNanometers));
            Assert.That(c.GetBaseFlowSuccessor(incoming.Index),Is.EqualTo(-1),"Lifecycle owns finish admission.");
            Assert.That(c.ContextJson,Does.Contain(definition.ToJson().Replace("\"","\\\"")));
            Assert.That(c.TrySample(incoming.Index,incoming.LengthNanometers,out var p),Is.True);
            foreach(double side in new[]{-.8,0,.8})
            {
                var pose=new ManualSkiHeadingState(p.X+p.ForwardZ*side,p.Z-p.ForwardX*side,Math.Atan2(p.ForwardX,p.ForwardZ));
                Assert.That(c.TryProject(pose,incoming.Index,incoming.LengthNanometers,1,out var a),Is.True);
                Assert.That(c.TryProject(pose,runway.Index,0,1,out var b),Is.True);
                Assert.That(a.PhysicalArcNanometers,Is.EqualTo(incoming.LengthNanometers));
                Assert.That(b.PhysicalArcNanometers,Is.Zero);Assert.That(b.MappedRouteNanometers,Is.EqualTo(a.MappedRouteNanometers));
            }
            var changed=ManualFinishRunoutDefinition.FromJson(definition.ToJson().Replace("\"brake_fraction\":1","\"brake_fraction\":0.5"));
            var other=new ManualRacePathCatalog(f.Venue.Track,f.Route,f.Profile,f.Venue.Visual,f.Path,f.Start,changed);
            Assert.That(other.ContextJson,Is.Not.EqualTo(c.ContextJson),"Braking policy is part of the race context.");
        }

        [TestCase("distance_m","0")][TestCase("brake_fraction","0")]
        [TestCase("brake_fraction","1.1")][TestCase("schema_version","2")]
        public void InvalidRunoutPolicyIsRejected(string field,string value)
        {
            var json=ManualScopedFieldStepperTests.Read("Biathlon/manual_finish_runout_v1");
            var invalid=PhysiologyContractTests.ReplaceNumber(json,field,value);
            if(field=="schema_version")Assert.Throws<SerializationException>(()=>ManualFinishRunoutDefinition.FromJson(invalid));
            else Assert.Throws<ArgumentOutOfRangeException>(()=>ManualFinishRunoutDefinition.FromJson(invalid));
        }

        [Test] public void FullRaceAndFinishedBodyContinuationRepeatExactly()
        {
            var a=ManualScopedFieldStepperTests.InitialCandidate(3,withFinish:true);
            var b=ManualScopedFieldStepperTests.InitialCandidate(3,withFinish:true);
            while(!a.runner.Field.IsComplete&&a.runner.Field.Tick.Value<30000)
            {
                a.runner.Step();b.runner.Step();
                Assert.That(a.runner.Field.IsComplete,Is.EqualTo(b.runner.Field.IsComplete));
            }
            Assert.That(a.runner.Field.IsComplete,Is.True);
            for(int i=0;i<3;i++)
            {
                Assert.That(a.stepper.World.GetActor(i).Pose,Is.EqualTo(b.stepper.World.GetActor(i).Pose));
                Assert.That(a.stepper.World.GetActor(i).Velocity,Is.EqualTo(b.stepper.World.GetActor(i).Velocity));
                var x=a.stepper.World.GetCursor(i);var y=b.stepper.World.GetCursor(i);
                Assert.That(x.OccurrenceIndex,Is.EqualTo(y.OccurrenceIndex));Assert.That(x.ObservedArcNanometers,Is.EqualTo(y.ObservedArcNanometers));
                Assert.That(x.CreditedArcNanometers,Is.EqualTo(y.CreditedArcNanometers));Assert.That(x.CreditedRouteNanometers,Is.EqualTo(y.CreditedRouteNanometers));
                var aa=a.runner.Field.GetAthlete(i);var bb=b.runner.Field.GetAthlete(i);
                Assert.That(aa.Motion,Is.EqualTo(bb.Motion));Assert.That(aa.PhysiologyState,Is.EqualTo(bb.PhysiologyState));
                Assert.That(aa.Lifecycle.GlobalFinishTime,Is.EqualTo(bb.Lifecycle.GlobalFinishTime));
                Assert.That(aa.Checkpoints.Get(aa.Checkpoints.Count-1).GlobalTime.CompareTo(aa.Lifecycle.GlobalFinishTime),Is.LessThan(0),"Production Woodland's last marker is intermediate.");
            }
            Assert.That(a.events.Count,Is.EqualTo(b.events.Count));
            for(int i=0;i<a.events.Count;i++)Assert.That(a.events[i],Is.EqualTo(b.events[i]));
        }
    }
}

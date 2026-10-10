using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScopedRaceCommandTapeTests
    {
        private static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper,RaceEventLog events) Initial(string format,int count)
            =>format=="mass_start"?ManualScopedFieldStepperTests.InitialCandidate(count,true):ManualReusableRangeTests.Initial(format,count);
        private static void State(ManualScopedFieldStepper a,ManualScopedFieldStepper b)
        {
            Assert.That(a.Field.Tick,Is.EqualTo(b.Field.Tick));Assert.That(a.World.Tick,Is.EqualTo(b.World.Tick));
            for(int i=0;i<a.World.Count;i++)
            {
                var aa=a.World.GetActor(i);var ba=b.World.GetActor(i);
                Assert.That(aa.Pose,Is.EqualTo(ba.Pose));Assert.That(aa.Velocity,Is.EqualTo(ba.Velocity));
                Assert.That(a.World.GetPhysicalMotion(i),Is.EqualTo(b.World.GetPhysicalMotion(i)));
                var ac=a.World.GetCursor(i);var bc=b.World.GetCursor(i);
                Assert.That(ac.OccurrenceIndex,Is.EqualTo(bc.OccurrenceIndex));
                Assert.That(ac.ObservedArcNanometers,Is.EqualTo(bc.ObservedArcNanometers));
                Assert.That(ac.CreditedArcNanometers,Is.EqualTo(bc.CreditedArcNanometers));
                Assert.That(a.Field.GetAthlete(i).LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                Assert.That(ac.CreditedRouteNanometers,Is.EqualTo(bc.CreditedRouteNanometers));
                Assert.That(a.Field.GetAthlete(i).PhysiologyState,Is.EqualTo(b.Field.GetAthlete(i).PhysiologyState));
                Assert.That(a.Field.GetAthlete(i).Lifecycle.GlobalFinishTime,Is.EqualTo(b.Field.GetAthlete(i).Lifecycle.GlobalFinishTime));
                Assert.That(a.Field.GetDraft(i),Is.EqualTo(b.Field.GetDraft(i)));
                Assert.That(a.Field.GetAthlete(i).RangePath.LaneOrdinal,Is.EqualTo(b.Field.GetAthlete(i).RangePath.LaneOrdinal));
            }
        }
        private static void Events(RaceEventLog a,RaceEventLog b)
        {Assert.That(a.Count,Is.EqualTo(b.Count));for(int i=0;i<a.Count;i++)Assert.That(a[i],Is.EqualTo(b[i]));}
        [TestCase("mass_start",3)][TestCase("sprint",1)][TestCase("pursuit",3)]
        public void CompleteSignedRacePersistsAndReplaysEveryWorldAndResourceEventWithoutAi(string format,int count)
        {
            var source=Initial(format,count);var recorded=Initial(format,count);
            // Production fatigue/recovery can extend a five-lap race beyond
            // the old 25-minute fixture budget. Keep complete-race coverage.
            const int raceBudget=60000;
            var tape=new ManualScopedRaceCommandTape("manual_complete_"+format,recorded.stepper,count*raceBudget);
            var inputs=new SprintAthleteInput[count];var yaw=new int[count];var brake=new double[count];
            bool steeringSeen=false,brakeSeen=false;
            while(!source.runner.Field.IsComplete&&source.runner.Field.Tick.Value<raceBudget)
            {
                source.runner.Step();
                for(int i=0;i<count;i++)
                {
                    inputs[i]=source.runner.GetLastInput(i);yaw[i]=source.runner.GetLastManualSteering(i);brake[i]=source.runner.GetLastManualBrake(i);
                    steeringSeen|=yaw[i]!=0;brakeSeen|=brake[i]>0;
                }
                tape.StepAndRecord(recorded.stepper,inputs,yaw,brake);
                State(source.stepper,recorded.stepper);
            }
            if(!source.runner.Field.IsComplete)for(int i=0;i<count;i++)
            {
                var a=source.runner.Field.GetAthlete(i);var actor=source.stepper.World.GetActor(i);var c=source.stepper.World.GetCursor(i);
                TestContext.WriteLine($"actor={i} tick={a.Tick.Value} finished={a.HasFinished} phase={a.LastFrame.Competition.Phase} bout={a.ActiveBout} row={c.OccurrenceIndex} arc={c.ObservedArcNanometers}/{source.stepper.World.GetCatalog(i).Get(c.OccurrenceIndex).LengthNanometers} x={actor.Pose.X:R} z={actor.Pose.Z:R} speed={a.Motion.SpeedMetersPerSecond:R}");
            }
            Assert.That(source.runner.Field.IsComplete&&recorded.runner.Field.IsComplete,Is.True);
            Assert.That(steeringSeen&&brakeSeen,Is.True);
            string json=tape.ToJson();var restored=ManualScopedRaceCommandTape.FromJson(json);
            Assert.That(restored.ToJson(),Is.EqualTo(json));
            Assert.That(json,Does.Contain("requested_yaw_units"));Assert.That(json,Does.Contain("brake_fraction"));
            Assert.That(json,Does.Not.Contain("requested_lateral_m"));
            Assert.That(json,Does.Contain("range_transit_soft_contacts"));
            Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>PackSprintRaceCommandTape.FromJson(json));
            var replay=Initial(format,count);var cursor=restored.CreateReplay(replay.stepper);
            while(cursor.HasNext)cursor.Step();
            State(recorded.stepper,replay.stepper);Events(recorded.events,replay.events);
            if(recorded.runner.Field.RangeReservations!=null)
                for(int i=0;i<count;i++)for(int bout=0;bout<recorded.runner.Field.RangeReservations.BoutCount;bout++)
                    Assert.That(recorded.runner.Field.RangeReservations.GetRecord(i,bout),Is.EqualTo(replay.runner.Field.RangeReservations.GetRecord(i,bout)));
            Assert.That(replay.runner.TacticalDecisions,Is.Zero,"Playback must not ask the bound runner for any AI decision.");
            Assert.Throws<InvalidOperationException>(()=>cursor.Step());
            TestContext.WriteLine(format+" signed replay actors="+count+" ticks="+tape.TickCount+" runs="+tape.RunCount+" json_chars="+json.Length);
        }
        private static SprintAthleteInput[] Inputs(ManualScopedFieldStepper stepper,int effort=600)
        {
            var inputs=new SprintAthleteInput[stepper.Field.Count];var tick=stepper.Field.Tick.Next();
            for(int i=0;i<inputs.Length;i++)
            {
                var id=stepper.Field.GetAthlete(i).AthleteId;
                inputs[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,effort),1,1,0),new WeaponAimCommand(tick,id,0,0));
            }
            return inputs;
        }
        [Test] public void ExplicitBrakeAndBoltCommandsSurvivePersistenceAndPlayback()
        {
            var f=Initial("mass_start",1);var tape=new ManualScopedRaceCommandTape("manual_brake_bolt",f.stepper,8);
            var inputs=Inputs(f.stepper);var brakes=new[]{.2};var yaw=new[]{0};
            for(int i=0;i<4;i++)
            {
                inputs=Inputs(f.stepper);
                var v=inputs[0];inputs[0]=new SprintAthleteInput(v.Ski,v.Aim,cycleBolt:i%2==0,manualBoltCycle:true);
                brakes[0]=i%2==0?.2:.7;tape.StepAndRecord(f.stepper,inputs,yaw,brakes);
            }
            string json=tape.ToJson();Assert.That(json,Does.Contain("cycle_bolt"));Assert.That(json,Does.Contain("manual_bolt"));
            Assert.That(json,Does.Contain("0.7"));Assert.That(tape.RunCount,Is.EqualTo(4));
            var r=Initial("mass_start",1);var restored=ManualScopedRaceCommandTape.FromJson(json);
            Assert.That(restored.ToJson(),Is.EqualTo(json));var cursor=restored.CreateReplay(r.stepper);
            while(cursor.HasNext)cursor.Step();State(f.stepper,r.stepper);Events(f.events,r.events);
        }
        [Test] public void BadLastControlAndExhaustedCapacityLeaveAllClocksBodiesEventsAndTapeUnchanged()
        {
            var f=Initial("mass_start",3);var tape=new ManualScopedRaceCommandTape("manual_atomic",f.stepper,3);
            var inputs=Inputs(f.stepper);var yaw=new int[3];var brakes=new double[3];
            string before=tape.ToJson();int events=f.events.Count;var body=f.stepper.World.GetActor(0);
            yaw[2]=1001;
            Assert.Throws<ArgumentOutOfRangeException>(()=>tape.StepAndRecord(f.stepper,inputs,yaw,brakes));
            yaw[2]=0;brakes[2]=double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(()=>tape.StepAndRecord(f.stepper,inputs,yaw,brakes));
            Assert.That(tape.ToJson(),Is.EqualTo(before));Assert.That(f.runner.Field.Tick.Value,Is.Zero);
            Assert.That(f.stepper.World.Tick.Value,Is.Zero);Assert.That(f.stepper.World.GetActor(0).Pose,Is.EqualTo(body.Pose));
            Assert.That(f.events.Count,Is.EqualTo(events));
            brakes[2]=0;tape.StepAndRecord(f.stepper,inputs,yaw,brakes);
            before=tape.ToJson();events=f.events.Count;body=f.stepper.World.GetActor(0);
            inputs=Inputs(f.stepper,601);
            Assert.Throws<ArgumentException>(()=>tape.StepAndRecord(f.stepper,inputs,yaw,brakes));
            Assert.That(tape.ToJson(),Is.EqualTo(before));Assert.That(f.runner.Field.Tick.Value,Is.EqualTo(1));
            Assert.That(f.stepper.World.Tick.Value,Is.EqualTo(1));Assert.That(f.stepper.World.GetActor(0).Pose,Is.EqualTo(body.Pose));
            Assert.That(f.events.Count,Is.EqualTo(events));
        }
        [Test] public void ReplayRejectsForeignInitialContextAndExternalClockWithoutMovement()
        {
            var a=Initial("mass_start",1);var b=Initial("sprint",1);
            var tape=new ManualScopedRaceCommandTape("manual_context",a.stepper,4);
            Assert.Throws<ArgumentException>(()=>tape.CreateReplay(b.stepper));
            Assert.That(b.runner.Field.Tick.Value,Is.Zero);
            tape.StepAndRecord(a.stepper,Inputs(a.stepper),new int[1],new double[1]);
            var restored=ManualScopedRaceCommandTape.FromJson(tape.ToJson());var replay=Initial("mass_start",1);
            var cursor=restored.CreateReplay(replay.stepper);replay.runner.Step();
            Assert.Throws<InvalidOperationException>(()=>cursor.Step());
            Assert.That(replay.runner.Field.Tick.Value,Is.EqualTo(1));
            Assert.Throws<ArgumentException>(()=>new ManualScopedRaceCommandTape("late",a.stepper,4));
            Assert.Throws<ArgumentException>(()=>tape.StepAndRecord(replay.stepper,Inputs(replay.stepper),new int[1],new double[1]));
        }
        [Test] public void RepeatedCompleteCommandsMergeAndWarmRecordingAndPlaybackAllocateNothing()
        {
            var f=Initial("mass_start",1);var tape=new ManualScopedRaceCommandTape("manual_runs",f.stepper,1);
            var inputs=Inputs(f.stepper,0);var yaw=new int[1];var brakes=new double[1];
            for(int i=0;i<64;i++){inputs[0]=PackInputAt(f.stepper.Field.Tick.Next());tape.StepAndRecord(f.stepper,inputs,yaw,brakes);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<128;i++){inputs[0]=PackInputAt(f.stepper.Field.Tick.Next());tape.StepAndRecord(f.stepper,inputs,yaw,brakes);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.That(tape.RunCount,Is.EqualTo(1));Assert.That(tape.TickCount,Is.EqualTo(192));
            var r=Initial("mass_start",1);var cursor=ManualScopedRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(r.stepper);
            for(int i=0;i<64;i++)cursor.Step();before=GC.GetAllocatedBytesForCurrentThread();
            while(cursor.HasNext)cursor.Step();
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            State(f.stepper,r.stepper);Events(f.events,r.events);
            SprintAthleteInput PackInputAt(SimulationTick tick)
            {
                var id=f.runner.Field.GetAthlete(0).AthleteId;
                return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,0),1,1,0),new WeaponAimCommand(tick,id,0,0));
            }
        }
        private const string Empty="{\"schema_version\":1,\"kind\":\"manual_scoped_race_commands_v1\",\"id\":\"empty\",\"initial_context_json\":\"opaque_initial_context\",\"tick_count\":0,\"athletes\":[\"a\"],\"signed_athlete_runs\":[]}";
        [TestCase("schema")][TestCase("kind")][TestCase("coverage")][TestCase("duplicate")][TestCase("missing")][TestCase("negative")]
        public void InvalidPersistedContractCannotCreateAReplayTape(string defect)
        {
            string json=Empty;
            switch(defect)
            {
                case "schema":json=json.Replace("\"schema_version\":1","\"schema_version\":2");break;
                case "kind":json=json.Replace("manual_scoped_race_commands_v1","pack_sprint_race_commands_v1");break;
                case "coverage":json=json.Replace("\"tick_count\":0","\"tick_count\":1");break;
                case "duplicate":json=json.Replace("[\"a\"]","[\"a\",\"a\"]");break;
                case "missing":json=json.Replace("signed_athlete_runs","legacy_runs");break;
                case "negative":json=json.Replace("\"tick_count\":0","\"tick_count\":-1");break;
            }
            Assert.Catch(()=>ManualScopedRaceCommandTape.FromJson(json));
        }
    }
}

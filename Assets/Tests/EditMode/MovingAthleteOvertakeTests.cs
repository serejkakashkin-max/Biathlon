using System;
using System.IO;
using Biathlon.Replay;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class MovingAthleteOvertakeTests
    {
        [TestCase(0,false)][TestCase(1,false)][TestCase(1,true)][TestCase(2,false)][TestCase(2,true)]
        public void FasterFreshAthletePassesMovingSlowerPlayerInsteadOfFollowingHisBack(int version,bool reserveGate)
        {
            string policy=PhysiologyContractTests.DefinitionJson(ManualAiSteeringDefinition.ResourceForVersion(version));
            if(reserveGate)policy=policy.Replace("\"moving_overtake_minimum_reserve\": 0.2","\"moving_overtake_minimum_reserve\": 1");
            var f=ManualScopedFieldStepperTests.InitialCandidate(2,productionComposition:true,singleFileGrid:true,aiPolicyJson:policy);
            Assert.That(f.runner.MovingOvertakeVersion,Is.EqualTo(version));
            var initial=f.stepper.World.GetActor(0);double fx=initial.Observation.ForwardX,fz=initial.Observation.ForwardZ;
            double maxGap=-100,maxSide=0;bool faster=false;int follower=1;
            for(int i=0;i<200;i++)
            {
                var player=f.runner.Field.GetAthlete(0);var tick=player.Tick.Next();
                f.runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,player.AthleteId,150),1,1,0),new WeaponAimCommand(tick,player.AthleteId,0,0)),0);
                var a=f.stepper.World.GetActor(0);var b=f.stepper.World.GetActor(follower);
                double gap=(b.Pose.X-a.Pose.X)*fx+(b.Pose.Z-a.Pose.Z)*fz;
                double side=(b.Pose.X-a.Pose.X)*fz-(b.Pose.Z-a.Pose.Z)*fx;
                maxGap=Math.Max(maxGap,gap);maxSide=Math.Max(maxSide,Math.Abs(side));
                faster|=f.runner.Field.GetAthlete(follower).Motion.SpeedMetersPerSecond>player.Motion.SpeedMetersPerSecond+.2;
                if(gap>f.stepper.BodyLongitudinalSeparation)break;
            }
            TestContext.WriteLine($"moving pass max_gap={maxGap:R} lateral={maxSide:R} faster={faster}");
            Assert.That(faster,Is.True,"The trailing athlete must actually have a speed advantage in the physical scenario.");
            if(version>0&&!reserveGate)Assert.That(maxGap,Is.GreaterThan(f.stepper.BodyLongitudinalSeparation),"A free adjacent line and a faster fresh athlete must allow a physical pass, not permanent braking behind a moving body.");
            else Assert.That(maxGap,Is.LessThan(0),"Legacy behavior and an unmet reserve gate must not silently acquire the new passing intent.");
        }
        [TestCase(1)][TestCase(2)] public void NewMovingPassReplaysItsRecordedPhysicalCommandsWithoutAi(int version)
        {
            string resource=Path.GetFileNameWithoutExtension(ManualAiSteeringDefinition.ResourceForVersion(version));
            var source=ManualScopedFieldStepperTests.InitialCandidate(2,productionComposition:true,singleFileGrid:true,aiPolicyResource:resource);
            byte[] bytes;
            using(var stream=new MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(stream,source.stepper,SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json")) ))
                {
                    source.stepper.CommandObserver=writer;
                    for(int i=0;i<200;i++)
                    {var a=source.runner.Field.GetAthlete(0);var tick=a.Tick.Next();source.runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,150),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0)),0);}
                    writer.Complete();Assert.That(writer.Failure,Is.Null);
                }
                bytes=stream.ToArray();
            }
            var replay=ManualScopedFieldStepperTests.InitialCandidate(2,productionComposition:true,singleFileGrid:true,aiPolicyResource:resource);
            using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,replay.stepper,200))while(reader.HasNext)reader.Step();
            for(int i=0;i<2;i++)
            {
                Assert.That(replay.stepper.World.GetActor(i).Pose,Is.EqualTo(source.stepper.World.GetActor(i).Pose));
                Assert.That(replay.stepper.World.GetActor(i).Velocity,Is.EqualTo(source.stepper.World.GetActor(i).Velocity));
                Assert.That(replay.stepper.World.GetCursor(i).CreditedArcNanometers,Is.EqualTo(source.stepper.World.GetCursor(i).CreditedArcNanometers));
                Assert.That(replay.runner.Field.GetAthlete(i).LastFrame,Is.EqualTo(source.runner.Field.GetAthlete(i).LastFrame));
                Assert.That(replay.runner.Field.GetAthlete(i).PhysiologyState,Is.EqualTo(source.runner.Field.GetAthlete(i).PhysiologyState));
                Assert.That(replay.stepper.World.GetAcceptedImpact(i),Is.EqualTo(source.stepper.World.GetAcceptedImpact(i)));
            }
        }
    }
}

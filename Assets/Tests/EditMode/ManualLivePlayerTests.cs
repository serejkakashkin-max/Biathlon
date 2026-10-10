using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualLivePlayerTests
    {
        [Test] public void SignedPlayerTurnAndReleasePublishActualWorldPoseInsteadOfTheRouteTangent()
        {
            var f=ManualScopedFieldStepperTests.InitialCandidate(1,true);var field=f.runner.Field;
            var id=field.GetAthlete(0).AthleteId;
            for(int i=0;i<100;i++)Step(0);
            long initial=f.stepper.World.GetActor(0).Pose.HeadingNanoradians;
            for(int i=0;i<8;i++)Step(250);
            var previous=RacePresentationSnapshot.Capture(field);Step(0);
            var current=RacePresentationSnapshot.Capture(field);var actual=f.stepper.World.GetActor(0);
            Assert.That(f.runner.GetLastManualSteering(0),Is.Zero,"Released steering remains released.");
            Assert.That(actual.Pose.HeadingNanoradians,Is.Not.EqualTo(initial));
            Assert.That(current.Get(0).WorldPose,Is.EqualTo(actual.Pose));
            var mid=RacePresentationSampling.WorldPose(previous,current,0,.5);
            Assert.That(mid.X,Is.EqualTo((previous.Get(0).WorldPose.X+actual.Pose.X)*.5).Within(1e-9));
            Assert.That(mid.Z,Is.EqualTo((previous.Get(0).WorldPose.Z+actual.Pose.Z)*.5).Within(1e-9));
            Assert.That(RacePresentationSampling.WorldPose(previous,current,0,1).HeadingNanoradians,Is.EqualTo(actual.Pose.HeadingNanoradians));
            Assert.That(field.Tick.Value,Is.EqualTo(109));
            void Step(int yaw)
            {
                var tick=field.Tick.Next();f.runner.StepWithManualPlayer(0,new SprintAthleteInput(
                    new TimeTrialInput(new SimulationInput(tick,id,650),1,1,0),new WeaponAimCommand(tick,id,0,0)),yaw);
            }
        }
    }
}

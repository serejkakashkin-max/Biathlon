using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DraftFeedbackTests
    {
        [Test]
        public void FeedbackPowerMatchesAuthoritativeDragDifferenceIncludingPosture()
        {
            var track = DraftingTests.Flat(); var follower = DraftingTests.Participant("f", 10); var leader = new[] { DraftingTests.Participant("l", 12) };
            var draft = new DraftingSystem(DraftingTests.Definition()).Evaluate(follower, leader, track);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            foreach (double posture in new[] { 1.0, TuckTests.Definition().DragAreaMultiplier })
            {
                var baseline = forces.Step(follower.Motion, 350, track, dragAreaMultiplier: posture);
                var sheltered = forces.Step(follower.Motion, 350, track, dragAreaMultiplier: posture * draft.DragAreaMultiplier);
                var feedback = new DraftFeedback(draft, track.DraftAllowed, follower.Motion, baseline);
                Assert.That(feedback.Status, Is.EqualTo(DraftFeedbackStatus.Sheltered)); Assert.That(feedback.LeaderId.Value, Is.EqualTo("l"));
                Assert.That(feedback.EstimatedMechanicalWattsSaved, Is.EqualTo((baseline.AirDragNewtons - sheltered.AirDragNewtons) * follower.Motion.SpeedMetersPerSecond).Within(1e-10));
                Assert.That(feedback.OffsetFromWakeMeters, Is.EqualTo(draft.LateralWakeOffsetMeters));
            }
        }
        [Test]
        public void MissingLeaderAndProhibitedZoneCannotAdvertiseSavings()
        {
            var p = DraftingTests.Participant("a", 10); var track = DraftingTests.Flat();
            var force = new SkiForceSystem(SkiForceTests.Definition(), 20).Step(p.Motion, 350, track);
            var empty = new DraftFeedback(default, true, p.Motion, force);
            Assert.That(empty.Status, Is.EqualTo(DraftFeedbackStatus.NoLeader)); Assert.That(empty.EstimatedMechanicalWattsSaved, Is.Zero);
            var draft = new DraftingSystem(DraftingTests.Definition()).Evaluate(p, new[] { DraftingTests.Participant("b", 12) }, track);
            var forbidden = new DraftFeedback(draft, false, p.Motion, force);
            Assert.That(forbidden.Status, Is.EqualTo(DraftFeedbackStatus.ProhibitedZone)); Assert.That(forbidden.Strength, Is.Zero);
            Assert.That(forbidden.LeaderId.IsValid, Is.False); Assert.That(forbidden.EstimatedMechanicalWattsSaved, Is.Zero);
        }
        [Test]
        public void FeedbackConstructionDoesNotAllocateOrChangeAuthoritativeState()
        {
            var p = DraftingTests.Participant("a", 10); var track = DraftingTests.Flat();
            var force = new SkiForceSystem(SkiForceTests.Definition(), 20).Step(p.Motion, 350, track);
            var draft = new DraftingSystem(DraftingTests.Definition()).Evaluate(p, new[] { DraftingTests.Participant("b", 12) }, track);
            double sum = 0; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += new DraftFeedback(draft, true, p.Motion, force).EstimatedMechanicalWattsSaved;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(sum, Is.GreaterThan(0)); Assert.That(p.Motion.DistanceMeters, Is.EqualTo(10));
        }
    }
}

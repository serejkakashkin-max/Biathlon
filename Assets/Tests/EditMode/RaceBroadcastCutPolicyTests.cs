using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class RaceBroadcastCutPolicyTests
    {
        [Test]public void DenseOvertakesHoldTheShotButFinishCanInterrupt()
        {
            var policy=new RaceBroadcastCutPolicy(20,8);
            Assert.That(policy.TrySelect(new RaceHighlight(100,"a",RaceHighlightKind.Overtake)),Is.True);
            Assert.That(policy.TrySelect(new RaceHighlight(101,"b",RaceHighlightKind.Overtake)),Is.False);Assert.That(policy.ShotId,Is.EqualTo("wide"));
            Assert.That(policy.TrySelect(new RaceHighlight(102,"b",RaceHighlightKind.Finish)),Is.True);Assert.That(policy.ShotId,Is.EqualTo("finish"));
            Assert.That(policy.TrySelect(new RaceHighlight(103,"c",RaceHighlightKind.FinalRange)),Is.False);
            Assert.That(policy.TrySelect(new RaceHighlight(262,"c",RaceHighlightKind.FinalRange)),Is.True);Assert.That(policy.ShotId,Is.EqualTo("range"));
        }
        [Test]public void BackwardSeekResetsTheCameraHold()
        {
            var policy=new RaceBroadcastCutPolicy(20,8);policy.TrySelect(new RaceHighlight(500,"a",RaceHighlightKind.CloseFinish));policy.Reset();
            Assert.That(policy.TrySelect(new RaceHighlight(100,"b",RaceHighlightKind.Overtake)),Is.True);Assert.That(policy.UntilTick,Is.EqualTo(260));
        }
    }
}

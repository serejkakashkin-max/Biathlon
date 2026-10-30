using System;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CheckpointHistorySelectionTests
    {
        [Test]public void NoArrivalsHasNoSelectionAndNavigationCannotInventData()
        {
            var h=new CheckpointHistorySelection();h.Observe(0,-1);
            Assert.That(h.Selected,Is.EqualTo(-1));Assert.That(h.Move(-1)||h.Move(1),Is.False);
            h.Observe(10,0);Assert.That(h.Selected,Is.Zero);Assert.That(h.Move(-1),Is.False);
        }
        [Test]public void BrowsingKeepsTheSelectedOccurrenceWhileNewSplitsArrive()
        {
            var h=new CheckpointHistorySelection();h.Observe(10,2);h.Move(-1);
            Assert.That(h.Selected,Is.EqualTo(1));Assert.That(h.FollowsLatest,Is.False);
            h.Observe(20,4);Assert.That(h.Selected,Is.EqualTo(1));Assert.That(h.Latest,Is.EqualTo(4));
            h.Observe(20,4);Assert.That(h.Selected,Is.EqualTo(1));
            h.FollowLatest();Assert.That(h.Selected,Is.EqualTo(4));h.Observe(21,5);Assert.That(h.Selected,Is.EqualTo(5));
        }
        [Test]public void NavigationClampsAtTheFirstAndReachingLatestResumesFollowing()
        {
            var h=new CheckpointHistorySelection();h.Observe(10,2);h.Move(-1);h.Move(-1);
            Assert.That(h.Selected,Is.Zero);Assert.That(h.Move(-1),Is.False);
            h.Move(1);h.Move(1);Assert.That(h.FollowsLatest,Is.True);Assert.That(h.Move(1),Is.False);
            h.Observe(11,3);Assert.That(h.Selected,Is.EqualTo(3));
        }
        [TestCase(5,2)][TestCase(11,0)]public void RewindOrNewRaceRebasesSelectionWithoutFutureResults(long tick,int latest)
        {
            var h=new CheckpointHistorySelection();h.Observe(10,3);h.Move(-1);h.Observe(tick,latest);
            Assert.That(h.Selected,Is.EqualTo(latest));Assert.That(h.FollowsLatest,Is.True);
            h.Reset();Assert.That(h.Selected,Is.EqualTo(-1));
        }
        [Test]public void InvalidObservationsAndDirectionsAreRejected()
        {
            var h=new CheckpointHistorySelection();Assert.Throws<ArgumentOutOfRangeException>(()=>h.Observe(-1,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>h.Observe(0,-2));Assert.Throws<ArgumentOutOfRangeException>(()=>h.Move(0));
        }
    }
}

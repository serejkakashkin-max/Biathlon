using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualRibbonWindowCacheTests
    {
        private static ManualSkiWorldTrajectory Stationary(OccupancyDefinition body,ManualSkiHeadingState pose)
        {
            var velocity=new SkiPlanarVelocity(0,0);
            var skier=new ManualSkiContactBody(new AthleteId("window_cache_probe"),pose,velocity,75);
            return new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(body,1)
                .PreviewTrajectory(skier,new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),0,0,.05));
        }
        [Test]
        public void RepeatedOwnedContactWindowReusesGeometryWithoutAllocating()
        {
            var f=new ManualRacePathCatalogTests.Fixture();
            int index=f.Catalog.GetBaseFlowSuccessor(f.Catalog.StartApproachIndex);
            var pose=f.At(index,60*SkiState.NanometersPerMeter);
            Assert.That(f.Catalog.TryProject(pose,index,60*SkiState.NanometersPerMeter,1,out var at),Is.True);
            var source=f.Catalog.GetGeometryObservation(at);
            var sweep=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            var trajectory=Stationary(f.Venue.Occupancy,pose);
            var expected=sweep.Preview(trajectory,source);
            Assert.That(expected.Status,Is.EqualTo(ManualRouteTubeStatus.Clear));
            for(int i=0;i<64;i++)sweep.Preview(trajectory,source);
            long before=GC.GetAllocatedBytesForCurrentThread();bool equal=true;
            for(int i=0;i<256;i++)
            {
                var actual=sweep.Preview(trajectory,source);
                equal&=actual.Status==expected.Status&&actual.IntervalChecks==expected.IntervalChecks;
            }
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(equal,Is.True);Assert.That(allocated,Is.Zero);
            Assert.That(sweep.GeometryBuildCount,Is.EqualTo(1),"Repeated contact refinements share the exact same authored polygon.");
        }
        [Test]
        public void CachedProofMatchesFreshGeometryAcrossTravelRangePenaltyAndReturn()
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:18);
            var cached=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
            int main=f.Catalog.GetBaseFlowSuccessor(f.Catalog.StartApproachIndex);
            int[] windows={f.Catalog.StartApproachIndex,main,f.Catalog.GetFiringOccurrence(0),
                f.Catalog.GetBaseFlowSuccessor(f.Catalog.GetFiringOccurrence(0)),f.Catalog.GetPenaltyOccurrence(0,1),
                f.Catalog.GetPenaltyReturnOccurrence(0),main,f.Catalog.StartApproachIndex};
            foreach(int index in windows)
            {
                long station=f.Catalog.Get(index).LengthNanometers/2;
                for(int side=0;side<3;side++)
                {
                    var pose=f.At(index,station,side*.2);
                    Assert.That(f.Catalog.TryProject(pose,index,station,1,out var at),Is.True);
                    var source=f.Catalog.GetGeometryObservation(at);var trajectory=Stationary(f.Venue.Occupancy,pose);
                    var fresh=new ManualRouteRibbonSweep(f.Catalog.ProjectionGeometry,f.Venue.Occupancy,ManualScopedFieldStepperTests.Work());
                    var expected=fresh.Preview(trajectory,source);var actual=cached.Preview(trajectory,source);
                    Assert.That(actual.Status,Is.EqualTo(expected.Status));
                    Assert.That(actual.IntervalChecks,Is.EqualTo(expected.IntervalChecks));
                    Assert.That(cached.LastStartClearance,Is.EqualTo(fresh.LastStartClearance));
                    Assert.That(cached.LastMinimumSampledClearance,Is.EqualTo(fresh.LastMinimumSampledClearance));
                    Assert.That(cached.LastVertexCount,Is.EqualTo(fresh.LastVertexCount));
                }
            }
            Assert.That(cached.GeometryBuildCount,Is.GreaterThan(1).And.LessThan(windows.Length*3));
        }
    }
}

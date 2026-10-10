using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class WoodlandFinishLayoutTests
    {
        [TestCase("mass_start")][TestCase("sprint")][TestCase("pursuit")]
        public void FinalLapHasSeparateStraightFinishAndRunoutOutsideThePenaltyOval(string format)
        {
            var f=new ManualRacePathCatalogTests.Fixture(format);var route=f.Route;
            var last=route.GetSection(route.SectionCount-1);var finish=f.Venue.Visual.Get(last.LoopId);
            var penalty=f.Venue.Visual.Get(f.Venue.Track.Penalty.LoopId);
            Assert.That(last.LoopId,Is.EqualTo("finish_spur"));
            double end=last.LoopEnd.Meters;var line=finish.Sample(end);
            Assert.That(finish.Get(finish.Count-1).Distance-end,Is.GreaterThanOrEqualTo(30));
            var straight=finish.Sample(end-50);
            Assert.That(Math.Abs(straight.ForwardX-line.ForwardX)+Math.Abs(straight.ForwardZ-line.ForwardZ),Is.LessThan(1e-10));
            Assert.That(Math.Sqrt(Math.Pow(line.X-straight.X,2)+Math.Pow(line.Z-straight.Z,2)),Is.EqualTo(50).Within(.001));
            Assert.That(new TrackSampler(f.Venue.Track.GetLoop(last.LoopId)).Sample(last.LoopEnd).WidthMeters,Is.GreaterThanOrEqualTo(9));
            double minimum=double.PositiveInfinity;
            for(double d=0;d<=finish.Get(finish.Count-1).Distance;d+=1)
            {
                var p=finish.Sample(d);
                for(int i=1;i<penalty.Count;i++)
                {
                    var a=penalty.Get(i-1);var b=penalty.Get(i);double dx=b.X-a.X,dz=b.Z-a.Z;
                    double t=Math.Max(0,Math.Min(1,((p.X-a.X)*dx+(p.Z-a.Z)*dz)/(dx*dx+dz*dz)));
                    minimum=Math.Min(minimum,Math.Sqrt(Math.Pow(p.X-a.X-t*dx,2)+Math.Pow(p.Z-a.Z-t*dz,2)));
                }
            }
            Assert.That(minimum,Is.GreaterThan(9),"Finish approach and braking corridor must not cross the penalty trail.");
            var start=f.Venue.Visual.Get("main").Sample(0);
            Assert.That(Math.Sqrt(Math.Pow(line.X-start.X,2)+Math.Pow(line.Z-start.Z,2)),Is.GreaterThan(100));
            TestContext.WriteLine($"{format} penalty_centerline_clearance_m={minimum:F2} finish=({line.X:F2},{line.Z:F2})");
        }
    }
}

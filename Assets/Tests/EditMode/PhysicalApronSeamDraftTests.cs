using System;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PhysicalApronSeamDraftTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private sealed class Fixture
        {
            public TrackDefinition Track; public TrackVisualDefinition Visual; public RaceRoute Route;
            public PhysicalTrackMap Physical; public PhysicalRangeFieldLayout Layout; public OccupancyDefinition Body;
            public PhysicalSharedApronDefinition Definition; public PhysicalSharedApronMapV2 Map;
        }
        private static Fixture Load()
        {
            var dir = PhysicalApronDraftFixtureFiles.Root();
            string Read(string id) => File.ReadAllText(Path.Combine(dir.FullName, "candidate/resources", id + ".json"));
            const string V = "Biathlon/Venues/woodland_basin_v1/"; var f = new Fixture();
            f.Track = TrackDefinition.FromJson(Read(V + "track")); f.Visual = TrackVisualDefinition.FromJson(Read(V + "visual"), f.Track);
            var profile = CompetitionProfile.FromJson(Read(V + "mass_start_profile")); var j = PhysicalJunctionDefinition.FromJson(Read(V + "junctions"), f.Track);
            var c = PhysicalConnectionDefinition.FromJson(Read(V + "connections"), j); var paths = PhysicalRangePathDefinition.FromJson(Read(V + "paths"), f.Track, j, c);
            f.Route = new RaceRoute(f.Track, RaceRoutePlan.FromJson(Read(V + "mass_start_route")), profile.ToRouteRequirements(), j, c);
            f.Physical = new PhysicalTrackMap(f.Track, f.Route); f.Layout = new PhysicalRangeFieldLayout(f.Physical, f.Track, f.Route, paths);
            f.Body = OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            f.Definition = PhysicalSharedApronDefinition.FromJson(File.ReadAllText(Path.Combine(dir.FullName, "candidate/apron.json")), f.Track, f.Visual, paths, j, c, f.Body, Read);
            f.Map = new PhysicalSharedApronMapV2(f.Physical, f.Track, f.Route, profile, f.Layout, f.Visual, j, c, f.Body, f.Definition, .05, 2 * M);
            return f;
        }
        private static PhysicalApronSeamProjectionDraft Seam(Fixture f, int path, int occurrence, string opponent)
        {
            long at = f.Map.SeamRoute(path, occurrence); var p = f.Layout.GetPath(path);
            return new PhysicalApronSeamProjectionDraft(f.Physical, f.Definition, f.Body,
                p.MapRoute(at, PhysicalBoundarySide.Incoming), p.MapRoute(at), opponent);
        }
        private static PhysicalOccupancySweep Sweep(Fixture f, string loop, double at, double travel = 0, double line = 0)
            => new PhysicalOccupancySweep(new AthleteId("other"), f.Physical.MapLoop(loop, (long)Math.Round(at * M)), (long)Math.Round(travel * M), line, line);
        [Test] public void ActualSixtyPrivateSeamsContainEveryIndependentRotatingCornerAndLateralOffset()
        {
            var f = Load(); int queries = 0; double violation = 0;
            for (int p = 0; p < 30; p++) for (int seamIndex = 0; seamIndex < 2; seamIndex++)
            {
                var seam = Seam(f, p, seamIndex, "mat_01"); long at = f.Map.SeamRoute(p, seamIndex); var path = f.Layout.GetPath(p);
                var a = path.MapRoute(at, PhysicalBoundarySide.Incoming); var b = path.MapRoute(at); double sa = a.LocalNanometers / (double)M, sb = b.LocalNanometers / (double)M;
                var source = f.Visual.Get(a.LoopId); var target = f.Visual.Get(b.LoopId);
                // Evaluate incident directions independently from native world samples just
                // inside each positive edge, without copying the production support algebra.
                var start = source.Sample(sa); var before = source.Sample(sa - .000001); var end = target.Sample(sb); var after = target.Sample(sb + .000001);
                double angle = Math.Atan2(start.Z - before.Z, start.X - before.X), targetAngle = Math.Atan2(after.Z - end.Z, after.X - end.X);
                double turn = Math.Atan2(Math.Sin(targetAngle - angle), Math.Cos(targetAngle - angle));
                foreach (double lateral in new[] { -.85, 0, .85 }) for (int axis = 0; axis < 36; axis++)
                {
                    double ax = Math.Cos(axis * Math.PI / 18), az = Math.Sin(axis * Math.PI / 18);
                    seam.Bounds(lateral, lateral, ax, az, out double lo, out double hi);
                    for (int sample = 0; sample <= 128; sample++)
                    {
                        double phase = sample / 128d, heading = angle + turn * phase, fx = Math.Cos(heading), fz = Math.Sin(heading);
                        double x = start.X + (end.X - start.X) * phase, z = start.Z + (end.Z - start.Z) * phase;
                        for (int along = -1; along <= 1; along += 2) for (int across = -1; across <= 1; across += 2)
                        {
                            double offset = lateral + across * .35;
                            double projected = (x + along * .75 * fx + offset * fz) * ax + (z + along * .75 * fz - offset * fx) * az;
                            violation = Math.Max(violation, Math.Max(lo - projected, projected - hi)); queries++;
                        }
                    }
                }
            }
            Assert.That(queries, Is.EqualTo(3343680)); Assert.That(violation, Is.LessThanOrEqualTo(5e-8), "Independent finite-edge direction estimate includes floating subtraction error.");
            TestContext.WriteLine("Actual seam corner projections=" + queries + "; maximum numerical witness violation=" + violation.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        }
        [Test] public void RealNeighborEntryAndDepartureKeepWholeSeamRotationWithoutGlobalMatLock()
        {
            var f = Load(); var a = Seam(f, 28, 0, "mat_30"); var b = Sweep(f, "mat_30", 4.704);
            Assert.That(a.Conflicts(0, 0, b), Is.False);
            var otherEntry = Seam(f, 29, 0, "mat_29");
            Assert.That(a.Conflicts(0, 0, otherEntry, 0, 0), Is.False);
            // Neighbor's exit tangent shares a physical stem with the incoming skier.
            var exit = Seam(f, 29, 1, "exit_aisle");
            Assert.That(exit.Conflicts(0, 0, Sweep(f, "exit_aisle", 11, .1)), Is.True);
            Assert.That(a.ProjectionMarginMeters, Is.GreaterThanOrEqualTo(.01));
        }
        [Test] public void UnknownOutOfWindowForeignAndDefaultBodyCannotUseSeamSupport()
        {
            var f = Load(); var seam = Seam(f, 0, 0, "mat_02");
            Assert.That(seam.Conflicts(0, 0, default), Is.True);
            Assert.That(seam.Conflicts(0, 0, Sweep(f, "main", 2769)), Is.True);
            var foreign = new PhysicalTrackMap(f.Track, f.Route);
            Assert.That(seam.Conflicts(0, 0, new PhysicalOccupancySweep(new AthleteId("other"), foreign.MapLoop("mat_02", 4704000000), 0, 0, 0)), Is.True);
            Assert.That(() => new PhysicalApronSeamProjectionDraft(f.Physical, f.Definition, f.Body, f.Physical.MapLoop("main", 2820 * M), f.Physical.MapLoop("mat_01", 0), "mat_02"), Throws.ArgumentException);
            Assert.That(() => new PhysicalApronSeamProjectionDraft(f.Physical, f.Definition, f.Body, f.Physical.MapLoop("main", 2800 * M), f.Physical.MapLoop("mat_01", 0), "unknown"), Throws.ArgumentException);
        }
        [Test] public void SeamRotationHotQueriesAllocateNoManagedMemory()
        {
            var f = Load(); var seam = Seam(f, 28, 0, "mat_30"); var other = Sweep(f, "mat_30", 4.704);
            for (int i = 0; i < 50; i++) seam.Conflicts(0, 0, other);
            long before = GC.GetAllocatedBytesForCurrentThread(); bool conflict = false;
            for (int i = 0; i < 1000; i++) conflict |= seam.Conflicts(0, 0, other);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(conflict, Is.False);
        }
        [Test] public void ActualProjectionOwnerAndExactAcceptedOccurrenceHooksBind()
        {
            var f = Load(); var projection = new PhysicalMotionProjection(f.Physical, .05, rangePath: f.Layout.GetPath(0));
            Assert.That(f.Map.CreateTrace(0, new AthleteId("a"), 4, projection).Projection, Is.SameAs(projection));
            Assert.That(() => f.Map.CreateTrace(1, new AthleteId("a"), 4, projection), Throws.ArgumentException);
            Assert.That(() => f.Map.CreateTrace(0, new AthleteId("a"), 4, new PhysicalMotionProjection(f.Physical, .1, rangePath: f.Layout.GetPath(0))), Throws.ArgumentException);
            var tail = f.Map.CreateTail(0, 0, 0);
            Assert.That(f.Map.FindSeamOccurrence(0, tail.ConnectionIndex, tail.SeamRouteNanometers, tail.Point), Is.EqualTo(0));
            Assert.That(f.Map.FindSeamOccurrence(1, tail.ConnectionIndex, tail.SeamRouteNanometers, tail.Point), Is.EqualTo(-1));
            Assert.That(f.Map.FindSeamOccurrence(0, tail.ConnectionIndex, tail.SeamRouteNanometers + 1, tail.Point), Is.EqualTo(-1));
        }
    }
}

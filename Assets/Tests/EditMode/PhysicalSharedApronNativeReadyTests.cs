using System;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PhysicalSharedApronNativeReadyTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private sealed class Fixture
        {
            public string Root, Json;
            public TrackDefinition Track;
            public TrackVisualDefinition Visual;
            public CompetitionProfile Profile;
            public RaceRoute Route;
            public PhysicalTrackMap Physical;
            public PhysicalJunctionDefinition Junctions;
            public PhysicalConnectionDefinition Connections;
            public PhysicalRangePathDefinition Paths;
            public PhysicalRangeFieldLayout Layout;
            public OccupancyDefinition Body;
            public PhysicalSharedApronDefinition Definition;
            public PhysicalSharedApronMapV2 Map;
            public string Read(string id) => File.ReadAllText(Path.Combine(Root, "candidate/resources", id + ".json"));
        }
        private static Fixture Load()
        {
            var dir = PhysicalApronDraftFixtureFiles.Root();
            var f = new Fixture { Root = dir.FullName }; const string V = "Biathlon/Venues/woodland_basin_v1/";
            f.Track = TrackDefinition.FromJson(f.Read(V + "track")); f.Visual = TrackVisualDefinition.FromJson(f.Read(V + "visual"), f.Track);
            f.Profile = CompetitionProfile.FromJson(f.Read(V + "mass_start_profile"));
            f.Junctions = PhysicalJunctionDefinition.FromJson(f.Read(V + "junctions"), f.Track);
            f.Connections = PhysicalConnectionDefinition.FromJson(f.Read(V + "connections"), f.Junctions);
            f.Paths = PhysicalRangePathDefinition.FromJson(f.Read(V + "paths"), f.Track, f.Junctions, f.Connections);
            f.Route = new RaceRoute(f.Track, RaceRoutePlan.FromJson(f.Read(V + "mass_start_route")), f.Profile.ToRouteRequirements(), f.Junctions, f.Connections);
            f.Physical = new PhysicalTrackMap(f.Track, f.Route); f.Layout = new PhysicalRangeFieldLayout(f.Physical, f.Track, f.Route, f.Paths);
            f.Body = OccupancyDefinition.FromJson(f.Read("Biathlon/occupancy_provisional_v1"));
            f.Json = File.ReadAllText(Path.Combine(f.Root, "candidate/apron.json")); f.Definition = Bind(f, f.Json); f.Map = Map(f, f.Definition);
            return f;
        }
        private static PhysicalSharedApronDefinition Bind(Fixture f, string json) => PhysicalSharedApronDefinition.FromJson(json,
            f.Track, f.Visual, f.Paths, f.Junctions, f.Connections, f.Body, f.Read);
        private static PhysicalSharedApronMapV2 Map(Fixture f, PhysicalSharedApronDefinition d, PhysicalRangeFieldLayout layout = null,
            PhysicalTrackMap physical = null, OccupancyDefinition body = null, TrackVisualDefinition visual = null, CompetitionProfile profile = null, long reserve = 2 * M, double seconds = .05)
            => new PhysicalSharedApronMapV2(physical ?? f.Physical, f.Track, f.Route, profile ?? f.Profile, layout ?? f.Layout, visual ?? f.Visual,
                f.Junctions, f.Connections, body ?? f.Body, d, seconds, reserve);
        private static void Set(object target, string property, object value)
            => target.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(target, value);
        private static string Mutate(Fixture f, Action<PhysicalSharedApronDefinition> action)
        { var d = Bind(f, f.Json); action(d); return d.ToJson(); }
        private static string Identity(Fixture f, string member, string value)
        {
            string replacement = member == "schema_version" ? value : "\"" + value + "\"";
            return new Regex("(\"" + member + "\":)(?:\"[^\"]*\"|[0-9]+)").Replace(f.Definition.ToJson(), "$1" + replacement, 1);
        }
        private static int Region(Fixture f, string id)
        {
            for (int j = 0; j < f.Junctions.Count; j++) if (f.Junctions.Get(j).Id == id) return f.Map.FindRegionForJunction(j);
            return -1;
        }
        private static PhysicalOccupancySweep Sweep(Fixture f, string id, string loop, double at, double travel = 0, double before = 0, double after = 0)
            => new PhysicalOccupancySweep(new AthleteId(id), f.Physical.MapLoop(loop, (long)Math.Round(at * M)), (long)Math.Round(travel * M), before, after);
        private static RaceMotionSegment Segment(long from, long to, double start = 0, double end = .05, long travelBefore = 0)
            => (RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment), BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { from, to, travelBefore, travelBefore + to - from, start, end, false }, null);
        private static PhysicalApronMotionV2 Motion(Fixture f, PhysicalApronTraceV2 trace, int path, long from, long to, double start = 0, double end = .05, long travel = 0)
        { var s = Segment(from, to, start, end, travel); return new PhysicalApronMotionV2(s, trace.Projection.Project(s, f.Layout.GetPath(path).MapRoute(from))); }
        private static PhysicalApronTraceV2 Stationary(Fixture f, int path, string id, double line = 0, double offset = 0)
        {
            var t = f.Map.CreateTrace(path, new AthleteId(id), 4); long at = f.Layout.GetPath(path).FiringRouteNanometers(0) + (long)Math.Round(offset * M);
            var p = f.Layout.GetPath(path).MapRoute(at); t.Prepare(at, at, p, p, line, line, default, default, Array.Empty<PhysicalApronMotionV2>()); return t;
        }

        [Test] public void CompleteCandidateRoundTripsAllGuardsIncludingThreePortsOnTwoLoops()
        {
            var f = Load(); var d = Bind(f, f.Definition.ToJson());
            // Mono can parse serialized -0 as +0. Compare every exact numeric/identity
            // value, then require stable subsequent serialization; authoritative snapshot
            // and context byte comparisons below remain unchanged.
            EqualApronValues(f.Definition,d);
            Assert.That(Bind(f,d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
            Assert.That(d.CorridorCount, Is.EqualTo(32)); Assert.That(d.RegionCount, Is.EqualTo(120));
            Assert.That(d.TrackSnapshotJson, Is.EqualTo(f.Track.ToJson())); Assert.That(d.VisualSnapshotJson, Is.EqualTo(f.Visual.ToJson()));
            Assert.That(d.RangePathsSnapshotJson, Is.EqualTo(f.Paths.ToJson())); Assert.That(d.ConnectionSnapshotJson, Is.EqualTo(f.Connections.ToJson()));
            Assert.That(d.JunctionSnapshotJson,Is.EqualTo(f.Junctions.ToJson())); Assert.That(d.OccupancySnapshotJson,Is.EqualTo(f.Body.ToJson()));
            var guards=PhysicalJunctionDefinition.FromJson(d.JunctionSnapshotJson,f.Track);
            Assert.That(guards.Count,Is.EqualTo(f.Junctions.Count));
            for(int j=0;j<guards.Count;j++)
            {
                var actual=guards.Get(j);var expected=f.Junctions.Get(j);
                Assert.That(actual.Id,Is.EqualTo(expected.Id));Assert.That(actual.PortCount,Is.EqualTo(expected.PortCount));
                for(int p=0;p<actual.PortCount;p++)
                {
                    var a=actual.GetPort(p);var e=expected.GetPort(p);
                    Assert.That(a.Id,Is.EqualTo(e.Id));Assert.That(a.Anchor.LoopId,Is.EqualTo(e.Anchor.LoopId));
                    Assert.That(a.Anchor.Distance.Millimeters,Is.EqualTo(e.Anchor.Distance.Millimeters));
                    Assert.That(a.GuardBefore.Millimeters,Is.EqualTo(e.GuardBefore.Millimeters));Assert.That(a.GuardAfter.Millimeters,Is.EqualTo(e.GuardAfter.Millimeters));
                }
            }
            int r = Region(f, "apron_body_main_exit_aisle"); Assert.That(f.Map.IsEligible(r), Is.True);
            Assert.That(f.Junctions.Get(d.GetRegion(r).JunctionIndex).PortCount, Is.EqualTo(3));
            Assert.That(f.Map.ContextJson, Does.Contain("optional_all_heading_body_envelope_without_lateral_centering"));
        }
        private static void EqualApronValues(PhysicalSharedApronDefinition expected,PhysicalSharedApronDefinition actual)
        {
            Assert.That(actual.SchemaVersion,Is.EqualTo(expected.SchemaVersion));Assert.That(actual.Id,Is.EqualTo(expected.Id));
            Assert.That(actual.TrackId,Is.EqualTo(expected.TrackId));Assert.That(actual.RangePathsId,Is.EqualTo(expected.RangePathsId));
            Assert.That(actual.OccupancyId,Is.EqualTo(expected.OccupancyId));Assert.That(actual.ValidationPolicyResource,Is.EqualTo(expected.ValidationPolicyResource));
            Assert.That(actual.ValidationPolicy.ToJson(),Is.EqualTo(expected.ValidationPolicy.ToJson()));
            Assert.That(actual.CorridorCount,Is.EqualTo(expected.CorridorCount));Assert.That(actual.RegionCount,Is.EqualTo(expected.RegionCount));
            Assert.That(actual.MatchesPresentation(TrackVisualDefinition.FromJson(actual.VisualSnapshotJson,TrackDefinition.FromJson(actual.TrackSnapshotJson))),Is.True);
            for(int c=0;c<actual.CorridorCount;c++)
            {
                var a=actual.GetCorridor(c);var e=expected.GetCorridor(c);
                Assert.That(a.LoopId,Is.EqualTo(e.LoopId));Assert.That(a.Count,Is.EqualTo(e.Count));
                Assert.That(a.Start,Is.EqualTo(e.Start));Assert.That(a.End,Is.EqualTo(e.End));
                for(int k=0;k<a.Count;k++)
                {
                    var p=a.Get(k);var q=e.Get(k);
                    Assert.That(p.Distance,Is.EqualTo(q.Distance));Assert.That(p.X,Is.EqualTo(q.X));
                    Assert.That(p.Y,Is.EqualTo(q.Y));Assert.That(p.Z,Is.EqualTo(q.Z));
                }
            }
            for(int r=0;r<actual.RegionCount;r++)
            {
                Assert.That(actual.GetRegion(r).JunctionId,Is.EqualTo(expected.GetRegion(r).JunctionId));
                Assert.That(actual.GetRegion(r).JunctionIndex,Is.EqualTo(expected.GetRegion(r).JunctionIndex));
            }
        }
        [TestCase("schema_version", "2")]
        [TestCase("track_id", "foreign")]
        [TestCase("range_paths_id", "foreign")]
        [TestCase("occupancy_definition_id", "foreign")]
        public void MalformedIdentityRejectsCold(string member, string value)
        { var f = Load(); Assert.That(() => Bind(f, Identity(f, member, value)), Throws.Exception); }
        [Test] public void DuplicateUnknownSelfAndEmptyRegionsCannotAuthorize()
        {
            var f = Load();
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetRegion(1), "JunctionId", d.GetRegion(0).JunctionId))), Throws.ArgumentException);
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetRegion(0), "JunctionId", "unknown"))), Throws.ArgumentException);
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetRegion(0), "JunctionId", "mat_turn_01"))), Throws.ArgumentException);
            Assert.That(() => Bind(f, Mutate(f, d => Set(d, "regions", Array.Empty<PhysicalSharedApronRegion>()))), Throws.Exception);
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetCorridor(1), "LoopId", d.GetCorridor(0).LoopId))), Throws.ArgumentException);
        }
        [Test] public void EveryPortCoverageIncludesTheSecondExitPortAndHalfBody()
        {
            var f = Load();
            string shortExit = Mutate(f, d => RemoveKnots(d.GetCorridor(1), 0, d.GetCorridor(1).Count - 1));
            Assert.That(() => Bind(f, shortExit), Throws.ArgumentException);
            string shortMain = Mutate(f, d => { var c = d.GetCorridor(0); int first = 0; while (c.Get(first).Distance < 2800) first++; RemoveKnots(c, first, c.Count); });
            Assert.That(() => Bind(f, shortMain), Throws.Exception);
        }
        private static void RemoveKnots(PhysicalPassageCorridor c, int first, int end)
        {
            var knots = new PhysicalCorridorKnot[end - first];
            for (int i = first; i < end; i++) knots[i - first] = c.Get(i);
            Set(c, "knots", knots);
        }
        [Test] public void BadMetricSubmillimetresAndHiddenWorldChangeReject()
        {
            var f = Load();
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetCorridor(2).Get(1), "Distance", .2941))), Throws.Exception);
            Assert.That(() => Bind(f, Mutate(f, d => Set(d.GetCorridor(2).Get(1), "Z", 100d))), Throws.ArgumentException);
            var changed = TrackVisualDefinition.FromJson(f.Visual.ToJson(), f.Track);
            var loop = changed.Get("main");
            for (int i = 0; i < loop.Count; i++) if (loop.Get(i).Distance > 2800 && loop.Get(i).Distance < 2810)
            { Set(loop.Get(i), "Z", loop.Get(i).Z + .1); break; }
            Assert.That(f.Definition.MatchesPresentation(changed), Is.False);
            Assert.That(() => Map(f, f.Definition, visual: changed), Throws.ArgumentException);
        }
        [Test] public void ExactTickClockBindsOptionalContext()
        {
            var f = Load(); var changed = Map(f, f.Definition, seconds: .1);
            Assert.That(changed.ContextJson, Is.Not.EqualTo(f.Map.ContextJson));
            Assert.That(changed.ContextJson, Does.Contain("0.1"));
        }
        [Test] public void SameIdBodyAndForeignPhysicalFamilyCannotSubstitute()
        {
            var f = Load(); string changedBody = f.Body.ToJson().Replace("1.4", "1.41");
            Assert.That(() => Map(f, f.Definition, body: OccupancyDefinition.FromJson(changedBody)), Throws.ArgumentException);
            var other = new PhysicalTrackMap(f.Track, f.Route); var otherLayout = new PhysicalRangeFieldLayout(other, f.Track, f.Route, f.Paths);
            Assert.That(() => Map(f, f.Definition, layout: otherLayout), Throws.ArgumentException);
            var sprint = CompetitionProfile.FromJson(f.Read("Biathlon/Venues/woodland_basin_v1/sprint_profile"));
            Assert.That(() => Map(f, f.Definition, profile: sprint), Throws.ArgumentException);
        }
        [Test] public void WholeTickHeadroomIsDerivedWithoutShrinkingGuardOrBody()
        {
            var f = Load(); var huge = Map(f, f.Definition, reserve: 100 * M);
            Assert.That(f.Map.IsEligible(Region(f, "fork_30")), Is.True); Assert.That(huge.IsEligible(Region(f, "fork_30")), Is.False);
            Assert.That(huge.Conflicts(Region(f, "fork_30"), Sweep(f, "a", "main", 2810), Sweep(f, "b", "mat_30", 1)), Is.True);
        }
        [Test] public void EveryThirtyMatBodyPairAtFullPrivateLateralExtremesSeparates()
        {
            var f = Load(); int r = Region(f, "apron_body_main_exit_aisle"), count = 0;
            foreach (double la in new[] { -.85, 0, .85 }) foreach (double lb in new[] { -.85, 0, .85 })
                for (int a = 1; a <= 30; a++) for (int b = a + 1; b <= 30; b++)
                { count++; Assert.That(f.Map.Conflicts(r, Sweep(f, "a", "mat_" + a.ToString("00"), 4.704, before: la, after: la), Sweep(f, "b", "mat_" + b.ToString("00"), 4.704, before: lb, after: lb)), Is.False); }
            Assert.That(count, Is.EqualTo(3915));
        }
        [Test] public void ScopedTraceDoesNotSerializeAdjacentStationaryPathBodies()
        {
            var f = Load(); var scratch = new PhysicalApronPairScratchV2(4);
            for (int p = 0; p < 29; p++)
            {
                int r = Region(f, "apron_body_mat_" + (p + 2).ToString("00") + "_mat_" + (p + 1).ToString("00"));
                Assert.That(f.Map.Conflicts(r, Stationary(f, p, "a"), Stationary(f, p + 1, "b"), scratch), Is.False);
            }
            Assert.That(f.Map.Conflicts(Region(f, "fork_30"), Stationary(f, 0, "a"), Stationary(f, 1, "b"), scratch), Is.True, "Unrelated region membership is never collective authority.");
            Assert.That(Region(f, "start_main_join"), Is.EqualTo(-1));
        }
        [Test] public void PaidTurnAndZeroForwardLateralSweepsIncludeTheWholeBody()
        {
            var f = Load(); int r = Region(f, "apron_body_mat_02_mat_01"); var b = Stationary(f, 1, "b"); var scratch = new PhysicalApronPairScratchV2(4);
            bool contact = false;
            foreach (double line in new[] { -.85, 0, .85 })
            {
                var a = f.Map.CreateTrace(0, new AthleteId("a"), 4); long from = f.Layout.GetPath(0).FiringRouteNanometers(0), to = from + 2352000000;
                a.Prepare(from, to, f.Layout.GetPath(0).MapRoute(from), f.Layout.GetPath(0).MapRoute(to), line, line, default, default, new[] { Motion(f, a, 0, from, to) });
                bool blocked = f.Map.Conflicts(r, a, b, scratch); if (line == 0) Assert.That(blocked, Is.False); else contact |= blocked;
            }
            Assert.That(contact, Is.True);
            var lateral = f.Map.CreateTrace(0, new AthleteId("lateral"), 4); long at = f.Layout.GetPath(0).FiringRouteNanometers(0) + 1176000000;
            var point = f.Layout.GetPath(0).MapRoute(at); lateral.Prepare(at, at, point, point, -.85, .85, default, default, Array.Empty<PhysicalApronMotionV2>());
            Assert.That(f.Map.Conflicts(r, lateral, b, scratch), Is.True);
        }
        [Test] public void CapturedNeighborCorridorCanUseTheActualPairCache()
        {
            var f = Load(); int r = Region(f, "fork_30");
            Assert.That(f.Map.Conflicts(r, Sweep(f, "a", "mat_29", 1), Sweep(f, "b", "mat_30", 4.704)), Is.False);
            // Actual retained Main tail and selected neighbor path retain complete seam support.
            var a = f.Map.CreateTrace(28, new AthleteId("a"), 4); long seam = f.Map.SeamRoute(28, 0), end = seam + 100000000;
            var motion = Motion(f, a, 28, seam, end); var tail = f.Map.CreateTail(28, 0, 0);
            a.Prepare(seam, end, tail.Point, f.Layout.GetPath(28).MapRoute(end), 0, 0, default, tail, new[] { motion });
            Assert.That(f.Map.Conflicts(r, a, Stationary(f, 29, "b"), new PhysicalApronPairScratchV2(4)), Is.False);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void AllOldNewRetainedTailCombinationsKeepExactCommonRejoinEvidence(bool aNew, bool bNew)
        {
            var f = Load(); int r = Region(f, "apron_body_main_exit_aisle"); var scratch = new PhysicalApronPairScratchV2(4);
            PhysicalApronTraceV2 Build(int path, string id, double line, bool newly)
            {
                var t = f.Map.CreateTrace(path, new AthleteId(id), 4); int occurrence = 2; long seam = f.Map.SeamRoute(path, occurrence), end = seam + 200000000;
                var tail = f.Map.CreateTail(path, occurrence, line); long start = newly ? seam : end;
                t.Prepare(start, end, newly ? tail.Point : f.Layout.GetPath(path).MapRoute(start), f.Layout.GetPath(path).MapRoute(end), line, line,
                    newly ? default : tail, newly ? tail : default, newly ? new[] { Motion(f, t, path, start, end) } : Array.Empty<PhysicalApronMotionV2>());
                return t;
            }
            Assert.That(f.Map.Conflicts(r, Build(0, "a", -1.3, aNew), Build(1, "b", 1.3, bNew), scratch), Is.False);
            Assert.That(f.Map.Conflicts(r, Build(0, "a", 0, aNew), Build(1, "b", 0, bNew), scratch), Is.True);
        }
        [Test] public void DefaultForeignOutOfWindowAndImpossibleLateralEvidenceStayBlocking()
        {
            var f = Load(); int r = Region(f, "apron_body_main_exit_aisle"); var b = Sweep(f, "b", "mat_02", 4.704);
            Assert.That(f.Map.Conflicts(r, default, b), Is.True);
            var foreign = new PhysicalTrackMap(f.Track, f.Route);
            Assert.That(f.Map.Conflicts(r, new PhysicalOccupancySweep(new AthleteId("foreign"), foreign.MapLoop("mat_01", 4704000000), 0, 0, 0), b), Is.True);
            Assert.That(f.Map.Conflicts(r, Sweep(f, "a", "main", 2769), b), Is.True);
            Assert.That(f.Map.Conflicts(r, Sweep(f, "a", "penalty_150", 1), b), Is.True);
            Assert.That(f.Map.Conflicts(r, Sweep(f, "a", "mat_01", 4.704, before: .851, after: .851), b), Is.True);
            Assert.That(f.Map.Conflicts(-1, Sweep(f, "a", "mat_01", 4.704), b), Is.True);
        }
        [Test] public void ReceiptTimingTravelOwnerAndTailOccurrenceCannotBeSubstituted()
        {
            var f = Load(); var t = f.Map.CreateTrace(0, new AthleteId("a"), 4); var other = f.Map.CreateTrace(0, new AthleteId("b"), 4);
            long at = f.Layout.GetPath(0).FiringRouteNanometers(0), end = at + 100000000;
            var m = Motion(f, t, 0, at, end); var start = f.Layout.GetPath(0).MapRoute(at); var finish = f.Layout.GetPath(0).MapRoute(end);
            void Prepare(PhysicalApronMotionV2[] motions, PhysicalApronTailV2 tail = default) => t.Prepare(at, end, start, finish, 0, 0, tail, default, motions);
            Assert.That(() => Prepare(new[] { new PhysicalApronMotionV2(Segment(at, end, .01, .05), m.Receipt) }), Throws.Exception);
            Assert.That(() => Prepare(new[] { Motion(f, other, 0, at, end) }), Throws.ArgumentException);
            Assert.That(() => Prepare(new[] { new PhysicalApronMotionV2(default, m.Receipt) }), Throws.Exception);
            Assert.That(() => Prepare(new[] { m }, f.Map.CreateTail(1, 0, 0)), Throws.ArgumentException);
            var foreign = Load(); Assert.That(() => Prepare(new[] { m }, foreign.Map.CreateTail(0, 0, 0)), Throws.ArgumentException);
            Assert.That(f.Map.Conflicts(Region(f, "apron_body_mat_02_mat_01"), t, Stationary(f, 1, "b"), new PhysicalApronPairScratchV2(4)), Is.True, "Failed prepare cannot leave reusable proof.");
        }
        [Test] public void WholeTimelineAllowsPaidStationaryGapButRejectsTravelDiscontinuity()
        {
            var f = Load(); var t = f.Map.CreateTrace(0, new AthleteId("a"), 4); long at = f.Layout.GetPath(0).FiringRouteNanometers(0), middle = at + 100000000, end = middle + 100000000;
            var first = Motion(f, t, 0, at, middle, 0, .01, 50 * M); var second = Motion(f, t, 0, middle, end, .03, .04, 50 * M + 100000000);
            t.Prepare(at, end, f.Layout.GetPath(0).MapRoute(at), f.Layout.GetPath(0).MapRoute(end), 0, 0, default, default, new[] { first, second });
            Assert.That(f.Map.Conflicts(Region(f, "apron_body_mat_02_mat_01"), t, Stationary(f, 1, "b"), new PhysicalApronPairScratchV2(4)), Is.False);
            var bad = Motion(f, t, 0, middle, end, .03, .04, 70 * M);
            Assert.That(() => t.Prepare(at, end, f.Layout.GetPath(0).MapRoute(at), f.Layout.GetPath(0).MapRoute(end), 0, 0, default, default, new[] { first, bad }), Throws.Exception);
        }
        [Test] public void ScopedCompletePairQueriesAllocateNoManagedMemory()
        {
            var f = Load(); var a = Stationary(f, 0, "a"); var b = Stationary(f, 1, "b"); int r = Region(f, "apron_body_mat_02_mat_01"); var scratch = new PhysicalApronPairScratchV2(4);
            for (int i = 0; i < 50; i++) f.Map.Conflicts(r, a, b, scratch);
            long before = GC.GetAllocatedBytesForCurrentThread(); bool blocked = false;
            for (int i = 0; i < 500; i++) blocked |= f.Map.Conflicts(r, a, b, scratch);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(blocked, Is.False); Assert.That(bytes, Is.Zero);
        }
        [Test] public void EveryHeadingAtAcceptedStopFitsTheUnchangedConfiguredBodyEnvelope()
        {
            var f = Load(); var values = (Array)typeof(PhysicalSharedApronMapV2).GetField("poseProjections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
            var support = values.GetValue(2, 3); var bounds = support.GetType().GetMethod("Bounds", BindingFlags.Instance | BindingFlags.NonPublic);
            var center = f.Visual.Get("mat_01").Sample(4.704); double maximum = 0; int count = 0;
            foreach (double lateral in new[] { -.85, 0, .85 }) for (int axis = 0; axis < 36; axis++)
            {
                var sweep = Sweep(f, "a", "mat_01", 4.704, before: lateral, after: lateral); double x = Math.Cos(axis * Math.PI / 18), z = Math.Sin(axis * Math.PI / 18);
                object[] args = { sweep, x, z, 0d, 0d }; bounds.Invoke(support, args); double low = (double)args[3], high = (double)args[4];
                // Keep the actual accepted center from the native path normal while the
                // body independently traverses every setup/shooting/recovery orientation.
                double cx = center.X + center.ForwardZ * lateral, cz = center.Z - center.ForwardX * lateral;
                for (int heading = 0; heading <= 128; heading++)
                {
                    double angle = heading * Math.PI / 64, fx = Math.Cos(angle), fz = Math.Sin(angle);
                    for (int along = -1; along <= 1; along += 2) for (int across = -1; across <= 1; across += 2)
                    {
                        double value = (cx + along * .75 * fx - across * .35 * fz) * x + (cz + along * .75 * fz + across * .35 * fx) * z;
                        maximum = Math.Max(maximum, Math.Max(low - value, value - high)); count++;
                    }
                }
            }
            Assert.That(count, Is.EqualTo(55728)); Assert.That(maximum, Is.LessThanOrEqualTo(1e-10));
        }
        [Test] public void AsymmetricAcceptedLineUsesActualVisibleRightNormalForOrdinaryAndPoseBodies()
        {
            var f = Load(); const double at = 2805.25, line = .61;
            int main = -1;
            for (int i = 0; i < f.Definition.CorridorCount; i++) if (f.Definition.GetCorridor(i).LoopId == "main") main = i;
            var sample = f.Visual.Get("main").Sample(at);
            var sweep = Sweep(f, "signed", "main", at, before: line, after: line);
            foreach (string cacheName in new[] { "projections", "poseProjections" })
            {
                var cache = (Array)typeof(PhysicalSharedApronMapV2).GetField(cacheName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
                var support = cache.GetValue(main, main); var bounds = support.GetType().GetMethod("Bounds", BindingFlags.Instance | BindingFlags.NonPublic);
                object[] args = { sweep, 0d, 1d, 0d, 0d }; bounds.Invoke(support, args);
                double actualCenter = sample.Z - sample.ForwardX * line;
                Assert.That(((double)args[3] + (double)args[4]) * .5, Is.EqualTo(actualCenter).Within(1e-10), cacheName);
                Assert.That(actualCenter, Is.LessThan(sample.Z));
                Assert.That((double)args[4], Is.LessThan(sample.Z + sample.ForwardX * line), "Mirrored positive line must not stand in for the visible center.");
            }
            Assert.That(f.Map.ContextJson, Does.Contain("world_center_right_normal_plus_fz_minus_fx"));
        }
        [Test] public void TargetFacingSetupProneStandingRecoveryDoNotCenterTheAcceptedLineOrLockNeighborStops()
        {
            var f = Load(); var scratch = new PhysicalApronPairScratchV2(4); int count = 0;
            for (int p = 0; p < 29; p++) foreach (double first in new[] { -.85, 0, .85 }) foreach (double second in new[] { -.85, 0, .85 })
            {
                var a = Stationary(f, p, "a", first); var b = Stationary(f, p + 1, "b", second);
                a.SetAllBodyHeadings(true); b.SetAllBodyHeadings(true);
                long ad = f.Layout.GetPath(p).FiringRouteNanometers(0), bd = f.Layout.GetPath(p + 1).FiringRouteNanometers(0);
                var ap = f.Layout.GetPath(p).MapRoute(ad); var bp = f.Layout.GetPath(p + 1).MapRoute(bd);
                a.Prepare(ad, ad, ap, ap, first, first, default, default, Array.Empty<PhysicalApronMotionV2>());
                b.Prepare(bd, bd, bp, bp, second, second, default, default, Array.Empty<PhysicalApronMotionV2>());
                int r = Region(f, "apron_body_mat_" + (p + 2).ToString("00") + "_mat_" + (p + 1).ToString("00"));
                Assert.That(f.Map.Conflicts(r, a, b, scratch), Is.False); count++;
            }
            Assert.That(count, Is.EqualTo(261));
        }
        [Test] public void RebindingReusesOwnedBuffersAndInvalidatesAnyEarlierProof()
        {
            var f = Load(); var a = Stationary(f, 0, "a"); var b = Stationary(f, 1, "b"); var scratch = new PhysicalApronPairScratchV2(4);
            var buffer = typeof(PhysicalApronTraceV2).GetField("Motions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);
            a.RebindPath(1, new PhysicalMotionProjection(f.Physical, .05, rangePath: f.Layout.GetPath(1)));
            Assert.That(typeof(PhysicalApronTraceV2).GetField("Motions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a), Is.SameAs(buffer));
            Assert.That(f.Map.Conflicts(Region(f, "apron_body_mat_02_mat_01"), a, b, scratch), Is.True);
            Assert.That(() => a.RebindPath(0, new PhysicalMotionProjection(f.Physical, .05, rangePath: f.Layout.GetPath(1))), Throws.ArgumentException);
        }
        [Test] public void ValidatedProductionReceiptCapacityIsNotTrimmedByAnotherArbitraryCeiling()
        {
            var f = Load(); const int capacity = 10001;
            var t = f.Map.CreateTrace(0, new AthleteId("a"), capacity);
            var buffer = (Array)typeof(PhysicalApronTraceV2).GetField("Motions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(t);
            Assert.That(buffer.Length, Is.EqualTo(capacity));
            Assert.That(new PhysicalApronPairScratchV2(capacity), Is.Not.Null);
            Assert.That(() => new PhysicalApronPairScratchV2(int.MaxValue), Throws.TypeOf<OverflowException>());
        }
    }
}

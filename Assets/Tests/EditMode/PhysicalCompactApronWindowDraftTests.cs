using System;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class PhysicalCompactApronWindowDraftTests
    {
        private sealed class Fixture
        {
            public PhysicalTrackMap Physical;
            public PhysicalRangeFieldLayout Layout;
            public PhysicalSharedApronDefinition Definition;
            public PhysicalSharedApronMapV2 Map;
            public OccupancyDefinition Body;
        }
        private static Fixture Load()
        {
            var root = PhysicalApronDraftFixtureFiles.Root("candidate-v4", "resources/Biathlon/Venues/woodland_basin_v1/shared_apron.json");
            string Read(string id) => File.ReadAllText(Path.Combine(root.FullName, "candidate-v4/resources", id + ".json"));
            const string v = "Biathlon/Venues/woodland_basin_v1/";
            var track = TrackDefinition.FromJson(Read(v + "track")); var visual = TrackVisualDefinition.FromJson(Read(v + "visual"), track);
            var profile = CompetitionProfile.FromJson(Read(v + "mass_start_profile"));
            var junctions = PhysicalJunctionDefinition.FromJson(Read(v + "junctions"), track);
            var connections = PhysicalConnectionDefinition.FromJson(Read(v + "connections"), junctions);
            var paths = PhysicalRangePathDefinition.FromJson(Read(v + "paths"), track, junctions, connections);
            var route = new RaceRoute(track, RaceRoutePlan.FromJson(Read(v + "mass_start_route")), profile.ToRouteRequirements(), junctions, connections);
            var f = new Fixture { Physical = new PhysicalTrackMap(track, route), Body = OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1")) };
            f.Layout = new PhysicalRangeFieldLayout(f.Physical, track, route, paths);
            f.Definition = PhysicalSharedApronDefinition.FromJson(Read(v + "shared_apron"), track, visual, paths, junctions, connections, f.Body, Read);
            f.Map = new PhysicalSharedApronMapV2(f.Physical, track, route, profile, f.Layout, visual, junctions, connections, f.Body, f.Definition, .05, 2 * SkiState.NanometersPerMeter);
            return f;
        }
        [Test] public void ActualCompactTerminalSeamOutsideOptionalWindowKeepsItsCacheAbsent()
        {
            var f = Load();
            var ordinary = (Array)typeof(PhysicalSharedApronMapV2).GetField("seamProjections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
            var pose = (Array)typeof(PhysicalSharedApronMapV2).GetField("seamPoseProjections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
            for (int path = 0; path < f.Layout.Count; path++)
            {
                var terminal = f.Map.CreateTail(path, 2, 0);
                for (int corridor = 0; corridor < f.Definition.CorridorCount; corridor++)
                {
                    Assert.That(ordinary.GetValue(terminal.ConnectionIndex, corridor), Is.Null);
                    Assert.That(pose.GetValue(terminal.ConnectionIndex, corridor), Is.Null);
                }
            }
        }
        [Test] public void EveryActualPrivateForkAndMergeRetainsOrdinaryAndPoseSeamProof()
        {
            var f = Load();
            var ordinary = (Array)typeof(PhysicalSharedApronMapV2).GetField("seamProjections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
            var pose = (Array)typeof(PhysicalSharedApronMapV2).GetField("seamPoseProjections", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Map);
            for (int path = 0; path < f.Layout.Count; path++) for (int seam = 0; seam < 2; seam++)
            {
                var tail = f.Map.CreateTail(path, seam, 0);
                for (int corridor = 0; corridor < f.Definition.CorridorCount; corridor++)
                {
                    Assert.That(ordinary.GetValue(tail.ConnectionIndex, corridor), Is.Not.Null);
                    Assert.That(pose.GetValue(tail.ConnectionIndex, corridor), Is.Not.Null);
                }
            }
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Maps one immutable assigned lane on bout laps. Mapping does not grant travel,
    /// reserve a junction or remove any body from occupancy.</summary>
    public sealed class PhysicalRangeLaneMap
    {
        private readonly PhysicalTrackMap map;
        private readonly RaceRoute route;
        private readonly PhysicalRangeLaneDefinition definition;
        private readonly string lane;
        private readonly long entry, exit;
        private readonly bool[] usesRange;
        public PhysicalTrackMap PhysicalMap => map;
        public PhysicalRangeLaneMap(PhysicalTrackMap map, RaceRoute route, PhysicalRangeLaneDefinition definition, int laneIndex)
        {
            if (map == null || route == null || definition == null) throw new ArgumentNullException();
            if (map.TrackSnapshotJson != definition.TrackSnapshotJson || route.TrackSnapshotJson != definition.TrackSnapshotJson ||
                map.RoutePlanSnapshotJson != route.PlanSnapshotJson || map.RouteRequirementsSnapshotJson != route.RequirementsSnapshotJson || laneIndex < 0 || laneIndex >= definition.Count)
                throw new ArgumentException("Exact track snapshots and an assigned range lane required.");
            this.map = map; this.route = route; this.definition = definition; lane = definition.GetLane(laneIndex);
            entry = checked(definition.Entry.Millimeters * SkiState.NanometersPerMillimeter);
            exit = checked(definition.Exit.Millimeters * SkiState.NanometersPerMillimeter);
            var requirements = CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            usesRange = new bool[route.SectionCount]; var occurrences = new int[route.LapCount];
            for (int i = 0; i < usesRange.Length; i++)
            {
                var section = route.GetSection(i);
                usesRange[i] = requirements.RequiresRange(section.LapIndex) && section.LoopId == definition.MainLoopId;
                if (usesRange[i]) occurrences[section.LapIndex]++;
            }
            for (int i = 0; i < occurrences.Length; i++)
                if (requirements.RequiresRange(i) && occurrences[i] != 1) throw new ArgumentException("One range occurrence per required lap expected.");
        }
        public PhysicalTrackPoint MapRoute(long distance, PhysicalBoundarySide side = PhysicalBoundarySide.Outgoing)
        {
            var ordinary = map.MapRoute(distance, side); // includes bounds/side validation
            int index = route.Sample(new TrackDistance(distance / SkiState.NanometersPerMillimeter)).SectionIndex;
            var section = route.GetSection(index);
            long local = distance - section.Start.Millimeters * SkiState.NanometersPerMillimeter;
            if (!usesRange[index] || local < entry || local > exit ||
                local == entry && side == PhysicalBoundarySide.Incoming || local == exit && side == PhysicalBoundarySide.Outgoing) return ordinary;
            return map.MapLoop(lane, local - entry);
        }
    }
}

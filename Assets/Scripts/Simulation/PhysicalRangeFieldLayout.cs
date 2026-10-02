using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Cold authored range-path family shared by an independent field's roster.
    /// The immutable maps and complete choice set are built before any race tick.
    /// This binding does not reserve lanes or establish shared-body admission.</summary>
    public sealed class PhysicalRangeFieldLayout
    {
        private readonly TrackDefinition track;
        private readonly RaceRoute route;
        private readonly PhysicalRangePathMap[] paths;
        private readonly PhysicalRangePathDefinition definition;

        public int Count => paths.Length;
        public string ContextJson { get; }
        internal TrackDefinition Track => track;
        internal RaceRoute Route => route;
        internal PhysicalRangePathDefinition Definition => definition;

        public PhysicalRangeFieldLayout(PhysicalTrackMap map, TrackDefinition track,
            RaceRoute route, PhysicalRangePathDefinition definition)
        {
            if (map == null || track == null || route == null || definition == null)
                throw new ArgumentNullException();
            if (!map.IsBoundTo(track, route))
                throw new ArgumentException("The physical map must use the exact authored track and route objects.", nameof(map));
            this.track = track;
            this.route = route;
            this.definition = definition;
            paths = new PhysicalRangePathMap[definition.Count];
            for (int i = 0; i < paths.Length; i++)
            {
                // Every map validates the complete authored snapshots and route occurrences.
                // Reuse the same map, track, route and definition objects for one exact family.
                paths[i] = new PhysicalRangePathMap(map, track, route, definition, i);
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] {
                "physical_range_field_layout_v1", track.ToJson(), route.PlanSnapshotJson,
                route.RequirementsSnapshotJson, definition.ToJson(), definition.JunctionSnapshotJson,
                definition.ConnectionSnapshotJson,
                "authored_ordinal_order;initial_roster_slot_path;preallocated_same_family_choices;no_body_admission"
            });
        }

        public PhysicalRangePathMap GetPath(int index)
        {
            if (index < 0 || index >= paths.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return paths[index];
        }

        internal bool IsBoundTo(TrackDefinition authoredTrack, RaceRoute authoredRoute)
            => ReferenceEquals(track, authoredTrack) && ReferenceEquals(route, authoredRoute);

        internal void ValidatePackTopology(PhysicalJunctionDefinition junctions,
            PhysicalConnectionDefinition connections, long seamClearance)
        {
            if (junctions == null || connections == null ||
                definition.JunctionSnapshotJson != junctions.ToJson() ||
                definition.ConnectionSnapshotJson != connections.ToJson())
                throw new ArgumentException("Pack topology must match the captured range-path topology.");
            // The pack retains one incoming body footprint. Require enough real travel
            // between loop changes to clear it before another seam can be proposed.
            for (int p = 0; p < definition.Count; p++)
            {
                var lane = definition.GetLane(p);
                long previous = 0; // conservatively include a possible rear-grid start seam
                var requirements = CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
                for (int sectionIndex = 0; sectionIndex < route.SectionCount; sectionIndex++)
                {
                    var section = route.GetSection(sectionIndex);
                    long origin = checked(section.Start.Millimeters * SkiState.NanometersPerMillimeter);
                    if (sectionIndex > 0 && route.GetSection(sectionIndex - 1).LoopId != section.LoopId)
                    {
                        RequireSpacing(origin); previous = origin;
                    }
                    if (section.LoopId != definition.MainLoopId || !section.HasRange || !requirements.RequiresRange(section.LapIndex)) continue;
                    long localOrigin=checked(origin-section.LoopStart.Millimeters*SkiState.NanometersPerMillimeter);
                    long entry = checked(localOrigin + definition.Entry.Millimeters * SkiState.NanometersPerMillimeter);
                    for (int s = 1; s < lane.SliceCount; s++)
                        if (lane.GetSlice(s - 1).LoopId != lane.GetSlice(s).LoopId)
                        {
                            long at = checked(entry + lane.GetSlice(s).Offset.Millimeters * SkiState.NanometersPerMillimeter);
                            RequireSpacing(at); previous = at;
                        }
                    long exit = checked(localOrigin + definition.Exit.Millimeters * SkiState.NanometersPerMillimeter);
                    RequireSpacing(exit); previous = exit;
                }
                void RequireSpacing(long at)
                {
                    if (at - previous <= seamClearance)
                        throw new ArgumentException("Consecutive route seams need room for the incoming body and a full maximum-speed tick, including across laps.");
                }
            }
            if (checked(track.GetLoop(track.Penalty.LoopId).Length.Millimeters * SkiState.NanometersPerMillimeter) <= seamClearance)
                throw new ArgumentException("The penalty loop must clear its incoming body before rejoin.");
        }

        /// <summary>Caller-owned array of immutable maps. Athlete constructors clone it again,
        /// preserving their prepared-choice configuration if another caller changes this array.</summary>
        internal PhysicalRangePathMap[] CreateChoices() => (PhysicalRangePathMap[])paths.Clone();
    }
}

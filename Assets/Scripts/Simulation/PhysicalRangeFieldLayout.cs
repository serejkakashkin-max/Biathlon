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

        public int Count => paths.Length;
        public string ContextJson { get; }

        public PhysicalRangeFieldLayout(PhysicalTrackMap map, TrackDefinition track,
            RaceRoute route, PhysicalRangePathDefinition definition)
        {
            if (map == null || track == null || route == null || definition == null)
                throw new ArgumentNullException();
            if (!map.IsBoundTo(track, route))
                throw new ArgumentException("The physical map must use the exact authored track and route objects.", nameof(map));
            this.track = track;
            this.route = route;
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

        /// <summary>Caller-owned array of immutable maps. Athlete constructors clone it again,
        /// preserving their prepared-choice configuration if another caller changes this array.</summary>
        internal PhysicalRangePathMap[] CreateChoices() => (PhysicalRangePathMap[])paths.Clone();
    }
}

using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class SnowConditionSystem
    {
        private readonly Dictionary<string, SnowProfile> surfaces = new Dictionary<string, SnowProfile>(StringComparer.Ordinal);
        public string ConditionsId { get; }
        public string CatalogId { get; }
        public SnowConditionSystem(SnowCatalog catalog, RaceSnowConditions conditions, TrackDefinition track)
        {
            if (catalog == null || conditions == null || track == null) throw new ArgumentNullException(nameof(catalog));
            ConditionsId = conditions.Id; CatalogId = catalog.Id;
            for (int i = 0; i < conditions.SurfaceCount; i++)
            {
                var binding = conditions.GetSurface(i);
                surfaces.Add(binding.SurfaceId, catalog.GetProfile(binding.ProfileId));
            }
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i);
                for (int j = 0; j < loop.SegmentCount; j++)
                    if (!surfaces.ContainsKey(loop.GetSegment(j).SnowProfileId))
                        throw new ArgumentException("Track surface lacks race snow binding.", nameof(conditions));
            }
        }
        public SnowProfile Sample(TrackSample track)
        {
            if (track.SnowProfileId == null || !surfaces.TryGetValue(track.SnowProfileId, out var profile))
                throw new ArgumentException("Unbound track snow surface.", nameof(track));
            return profile;
        }
    }
}

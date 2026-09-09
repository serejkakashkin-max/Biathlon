using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SnowProfile
    {
        private SnowProfile() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "glide_multiplier", IsRequired = true)] public double GlideMultiplier { get; private set; }
        [DataMember(Name = "grip_multiplier", IsRequired = true)] public double GripMultiplier { get; private set; }
        [DataMember(Name = "snow_drag_multiplier", IsRequired = true)] public double SnowDragMultiplier { get; private set; }
        public double ResistanceMultiplier => SnowDragMultiplier / GlideMultiplier;
        internal void Validate()
        {
            TrackValidation.Id(Id);
            TrackValidation.Finite(GlideMultiplier, 0.000000001, 1000000, nameof(GlideMultiplier));
            TrackValidation.Finite(GripMultiplier, 0.000000001, 1000000, nameof(GripMultiplier));
            TrackValidation.Finite(SnowDragMultiplier, 0.000000001, 1000000, nameof(SnowDragMultiplier));
            TrackValidation.Finite(ResistanceMultiplier, 0.000000001, 1000000, nameof(ResistanceMultiplier));
        }
    }
    [DataContract]
    public sealed class SnowCatalog
    {
        private SnowCatalog() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "profiles", IsRequired = true)] private SnowProfile[] Profiles { get; set; }
        private Dictionary<string, SnowProfile> byId;
        public SnowProfile GetProfile(string id) => byId.TryGetValue(id, out var value) ? value : throw new ArgumentException("Unknown snow profile.", nameof(id));
        public static SnowCatalog FromJson(string json)
        {
            var v = RouteJson.Read<SnowCatalog>(json);
            if (v == null || v.SchemaVersion != 1 || v.Profiles == null || v.Profiles.Length == 0)
                throw new SerializationException("Invalid snow catalog.");
            TrackValidation.Id(v.Id); v.byId = new Dictionary<string, SnowProfile>(StringComparer.Ordinal);
            foreach (var profile in v.Profiles)
            {
                if (profile == null) throw new ArgumentException("Null snow profile.");
                profile.Validate();
                if (v.byId.ContainsKey(profile.Id)) throw new ArgumentException("Duplicate snow profile ID.");
                v.byId.Add(profile.Id, profile);
            }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    [DataContract]
    public sealed class SnowSurfaceBinding
    {
        private SnowSurfaceBinding() { }
        [DataMember(Name = "surface_id", IsRequired = true)] public string SurfaceId { get; private set; }
        [DataMember(Name = "profile_id", IsRequired = true)] public string ProfileId { get; private set; }
    }
    [DataContract]
    public sealed class RaceSnowConditions
    {
        private RaceSnowConditions() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "surfaces", IsRequired = true)] private SnowSurfaceBinding[] Surfaces { get; set; }
        public int SurfaceCount => Surfaces.Length;
        public SnowSurfaceBinding GetSurface(int index) => Surfaces[index];
        public static RaceSnowConditions FromJson(string json)
        {
            var v = RouteJson.Read<RaceSnowConditions>(json);
            if (v == null || v.SchemaVersion != 1 || v.Surfaces == null || v.Surfaces.Length == 0)
                throw new SerializationException("Invalid race snow conditions.");
            TrackValidation.Id(v.Id); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var surface in v.Surfaces)
            {
                if (surface == null) throw new ArgumentException("Null surface binding.");
                TrackValidation.Id(surface.SurfaceId); TrackValidation.Id(surface.ProfileId);
                if (!ids.Add(surface.SurfaceId)) throw new ArgumentException("Duplicate snow surface binding.");
            }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public readonly struct SkiTechniqueStats
    {
        public int SkiTechnique { get; }
        public int Climbing { get; }
        public SkiTechniqueStats(int skiTechnique, int climbing)
        {
            if (skiTechnique < 0 || skiTechnique > 100 || climbing < 0 || climbing > 100)
                throw new ArgumentOutOfRangeException(nameof(skiTechnique));
            SkiTechnique = skiTechnique; Climbing = climbing;
        }
    }

    [DataContract]
    public sealed class SkiTechniqueProfile
    {
        private SkiTechniqueProfile() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "flat_affinity", IsRequired = true)] public double FlatAffinity { get; private set; }
        [DataMember(Name = "climb_affinity", IsRequired = true)] public double ClimbAffinity { get; private set; }
        internal void Validate()
        {
            TrackValidation.Id(Id);
            TrackValidation.Finite(FlatAffinity, 0, 1, nameof(FlatAffinity));
            TrackValidation.Finite(ClimbAffinity, 0, 1, nameof(ClimbAffinity));
        }
    }

    [DataContract]
    public sealed class SkiTechniqueDefinition
    {
        private SkiTechniqueDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "full_climb_grade", IsRequired = true)] public double FullClimbGrade { get; private set; }
        [DataMember(Name = "minimum_skill_efficiency", IsRequired = true)] public double MinimumSkillEfficiency { get; private set; }
        [DataMember(Name = "minimum_climbing_efficiency", IsRequired = true)] public double MinimumClimbingEfficiency { get; private set; }
        [DataMember(Name = "exhausted_technique_modifier", IsRequired = true)] public double ExhaustedTechniqueModifier { get; private set; }
        [DataMember(Name = "profiles", IsRequired = true)] private SkiTechniqueProfile[] Profiles { get; set; }
        public int ProfileCount => Profiles.Length;
        public SkiTechniqueProfile GetProfile(int index) => Profiles[index];
        public static SkiTechniqueDefinition FromJson(string json)
        {
            var value = RouteJson.Read<SkiTechniqueDefinition>(json);
            if (value == null || value.SchemaVersion != 1 || value.Profiles == null || value.Profiles.Length == 0)
                throw new SerializationException("Invalid ski technique schema/profiles.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.FullClimbGrade, 0.000000001, 1, nameof(FullClimbGrade));
            TrackValidation.Finite(value.MinimumSkillEfficiency, 0, 1, nameof(MinimumSkillEfficiency));
            TrackValidation.Finite(value.MinimumClimbingEfficiency, 0, 1, nameof(MinimumClimbingEfficiency));
            TrackValidation.Finite(value.ExhaustedTechniqueModifier, 0, 1, nameof(ExhaustedTechniqueModifier));
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in value.Profiles)
            {
                if (profile == null) throw new ArgumentException("Null technique profile.");
                profile.Validate();
                if (!ids.Add(profile.Id)) throw new ArgumentException("Duplicate technique ID.");
            }
            return value;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

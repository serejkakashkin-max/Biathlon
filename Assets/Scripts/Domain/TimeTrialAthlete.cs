using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class TimeTrialAthlete
    {
        private TimeTrialAthlete() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "endurance", IsRequired = true)] public int Endurance { get; private set; }
        [DataMember(Name = "anaerobic_capacity", IsRequired = true)] public int AnaerobicCapacity { get; private set; }
        [DataMember(Name = "recovery", IsRequired = true)] public int Recovery { get; private set; }
        [DataMember(Name = "strength", IsRequired = true)] public int Strength { get; private set; }
        [DataMember(Name = "ski_technique", IsRequired = true)] public int SkiTechnique { get; private set; }
        [DataMember(Name = "climbing", IsRequired = true)] public int Climbing { get; private set; }
        [DataMember(Name = "downhill", IsRequired = true)] public int Downhill { get; private set; }
        [DataMember(Name = "cornering", IsRequired = true)] public int Cornering { get; private set; }
        public AthletePhysiologyStats PhysiologyStats => new AthletePhysiologyStats(Endurance, AnaerobicCapacity, Recovery, Strength);
        public SkiTechniqueStats TechniqueStats => new SkiTechniqueStats(SkiTechnique, Climbing);
        public static TimeTrialAthlete FromJson(string json)
        {
            var v = RouteJson.Read<TimeTrialAthlete>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid time trial athlete schema.");
            TrackValidation.Id(v.Id);
            var physical = v.PhysiologyStats; var technique = v.TechniqueStats;
            if (v.Downhill < 0 || v.Downhill > 100 || v.Cornering < 0 || v.Cornering > 100) throw new ArgumentOutOfRangeException(nameof(json));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct TimeTrialInput
    {
        public SimulationInput Effort { get; }
        public double Form { get; }
        public double LineQuality { get; }
        public double HeadwindMetersPerSecond { get; }
        public bool TuckRequested { get; }
        public bool SprintRequested { get; }
        public string TechniqueId { get; }
        public double MovementEfficiency { get; }
        public TimeTrialInput(SimulationInput effort, double form, double lineQuality, double headwindMetersPerSecond,
            bool tuckRequested = false, bool sprintRequested = false, string techniqueId = null, double movementEfficiency = 1)
        {
            if (!effort.AthleteId.IsValid) throw new ArgumentException("Valid time-trial input required.");
            Effort = effort;
            Form = PhysiologyNumbers.RequireRange(form, 0, 1.10, nameof(form));
            LineQuality = PhysiologyNumbers.RequireRange(lineQuality, 0, 1, nameof(lineQuality));
            HeadwindMetersPerSecond = PhysiologyNumbers.RequireRange(headwindMetersPerSecond, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(headwindMetersPerSecond));
            TuckRequested = tuckRequested; SprintRequested = sprintRequested; TechniqueId = techniqueId;
            MovementEfficiency = PhysiologyNumbers.RequireRange(movementEfficiency, 0, 1, nameof(movementEfficiency));
        }
    }
}

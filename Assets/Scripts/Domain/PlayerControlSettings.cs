using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class PlayerControlSettings
    {
        private PlayerControlSettings() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "assisted_effort", IsRequired = true)] public bool AssistedEffort { get; private set; }
        [DataMember(Name = "assisted_center", IsRequired = true)] public bool AssistedCenter { get; private set; }
        [DataMember(Name = "manual_technique", IsRequired = true)] public bool ManualTechnique { get; private set; }
        [DataMember(Name = "form", IsRequired = true)] public double Form { get; private set; }
        [DataMember(Name = "headwind_mps", IsRequired = true)] public double Headwind { get; private set; }
        public static PlayerControlSettings FromJson(string json)
        {
            var v = RouteJson.Read<PlayerControlSettings>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid player control settings schema.");
            TrackValidation.Id(v.Id);
            PhysiologyNumbers.RequireRange(v.Form, 0, 1.10, nameof(Form));
            PhysiologyNumbers.RequireRange(v.Headwind, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(Headwind));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

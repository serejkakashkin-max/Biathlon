using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingHandlingDefinition
    {
        private ShootingHandlingDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "novice_time_multiplier", IsRequired = true)] public double NoviceTimeMultiplier { get; private set; }
        [DataMember(Name = "expert_time_multiplier", IsRequired = true)] public double ExpertTimeMultiplier { get; private set; }
        [DataMember(Name = "novice_settle_tau_s", IsRequired = true)] public double NoviceSettleTau { get; private set; }
        [DataMember(Name = "expert_settle_tau_s", IsRequired = true)] public double ExpertSettleTau { get; private set; }
        [DataMember(Name = "recoil_x_rad", IsRequired = true)] public double RecoilX { get; private set; }
        [DataMember(Name = "recoil_y_rad", IsRequired = true)] public double RecoilY { get; private set; }
        [DataMember(Name = "maximum_recoil_rad", IsRequired = true)] public double MaximumRecoil { get; private set; }
        [DataMember(Name = "equipment_envelope", IsRequired = true)] public double EquipmentEnvelope { get; private set; }
        public static ShootingHandlingDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingHandlingDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported handling schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.ExpertTimeMultiplier, 0.5, 1, nameof(ExpertTimeMultiplier));
            TrackValidation.Finite(v.NoviceTimeMultiplier, 1, 2, nameof(NoviceTimeMultiplier));
            TrackValidation.Finite(v.ExpertSettleTau, 0.01, 5, nameof(ExpertSettleTau));
            TrackValidation.Finite(v.NoviceSettleTau, v.ExpertSettleTau, 5, nameof(NoviceSettleTau));
            TrackValidation.Finite(v.MaximumRecoil, 0.00001, 0.1, nameof(MaximumRecoil));
            TrackValidation.Finite(v.RecoilX, 0, v.MaximumRecoil, nameof(RecoilX));
            TrackValidation.Finite(v.RecoilY, 0.000001, v.MaximumRecoil, nameof(RecoilY));
            TrackValidation.Finite(v.EquipmentEnvelope, 0, 0.1, nameof(EquipmentEnvelope));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    [DataContract]
    public sealed class ShootingEquipmentDefinition
    {
        private ShootingEquipmentDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "handling_multiplier", IsRequired = true)] public double HandlingMultiplier { get; private set; }
        [DataMember(Name = "recoil_multiplier", IsRequired = true)] public double RecoilMultiplier { get; private set; }
        [DataMember(Name="ergonomics_multiplier",EmitDefaultValue=false)] private double ergonomics {get;set;}
        public double ErgonomicsMultiplier=>ergonomics==0?1:ergonomics;
        public static ShootingEquipmentDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingEquipmentDefinition>(json);
            if (v == null || v.SchemaVersion != 1 && v.SchemaVersion != 2) throw new SerializationException("Unsupported rifle schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.HandlingMultiplier, 0.5, 1.5, nameof(HandlingMultiplier));
            TrackValidation.Finite(v.RecoilMultiplier, 0.5, 1.5, nameof(RecoilMultiplier));
            if(v.SchemaVersion==1&&v.ergonomics!=0||v.SchemaVersion==2&&v.ergonomics==0)throw new SerializationException("Rifle ergonomics requires its explicit version2 definition.");
            TrackValidation.Finite(v.ErgonomicsMultiplier,.5,1.5,nameof(ErgonomicsMultiplier));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct ShootingHandlingProfile
    {
        public double TimeMultiplier { get; } public double SettleTauSeconds { get; }
        public double RecoilX { get; } public double RecoilY { get; }
        public double SetupTimeMultiplier {get;} public double ExitTimeMultiplier {get;}
        public bool IsValid => TimeMultiplier > 0 && SettleTauSeconds > 0;
        public ShootingHandlingProfile(ShootingHandlingDefinition definition, ShootingEquipmentDefinition equipment, int shootingSpeed)
        {
            if (definition == null || equipment == null) throw new System.ArgumentNullException(nameof(definition));
            if (shootingSpeed < 0 || shootingSpeed > 100) throw new System.ArgumentOutOfRangeException(nameof(shootingSpeed));
            PhysiologyNumbers.RequireRange(equipment.HandlingMultiplier, 1 - definition.EquipmentEnvelope, 1 + definition.EquipmentEnvelope, nameof(equipment.HandlingMultiplier));
            PhysiologyNumbers.RequireRange(equipment.RecoilMultiplier, 1 - definition.EquipmentEnvelope, 1 + definition.EquipmentEnvelope, nameof(equipment.RecoilMultiplier));
            PhysiologyNumbers.RequireRange(equipment.ErgonomicsMultiplier,1-definition.EquipmentEnvelope,1+definition.EquipmentEnvelope,nameof(equipment.ErgonomicsMultiplier));
            PhysiologyNumbers.RequireRange(equipment.HandlingMultiplier*equipment.ErgonomicsMultiplier,1-definition.EquipmentEnvelope,1+definition.EquipmentEnvelope,"combined equipment setup");
            double skill = shootingSpeed / 100.0;
            TimeMultiplier = (definition.NoviceTimeMultiplier + (definition.ExpertTimeMultiplier - definition.NoviceTimeMultiplier) * skill) * equipment.HandlingMultiplier;
            SetupTimeMultiplier=TimeMultiplier*equipment.ErgonomicsMultiplier;ExitTimeMultiplier=equipment.ErgonomicsMultiplier;
            SettleTauSeconds = (definition.NoviceSettleTau + (definition.ExpertSettleTau - definition.NoviceSettleTau) * skill) * equipment.HandlingMultiplier;
            RecoilX = definition.RecoilX * equipment.RecoilMultiplier; RecoilY = definition.RecoilY * equipment.RecoilMultiplier;
        }
    }
}

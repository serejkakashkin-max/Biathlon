using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class AiPersonalityDefinition
    {
        private AiPersonalityDefinition() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="aggression", IsRequired=true)] public double Aggression { get; private set; }
        [DataMember(Name="patience", IsRequired=true)] public double Patience { get; private set; }
        [DataMember(Name="draft_preference", IsRequired=true)] public double DraftPreference { get; private set; }
        [DataMember(Name="shooting_risk", IsRequired=true)] public double ShootingRisk { get; private set; }
        [DataMember(Name="shooting_tempo", IsRequired=true)] public double ShootingTempo { get; private set; }
        [DataMember(Name="final_lap_aggression", IsRequired=true)] public double FinalLapAggression { get; private set; }
        [DataMember(Name="pressure_resistance", IsRequired=true)] public double PressureResistance { get; private set; }
        [DataMember(Name="overtake_confidence", IsRequired=true)] public double OvertakeConfidence { get; private set; }
        public static AiPersonalityDefinition FromJson(string json)
        {
            var v=RouteJson.Read<AiPersonalityDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported AI personality schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.Aggression,0,1,nameof(Aggression)); TrackValidation.Finite(v.Patience,0,1,nameof(Patience));
            TrackValidation.Finite(v.DraftPreference,0,1,nameof(DraftPreference)); TrackValidation.Finite(v.ShootingRisk,0,1,nameof(ShootingRisk));
            TrackValidation.Finite(v.ShootingTempo,0,1,nameof(ShootingTempo)); TrackValidation.Finite(v.FinalLapAggression,0,1,nameof(FinalLapAggression));
            TrackValidation.Finite(v.PressureResistance,0,1,nameof(PressureResistance)); TrackValidation.Finite(v.OvertakeConfidence,0,1,nameof(OvertakeConfidence));
            return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

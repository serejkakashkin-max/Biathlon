using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class DraftSelectionDefinition
    {
        private DraftSelectionDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="switch_strength_margin",IsRequired=true)] public double SwitchStrengthMargin { get; private set; }
        [DataMember(Name="minimum_hold_seconds",IsRequired=true)] public double MinimumHoldSeconds { get; private set; }
        public static DraftSelectionDefinition FromJson(string json)
        {
            var v=RouteJson.Read<DraftSelectionDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported draft selection schema.");
            TrackValidation.Id(v.Id); TrackValidation.Finite(v.SwitchStrengthMargin,0,1,nameof(SwitchStrengthMargin));
            TrackValidation.Finite(v.MinimumHoldSeconds,0,120,nameof(MinimumHoldSeconds)); return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

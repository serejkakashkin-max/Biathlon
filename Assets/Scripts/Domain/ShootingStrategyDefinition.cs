using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class ShootingStrategyDefinition
    {
        private ShootingStrategyDefinition() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="cautious_tolerance_rad", IsRequired=true)] public double CautiousTolerance { get; private set; }
        [DataMember(Name="fast_tolerance_rad", IsRequired=true)] public double FastTolerance { get; private set; }
        [DataMember(Name="cautious_stable_seconds", IsRequired=true)] public double CautiousStable { get; private set; }
        [DataMember(Name="fast_stable_seconds", IsRequired=true)] public double FastStable { get; private set; }
        [DataMember(Name="cautious_maximum_aim_seconds", IsRequired=true)] public double CautiousMaximumAim { get; private set; }
        [DataMember(Name="fast_maximum_aim_seconds", IsRequired=true)] public double FastMaximumAim { get; private set; }
        [DataMember(Name="stability_need_weight", IsRequired=true)] public double StabilityWeight { get; private set; }
        public static ShootingStrategyDefinition FromJson(string json)
        {
            var v=RouteJson.Read<ShootingStrategyDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported shooting strategy schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.CautiousTolerance,0.000001,0.005,nameof(CautiousTolerance));
            TrackValidation.Finite(v.FastTolerance,v.CautiousTolerance,0.005,nameof(FastTolerance));
            TrackValidation.Finite(v.FastStable,0.01,2,nameof(FastStable));
            TrackValidation.Finite(v.CautiousStable,v.FastStable,2,nameof(CautiousStable));
            TrackValidation.Finite(v.FastMaximumAim,v.FastStable,30,nameof(FastMaximumAim));
            TrackValidation.Finite(v.CautiousMaximumAim,System.Math.Max(v.FastMaximumAim,v.CautiousStable),30,nameof(CautiousMaximumAim));
            TrackValidation.Finite(v.StabilityWeight,0,1,nameof(StabilityWeight)); return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

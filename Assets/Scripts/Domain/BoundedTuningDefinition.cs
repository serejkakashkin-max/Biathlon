using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class TuningParameter
    {
        private TuningParameter() { }
        [DataMember(Name="name",IsRequired=true)] public string Name { get; private set; }
        [DataMember(Name="minimum",IsRequired=true)] public double Minimum { get; private set; }
        [DataMember(Name="maximum",IsRequired=true)] public double Maximum { get; private set; }
        [DataMember(Name="initial",IsRequired=true)] public double Initial { get; private set; }
        [DataMember(Name="values",IsRequired=true)] private double[] values { get; set; }
        public int Count=>values.Length; public double Get(int i)=>values[i];
        internal void Validate()
        {
            // This release deliberately exposes only AI control coefficients, never outcome probabilities.
            if(Name!="reaction_seconds"&&Name!="tracking_error_rad")throw new SerializationException("Coefficient is not in the M8 tuner allowlist.");
            double lower=Name=="reaction_seconds"?.01:0,upper=Name=="reaction_seconds"?2:.005;
            TrackValidation.Finite(Minimum,lower,upper,Name);TrackValidation.Finite(Maximum,Minimum,upper,Name);
            TrackValidation.Finite(Initial,Minimum,Maximum,Name);
            if(values==null||values.Length==0||values.Length>100)throw new SerializationException("Finite nonempty candidate list required.");
            for(int i=0;i<values.Length;i++){TrackValidation.Finite(values[i],Minimum,Maximum,Name);for(int j=0;j<i;j++)if(values[i]==values[j])throw new SerializationException("Duplicate candidate.");}
        }
    }
    [DataContract] public sealed class TuningTarget
    {
        private TuningTarget() { }
        [DataMember(Name="metric",IsRequired=true)] public string Metric { get; private set; }
        [DataMember(Name="minimum",IsRequired=true)] public double Minimum { get; private set; }
        [DataMember(Name="maximum",IsRequired=true)] public double Maximum { get; private set; }
        internal void Validate(){TrackValidation.Id(Metric);TrackValidation.Finite(Minimum,-1e9,1e9,Metric);TrackValidation.Finite(Maximum,Minimum,1e9,Metric);if(Maximum==Minimum)throw new SerializationException("Target window needs positive width.");}
    }
    [DataContract] public sealed class BoundedTuningDefinition
    {
        private BoundedTuningDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="maximum_evaluations",IsRequired=true)] public int MaximumEvaluations { get; private set; }
        [DataMember(Name="parameters",IsRequired=true)] private TuningParameter[] parameters { get; set; }
        [DataMember(Name="targets",IsRequired=true)] private TuningTarget[] targets { get; set; }
        public int ParameterCount=>parameters.Length;public int TargetCount=>targets.Length;
        public TuningParameter Parameter(int i)=>parameters[i];public TuningTarget Target(int i)=>targets[i];
        public int CandidateCount { get {int n=1;foreach(var p in parameters)n*=p.Count;return n;} }
        public string ToJson()=>RouteJson.Write(this);
        public static BoundedTuningDefinition FromJson(string json)
        {
            var d=RouteJson.Read<BoundedTuningDefinition>(json);
            if(d==null||d.SchemaVersion!=1||d.parameters==null||d.parameters.Length<1||d.parameters.Length>2||d.targets==null||d.targets.Length<1||d.targets.Length>32||d.MaximumEvaluations<1||d.MaximumEvaluations>10000)throw new SerializationException("Invalid bounded search schema/budget.");
            TrackValidation.Id(d.Id);
            for(int i=0;i<d.ParameterCount;i++){if(d.parameters[i]==null)throw new SerializationException("Null parameter.");d.parameters[i].Validate();for(int j=0;j<i;j++)if(d.parameters[i].Name==d.parameters[j].Name)throw new SerializationException("Duplicate parameter.");}
            for(int i=0;i<d.TargetCount;i++){if(d.targets[i]==null)throw new SerializationException("Null target.");d.targets[i].Validate();for(int j=0;j<i;j++)if(d.targets[i].Metric==d.targets[j].Metric)throw new SerializationException("Duplicate target.");}
            if(d.CandidateCount>d.MaximumEvaluations)throw new SerializationException("Search exceeds declared evaluation budget.");return d;
        }
    }
}

using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class ManualCourseContactDefinition
    {
        private ManualCourseContactDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="normal_probe_m",IsRequired=true)] public double NormalProbe {get;private set;}
        [DataMember(Name="normal_sample_columns",EmitDefaultValue=false)] public int NormalSampleColumns {get;private set;}
        [DataMember(Name="normal_sample_rows",EmitDefaultValue=false)] public int NormalSampleRows {get;private set;}
        [DataMember(Name="angular_probe_rad",IsRequired=true)] public double AngularProbe {get;private set;}
        [DataMember(Name="maximum_responses_per_tick",IsRequired=true)] public int MaximumResponses {get;private set;}
        [DataMember(Name="arrest_contact_spin",EmitDefaultValue=false)] public bool ArrestContactSpin {get;private set;}
        [DataMember(Name="separation_bias_speed_mps",EmitDefaultValue=false)] public double SeparationBiasSpeed {get;private set;}
        public static ManualCourseContactDefinition FromJson(string json)
        {
            var d=RouteJson.Read<ManualCourseContactDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid course contact schema.");
            TrackValidation.Id(d.Id);TrackValidation.Finite(d.NormalProbe,.00001,.01,nameof(NormalProbe));
            if(d.NormalSampleColumns<0||d.NormalSampleColumns>32||d.NormalSampleRows<0||d.NormalSampleRows>32||
                (d.NormalSampleColumns==0)!=(d.NormalSampleRows==0)||d.NormalSampleColumns==1||d.NormalSampleRows==1)
                throw new SerializationException("Normal support sampling must be disabled or use bounded rows and columns.");
            TrackValidation.Finite(d.AngularProbe,.00001,.01,nameof(AngularProbe));
            TrackValidation.Finite(d.SeparationBiasSpeed,0,.1,nameof(SeparationBiasSpeed));
            if(d.MaximumResponses<1||d.MaximumResponses>64)throw new SerializationException("Invalid course response bound.");return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

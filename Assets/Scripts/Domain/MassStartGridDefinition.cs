using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class MassStartGridDefinition
    {
        private MassStartGridDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="columns",IsRequired=true)] public int Columns { get; private set; }
        [DataMember(Name="row_spacing_m",IsRequired=true)] public double RowSpacingMeters { get; private set; }
        [DataMember(Name="column_spacing_m",IsRequired=true)] public double ColumnSpacingMeters { get; private set; }
        public TrackDistance RowSpacing { get; private set; }
        public TrackDistance ColumnSpacing { get; private set; }
        public static MassStartGridDefinition FromJson(string json)
        {
            var d=RouteJson.Read<MassStartGridDefinition>(json);
            if(d==null || d.SchemaVersion!=1 || d.Columns<1 || d.Columns>30)
                throw new SerializationException("Supported grid schema and bounded columns required.");
            TrackValidation.Id(d.Id);
            d.RowSpacing=TrackDistance.FromMeters(d.RowSpacingMeters);
            d.ColumnSpacing=TrackDistance.FromMeters(d.ColumnSpacingMeters);
            if(d.RowSpacing.Millimeters==0 || d.ColumnSpacing.Millimeters==0)
                throw new SerializationException("Positive grid spacing required.");
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

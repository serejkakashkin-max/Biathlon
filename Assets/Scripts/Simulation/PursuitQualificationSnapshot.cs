using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    [DataContract]
    public sealed class PursuitQualifier
    {
        private PursuitQualifier() { }
        [DataMember(Name="athlete_id",IsRequired=true)] public string AthleteId { get; private set; }
        [DataMember(Name="start_ordinal",IsRequired=true)] public int StartOrdinal { get; private set; }
        [DataMember(Name="source_rank",IsRequired=true)] public int SourceRank { get; private set; }
        [DataMember(Name="start_gap_units",IsRequired=true)] public long StartGapUnits { get; private set; }
        internal PursuitQualifier(SprintResultRow row,int ordinal)
        { AthleteId=row.AthleteId;StartOrdinal=ordinal;SourceRank=row.Rank;StartGapUnits=row.GapUnits.Value; }
    }

    /// <summary>Cold-path Pursuit dependency. Gaps use the source's registered integer units.
    /// Expected context must come from the calling competition, not from this payload.</summary>
    [DataContract]
    public sealed class PursuitQualificationSnapshot
    {
        private PursuitQualificationSnapshot() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="source_result_id",IsRequired=true)] public string SourceResultId { get; private set; }
        [DataMember(Name="source_sprint_json",IsRequired=true)] public string SourceSprintJson { get; private set; }
        [DataMember(Name="pursuit_profile_json",IsRequired=true)] public string PursuitProfileJson { get; private set; }
        [DataMember(Name="gap_units_per_second",IsRequired=true)] public int GapUnitsPerSecond { get; private set; }
        [DataMember(Name="qualifiers",IsRequired=true)] private PursuitQualifier[] rows;
        public int Count => rows.Length;
        public PursuitQualifier Get(int index) => rows[index];

        public static PursuitQualificationSnapshot Create(string id,SprintResultSnapshot source,CompetitionProfile pursuit)
        {
            if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Stable qualification ID required.",nameof(id));
            if(source==null || pursuit==null) throw new ArgumentNullException();
            // Revalidate the complete source before consuming any derived rank/gap field.
            source=SprintResultSnapshot.FromJson(source.ToJson());
            var sprint=CompetitionProfile.FromJson(source.CompetitionProfileJson);
            if(pursuit.Format!=CompetitionFormat.Pursuit || sprint.Category!=pursuit.Category || sprint.RulesRevision!=pursuit.RulesRevision)
                throw new ArgumentException("Compatible Sprint/Pursuit category and rule revision required.");
            var qualifiers=new List<PursuitQualifier>();
            for(int i=0;i<source.Count;i++)
            {
                var row=source.Get(i);if(row.Status!=RaceResultStatus.Finished)continue;
                qualifiers.Add(new PursuitQualifier(row,qualifiers.Count+1));
            }
            if(qualifiers.Count==0 || qualifiers.Count>pursuit.FieldCapacity)
                throw new ArgumentException("A nonempty qualifying field within target capacity is required; no implicit truncation.");
            return new PursuitQualificationSnapshot {SchemaVersion=1,Id=id,SourceResultId=source.Id,SourceSprintJson=source.ToJson(),
                PursuitProfileJson=pursuit.ToJson(),GapUnitsPerSecond=ResultTimingDefinition.FromJson(source.TimingDefinitionJson).UnitsPerSecond,rows=qualifiers.ToArray()};
        }
        public string ToJson()
        {
            using(var stream=new MemoryStream())
            {new DataContractJsonSerializer(typeof(PursuitQualificationSnapshot)).WriteObject(stream,this);return Encoding.UTF8.GetString(stream.ToArray());}
        }
        public static PursuitQualificationSnapshot FromJson(string json,string expectedSprintResultId,CompetitionProfile expectedPursuitProfile)
        {
            if(string.IsNullOrWhiteSpace(expectedSprintResultId) || expectedPursuitProfile==null) throw new ArgumentException("Caller-owned expected dependency context required.");
            PursuitQualificationSnapshot value;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json)))))
                value=(PursuitQualificationSnapshot)new DataContractJsonSerializer(typeof(PursuitQualificationSnapshot)).ReadObject(stream);
            if(value==null || value.SchemaVersion!=1 || value.rows==null) throw new SerializationException("Invalid Pursuit qualification schema.");
            var source=SprintResultSnapshot.FromJson(value.SourceSprintJson);
            var target=CompetitionProfile.FromJson(value.PursuitProfileJson);
            if(source.Id!=expectedSprintResultId || value.SourceResultId!=source.Id || target.ToJson()!=expectedPursuitProfile.ToJson())
                throw new SerializationException("Wrong source result or target competition profile.");
            var canonical=Create(value.Id,source,target);
            if(value.GapUnitsPerSecond!=canonical.GapUnitsPerSecond || value.Count!=canonical.Count)
                throw new SerializationException("Altered qualification size or timing units.");
            for(int i=0;i<value.Count;i++)
            {
                var a=value.rows[i];var b=canonical.rows[i];
                if(a==null || a.AthleteId!=b.AthleteId || a.StartOrdinal!=b.StartOrdinal || a.SourceRank!=b.SourceRank || a.StartGapUnits!=b.StartGapUnits)
                    throw new SerializationException("Altered qualifier identity, order, rank or gap.");
            }
            return canonical;
        }
    }
}

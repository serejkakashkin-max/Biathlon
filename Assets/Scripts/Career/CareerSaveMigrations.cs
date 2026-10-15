using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] internal sealed class CareerSaveVersionHeader
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="save_version",IsRequired=true)] public int SaveVersion {get;private set;}
    }

    /// <summary>Forward-incompatible data is preserved, never treated as a damaged old save.</summary>
    public sealed class UnsupportedCareerSaveVersionException : SerializationException
    {
        public int SchemaVersion {get;}
        public int SaveVersion {get;}
        public UnsupportedCareerSaveVersionException(int schemaVersion,int saveVersion)
            :base("Unsupported career save version.") {SchemaVersion=schemaVersion;SaveVersion=saveVersion;}
    }

    /// <summary>IO-free, deterministic migration receipt. Reading never rewrites the source file.</summary>
    public sealed class CareerSaveMigrationResult
    {
        public CareerSave Save {get;}
        public int SourceVersion {get;}
        public int TargetVersion=>CareerSave.CurrentVersion;
        public int StepCount=>TargetVersion-SourceVersion;
        public bool WasMigrated=>StepCount!=0;
        internal CareerSaveMigrationResult(CareerSave save,int sourceVersion) {Save=save;SourceVersion=sourceVersion;}
        public string StepId(int index)
        {
            if(index<0||index>=StepCount)throw new ArgumentOutOfRangeException(nameof(index));
            return SourceVersion+index==1?"career_v1_to_v2_confidence":"career_v2_to_v3_appearance";
        }
    }

    /// <summary>Ordered v1→v2→v3 migrations retain original abilities, RNG and recorded race payloads.</summary>
    public static class CareerSaveMigrations
    {
        public static CareerSaveMigrationResult Load(string json)=>CareerSave.ReadMigrated(json);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Presentation-only entry list. Scene bootstraps retain all race authority.</summary>
    [Serializable] public sealed class ProductionReviewDefinition
    {
        public int schema_version;
        public string id, venue_id, initial_entry_id, initial_quality_tier;
        public ProductionReviewEntry[] entries;
        public static ProductionReviewDefinition FromJson(string json)
        {
            if(string.IsNullOrWhiteSpace(json))throw new ArgumentException("A review definition is required.");
            var value=JsonUtility.FromJson<ProductionReviewDefinition>(json);
            if(value==null)throw new ArgumentException("A review definition is required.");
            value.Validate();return value;
        }
        public string ToJson(){Validate();return JsonUtility.ToJson(this);}
        public ProductionReviewEntry Get(string entryId)
        {
            if(entries!=null)foreach(var entry in entries)if(entry!=null&&entry.id==entryId)return entry;
            throw new ArgumentException("Unknown production review entry: "+entryId);
        }
        public ProductionReviewEntry ForScene(string path)
        {
            foreach(var entry in entries)if(entry.scene_path==path)return entry;
            throw new ArgumentException("The active scene is not in this production review.");
        }
        public void Validate()
        {
            if(schema_version!=1||!Stable(id)||!Stable(venue_id)||!Stable(initial_entry_id)||
                (initial_quality_tier!="high"&&initial_quality_tier!="balanced"&&initial_quality_tier!="low")||entries==null||entries.Length!=2)
                throw new ArgumentException("A versioned two-entry production review is required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);var scenes=new HashSet<string>(StringComparer.Ordinal);var formats=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in entries)
            {
                if(entry==null||!Stable(entry.id)||entry.field_count<1||entry.field_count>30||(entry.format_id!="sprint"&&entry.format_id!="mass_start")||string.IsNullOrWhiteSpace(entry.label)||entry.label.Length>80||entry.label.IndexOfAny(new[]{'\r','\n','\t'})>=0||
                    !ScenePath(entry.scene_path)||!ids.Add(entry.id)||!scenes.Add(entry.scene_path)||!formats.Add(entry.format_id))throw new ArgumentException("Invalid or duplicated review entry.");
            }
            Get(initial_entry_id);
        }
        private static bool Stable(string value)
        {
            if(string.IsNullOrEmpty(value)||value.Length>256)return false;
            foreach(char c in value)if(!(c>='a'&&c<='z')&&!(c>='A'&&c<='Z')&&!(c>='0'&&c<='9')&&c!='_'&&c!='-')return false;
            return true;
        }
        private static bool ScenePath(string value)
        {
            if(string.IsNullOrEmpty(value)||!value.StartsWith("Assets/Scenes/Production/",StringComparison.Ordinal)||!value.EndsWith(".unity",StringComparison.Ordinal))return false;
            string name=value.Substring("Assets/Scenes/Production/".Length);
            return name.Length>6&&Stable(name.Substring(0,name.Length-6));
        }
    }
    [Serializable] public sealed class ProductionReviewEntry { public string id,label,scene_path,format_id; public int field_count; }
}

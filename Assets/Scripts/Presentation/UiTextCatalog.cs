using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Presentation
{
    [DataContract] public sealed class UiTextCatalog
    {
        [DataContract] public sealed class Entry
        {
            [DataMember(Name="key",IsRequired=true)] public string Key { get; private set; }
            [DataMember(Name="ru",IsRequired=true)] public string Russian { get; private set; }
            [DataMember(Name="en",IsRequired=true)] public string English { get; private set; }
        }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="entries",IsRequired=true)] private Entry[] entries { get; set; }
        private Dictionary<string,Entry> byKey;
        public static UiTextCatalog FromJson(string json)
        {
            UiTextCatalog catalog;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))catalog=(UiTextCatalog)new DataContractJsonSerializer(typeof(UiTextCatalog)).ReadObject(stream);
            if(catalog==null||catalog.SchemaVersion!=1||string.IsNullOrWhiteSpace(catalog.Id)||catalog.entries==null||catalog.entries.Length==0)throw new SerializationException("Invalid UI text catalog.");
            catalog.byKey=new Dictionary<string,Entry>(StringComparer.Ordinal);
            foreach(var row in catalog.entries)
            {
                if(row==null||string.IsNullOrWhiteSpace(row.Key)||string.IsNullOrWhiteSpace(row.Russian)||string.IsNullOrWhiteSpace(row.English)||catalog.byKey.ContainsKey(row.Key))throw new SerializationException("Invalid/duplicate localized text.");
                catalog.byKey.Add(row.Key,row);
            }
            return catalog;
        }
        public string Text(string key,string locale)
        {
            if(locale!="ru"&&locale!="en")throw new ArgumentException("Unsupported UI locale.");
            if(!byKey.TryGetValue(key,out var row))throw new ArgumentException("Unknown UI text key: "+key);
            return locale=="ru"?row.Russian:row.English;
        }
    }
}

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
            [DataMember(Name="ru",EmitDefaultValue=false)] public string Russian { get; private set; }
            [DataMember(Name="en",EmitDefaultValue=false)] public string English { get; private set; }
            [DataMember(Name="values",EmitDefaultValue=false)] public string[] Values { get; private set; }
            [DataMember(Name="source_ru",EmitDefaultValue=false)] public string SourceRussian { get; private set; }
            [DataMember(Name="source_en",EmitDefaultValue=false)] public string SourceEnglish { get; private set; }
        }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="entries",IsRequired=true)] private Entry[] entries { get; set; }
        [DataMember(Name="locales",EmitDefaultValue=false)] private string[] locales {get;set;}
        private Dictionary<string,Entry> byKey;
        private Dictionary<string,Dictionary<string,Entry>> byPair;
        public int Count=>entries.Length;
        public int LocaleCount=>locales.Length;
        public string LocaleAt(int index)=>locales[index];
        public string KeyAt(int index)=>entries[index].Key;
        public static UiTextCatalog FromJson(string json)
        {
            UiTextCatalog catalog;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))catalog=(UiTextCatalog)new DataContractJsonSerializer(typeof(UiTextCatalog)).ReadObject(stream);
            if(catalog==null||(catalog.SchemaVersion!=1&&catalog.SchemaVersion!=2)||string.IsNullOrWhiteSpace(catalog.Id)||catalog.entries==null||catalog.entries.Length==0)throw new SerializationException("Invalid UI text catalog.");
            if(catalog.SchemaVersion==1)catalog.locales=new[]{"ru","en"};
            if(catalog.locales==null||catalog.locales.Length<1||catalog.locales.Length>32)throw new SerializationException("Invalid locale inventory.");
            var known=new HashSet<string>(StringComparer.Ordinal);
            foreach(string locale in catalog.locales)
            {if(string.IsNullOrWhiteSpace(locale)||locale.Length>12||!known.Add(locale))throw new SerializationException("Invalid/duplicate locale.");foreach(char c in locale)if(!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c=='-'))throw new SerializationException("Invalid locale identifier.");}
            catalog.byKey=new Dictionary<string,Entry>(StringComparer.Ordinal);
            catalog.byPair=new Dictionary<string,Dictionary<string,Entry>>(StringComparer.Ordinal);
            foreach(var row in catalog.entries)
            {
                if(row==null||string.IsNullOrWhiteSpace(row.Key)||catalog.byKey.ContainsKey(row.Key))throw new SerializationException("Invalid/duplicate localized text.");
                if(catalog.SchemaVersion==1)
                {if(string.IsNullOrWhiteSpace(row.Russian)||string.IsNullOrWhiteSpace(row.English))throw new SerializationException("Missing legacy translation.");}
                else
                {
                    if(row.Values==null||row.Values.Length!=catalog.locales.Length)throw new SerializationException("Missing localized values.");
                    string signature=null;
                    foreach(string value in row.Values)
                    {if(string.IsNullOrWhiteSpace(value))throw new SerializationException("Empty translation.");string next=PlaceholderSignature(value);if(signature!=null&&next!=signature)throw new SerializationException("Translation placeholders differ.");signature=next;}
                }
                catalog.byKey.Add(row.Key,row);
                if(row.SourceRussian!=null||row.SourceEnglish!=null)
                {
                    if(string.IsNullOrEmpty(row.SourceRussian)||string.IsNullOrEmpty(row.SourceEnglish))throw new SerializationException("Incomplete legacy label bridge.");
                    if(!catalog.byPair.TryGetValue(row.SourceRussian,out var pairs))catalog.byPair.Add(row.SourceRussian,pairs=new Dictionary<string,Entry>(StringComparer.Ordinal));
                    if(pairs.ContainsKey(row.SourceEnglish))throw new SerializationException("Duplicate legacy label bridge.");pairs.Add(row.SourceEnglish,row);
                }
            }
            return catalog;
        }
        public string Text(string key,string locale)
        {
            int index=Array.IndexOf(locales,locale);if(index<0)throw new ArgumentException("Unsupported UI locale.");
            if(!byKey.TryGetValue(key,out var row))throw new ArgumentException("Unknown UI text key: "+key);
            return SchemaVersion==1?(locale=="ru"?row.Russian:row.English):row.Values[index];
        }
        public string TextPair(string russian,string english,string locale)
        {
            if(byPair.TryGetValue(russian,out var pairs)&&pairs.TryGetValue(english,out var row))return Text(row.Key,locale);
            if(locale=="ru")return russian;if(locale=="en")return english;
            throw new ArgumentException("Missing migrated label in this locale.");
        }
        private static string PlaceholderSignature(string text)
        {
            var indices=new SortedSet<int>();
            for(int i=0;i<text.Length;i++)
            {
                if(text[i]=='}'){if(i+1<text.Length&&text[i+1]=='}'){i++;continue;}throw new SerializationException("Unmatched format brace.");}
                if(text[i]!='{')continue;if(i+1<text.Length&&text[i+1]=='{'){i++;continue;}
                int end=text.IndexOf('}',i+1);if(end<0)throw new SerializationException("Unmatched format brace.");
                string token=text.Substring(i+1,end-i-1);int colon=token.IndexOf(':'),comma=token.IndexOf(',');int stop=colon<0?token.Length:colon;if(comma>=0)stop=Math.Min(stop,comma);
                if(!int.TryParse(token.Substring(0,stop),out int index)||index<0||index>31||token.IndexOf('{')>=0)throw new SerializationException("Invalid format placeholder.");
                indices.Add(index);i=end;
            }
            return string.Join(",",indices);
        }
    }
}

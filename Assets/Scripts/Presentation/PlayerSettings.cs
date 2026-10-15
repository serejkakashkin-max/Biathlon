using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Presentation
{
    [DataContract] public sealed class ControlBindingDefinition
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="context",IsRequired=true)] public string Context {get;private set;}
        [DataMember(Name="kind",IsRequired=true)] public string Kind {get;private set;}
        [DataMember(Name="ru",IsRequired=true)] public string Russian {get;private set;}
        [DataMember(Name="en",IsRequired=true)] public string English {get;private set;}
        [DataMember(Name="keyboard",IsRequired=true)] public string Keyboard {get;private set;}
        [DataMember(Name="gamepad",IsRequired=true)] public string Gamepad {get;private set;}
        public string Default(string device)=>device=="keyboard"?Keyboard:device=="gamepad"?Gamepad:throw new ArgumentException("Unknown control device.");
        public string Title(string locale)=>locale=="ru"?Russian:locale=="en"?English:throw new ArgumentException("Unknown control locale.");
    }
    [DataContract] public sealed class ControlPathDefinition
    {
        [DataMember(Name="path",IsRequired=true)] public string Path {get;private set;}
        [DataMember(Name="device",IsRequired=true)] public string Device {get;private set;}
        [DataMember(Name="kind",IsRequired=true)] public string Kind {get;private set;}
        [DataMember(Name="label",IsRequired=true)] public string Label {get;private set;}
    }
    /// <summary>Finite authored control paths. Paths are never interpreted as file paths or arbitrary device expressions.</summary>
    [DataContract] public sealed class InputBindingCatalog
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="actions",IsRequired=true)] private ControlBindingDefinition[] actions=null;
        [DataMember(Name="paths",IsRequired=true)] private ControlPathDefinition[] paths=null;
        private Dictionary<string,ControlBindingDefinition> byAction;
        private Dictionary<string,ControlPathDefinition> byPath;
        public int Count=>actions.Length;
        public ControlBindingDefinition Get(int index)=>actions[index];
        public int PathCount=>paths.Length;
        public ControlPathDefinition PathAt(int index)=>paths[index];
        public ControlBindingDefinition Action(string id)=>byAction.TryGetValue(id,out var value)?value:throw new ArgumentException("Unknown control action.");
        public ControlPathDefinition Path(string path)=>byPath.TryGetValue(path,out var value)?value:throw new ArgumentException("Unsupported control path.");
        public static InputBindingCatalog FromJson(string json)
        {
            var d=SettingsJson.Read<InputBindingCatalog>(json);
            if(d==null||d.SchemaVersion!=1||d.Id!="biathlon_input_bindings_v1"||d.actions==null||d.actions.Length<1||d.actions.Length>80||d.paths==null||d.paths.Length<1||d.paths.Length>256)throw new SerializationException("Invalid binding catalog.");
            d.byAction=new Dictionary<string,ControlBindingDefinition>(StringComparer.Ordinal);d.byPath=new Dictionary<string,ControlPathDefinition>(StringComparer.Ordinal);
            foreach(var p in d.paths)
            {
                if(p==null||string.IsNullOrEmpty(p.Path)||p.Path.Length>80||p.Device!="keyboard"&&p.Device!="gamepad"||p.Kind!="button"&&p.Kind!="axis"||string.IsNullOrWhiteSpace(p.Label)||d.byPath.ContainsKey(p.Path)||
                    p.Path.Contains("*")||p.Path.Contains("..")||p.Path=="<Keyboard>/escape"||p.Path=="<Gamepad>/start")throw new SerializationException("Invalid control path.");
                d.byPath.Add(p.Path,p);
            }
            foreach(var a in d.actions)
            {
                if(a==null||!SettingsJson.StableId(a.Id)||a.Context!="ski"&&a.Context!="shooting"&&a.Context!="global"||a.Kind!="button"&&a.Kind!="axis"||
                    string.IsNullOrWhiteSpace(a.Russian)||string.IsNullOrWhiteSpace(a.English)||d.byAction.ContainsKey(a.Id))throw new SerializationException("Invalid control action.");
                d.byAction.Add(a.Id,a);d.ValidatePath(a.Id,"keyboard",a.Keyboard);d.ValidatePath(a.Id,"gamepad",a.Gamepad);
            }
            new PlayerSettings().Validate(d);return d;
        }
        public void ValidatePath(string action,string device,string path)
        {
            var a=Action(action);var p=Path(path);
            if(p.Device!=device||p.Kind!=a.Kind)throw new ArgumentException("Control path/device/kind mismatch.");
        }
        public string Conflict(PlayerSettings settings,string action,string device,string path)
        {
            ValidatePath(action,device,path);var requested=Action(action);
            foreach(var row in actions)if(row.Id!=action&&(row.Context==requested.Context||row.Context=="global"||requested.Context=="global")&&settings.Binding(this,row.Id,device)==path)return row.Id;
            return null;
        }
    }
    [DataContract] public sealed class InputBindingOverride
    {
        [DataMember(Name="action",IsRequired=true)] public string Action {get;private set;}
        [DataMember(Name="device",IsRequired=true)] public string Device {get;private set;}
        [DataMember(Name="path",IsRequired=true)] public string Path {get;private set;}
        public InputBindingOverride(string action,string device,string path){Action=action;Device=device;Path=path;}
    }
    /// <summary>Product preferences only. Neither career state nor race capability is stored here.</summary>
    [DataContract] public sealed class PlayerSettings
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}=1;
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}="player_settings_v1";
        [DataMember(Name="locale",IsRequired=true)] public string Locale {get;set;}="ru";
        [DataMember(Name="text_size",IsRequired=true)] public int TextSize {get;set;}
        [DataMember(Name="high_contrast",IsRequired=true)] public bool HighContrast {get;set;}
        [DataMember(Name="reduced_motion",IsRequired=true)] public bool ReducedMotion {get;set;}
        [DataMember(Name="quality",IsRequired=true)] public string Quality {get;set;}="high";
        [DataMember(Name="device",IsRequired=true)] public string Device {get;set;}="keyboard";
        [DataMember(Name="aim_sensitivity",IsRequired=true)] public int AimSensitivity {get;set;}=1000;
        [DataMember(Name="invert_aim",IsRequired=true)] public bool InvertAim {get;set;}
        [DataMember(Name="master",IsRequired=true)] public int Master {get;set;}=800;
        [DataMember(Name="effects",IsRequired=true)] public int Effects {get;set;}=850;
        [DataMember(Name="body",IsRequired=true)] public int Body {get;set;}=650;
        [DataMember(Name="ambience",IsRequired=true)] public int Ambience {get;set;}=700;
        [DataMember(Name="ui",IsRequired=true)] public int Interface {get;set;}=650;
        [DataMember(Name="music",IsRequired=true)] public int Music {get;set;}=450;
        [DataMember(Name="quiet",IsRequired=true)] public bool Quiet {get;set;}
        [DataMember(Name="mute_unfocused",IsRequired=true)] public bool MuteUnfocused {get;set;}=true;
        [DataMember(Name="bindings",IsRequired=true)] private InputBindingOverride[] bindings=Array.Empty<InputBindingOverride>();
        public int BindingOverrideCount=>bindings.Length;
        public InputBindingOverride Override(int index)=>bindings[index];
        public string Binding(InputBindingCatalog catalog,string action,string device)
        {foreach(var row in bindings)if(row.Action==action&&row.Device==device)return row.Path;return catalog.Action(action).Default(device);}
        public void Rebind(InputBindingCatalog catalog,string action,string device,string path)
        {
            string conflict=catalog.Conflict(this,action,device,path);if(conflict!=null)throw new ArgumentException("Binding already used by "+conflict+".");
            var next=new List<InputBindingOverride>();foreach(var row in bindings)if(row.Action!=action||row.Device!=device)next.Add(row);
            if(path!=catalog.Action(action).Default(device))next.Add(new InputBindingOverride(action,device,path));
            next.Sort((a,b)=>{int c=StringComparer.Ordinal.Compare(a.Action,b.Action);return c==0?StringComparer.Ordinal.Compare(a.Device,b.Device):c;});bindings=next.ToArray();
        }
        public void ResetBindings(){bindings=Array.Empty<InputBindingOverride>();}
        public void Validate(InputBindingCatalog catalog)
        {
            if(SchemaVersion!=1||Id!="player_settings_v1"||Locale!="ru"&&Locale!="en"||TextSize<0||TextSize>2||Quality!="low"&&Quality!="balanced"&&Quality!="high"||Device!="keyboard"&&Device!="gamepad"||
                AimSensitivity<250||AimSensitivity>2000||!Unit(Master)||!Unit(Effects)||!Unit(Body)||!Unit(Ambience)||!Unit(Interface)||!Unit(Music)||bindings==null||bindings.Length>catalog.Count*2)throw new SerializationException("Invalid player settings.");
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in bindings)
            {
                if(row==null||!keys.Add(row.Action+"/"+row.Device))throw new SerializationException("Duplicate input override.");
                catalog.ValidatePath(row.Action,row.Device,row.Path);
            }
            for(int i=0;i<catalog.Count;i++)foreach(string device in new[]{"keyboard","gamepad"})
                if(catalog.Conflict(this,catalog.Get(i).Id,device,Binding(catalog,catalog.Get(i).Id,device))!=null)throw new SerializationException("Conflicting controls in player settings.");
        }
        private static bool Unit(int n)=>n>=0&&n<=1000;
        public string ToJson(InputBindingCatalog catalog){Validate(catalog);return SettingsJson.Write(this);}
        public PlayerSettings Clone(InputBindingCatalog catalog)=>FromJson(ToJson(catalog),catalog);
        public static PlayerSettings FromJson(string json,InputBindingCatalog catalog){var p=SettingsJson.Read<PlayerSettings>(json);if(p==null)throw new SerializationException("Missing player settings.");p.Validate(catalog);return p;}
    }
    internal static class SettingsJson
    {
        internal static T Read<T>(string json)
        {
            if(json==null)throw new ArgumentNullException(nameof(json));if(json.Length>65536)throw new SerializationException("Settings document too large.");
            Biathlon.Domain.JsonDocumentSyntax.Validate(json);
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        internal static string Write<T>(T value)
        {using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
        internal static bool StableId(string id){if(string.IsNullOrEmpty(id)||id.Length>64)return false;foreach(char c in id)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))return false;return true;}
    }
}

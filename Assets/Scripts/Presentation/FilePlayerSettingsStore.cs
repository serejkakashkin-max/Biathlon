using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Biathlon.Presentation
{
    public enum PlayerSettingsLoadStatus { New, Loaded, BackupRecovered, Invalid, Unavailable }
    public readonly struct PlayerSettingsLoad
    {
        public PlayerSettings Settings {get;}
        public PlayerSettingsLoadStatus Status {get;}
        public PlayerSettingsLoad(PlayerSettings settings,PlayerSettingsLoadStatus status){Settings=settings;Status=status;}
    }
    /// <summary>Validated preferences. Incomplete .tmp files are never selected; a damaged primary cannot replace the good backup.</summary>
    public sealed class FilePlayerSettingsStore
    {
        private readonly string path;
        private readonly InputBindingCatalog catalog;
        public FilePlayerSettingsStore(string directory,InputBindingCatalog catalog)
        {if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("Settings directory required.");this.catalog=catalog??throw new ArgumentNullException(nameof(catalog));path=Path.Combine(Path.GetFullPath(directory),"settings_v1.json");}
        public PlayerSettingsLoad Load(PlayerSettings legacy)
        {
            legacy.Validate(catalog);
            try
            {
                if(File.Exists(path)&&TryRead(path,out var primary))return new PlayerSettingsLoad(primary,PlayerSettingsLoadStatus.Loaded);
                if(File.Exists(path+".bak")&&TryRead(path+".bak",out var backup))return new PlayerSettingsLoad(backup,PlayerSettingsLoadStatus.BackupRecovered);
                return new PlayerSettingsLoad(legacy.Clone(catalog),File.Exists(path)||File.Exists(path+".bak")?PlayerSettingsLoadStatus.Invalid:PlayerSettingsLoadStatus.New);
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){return new PlayerSettingsLoad(legacy.Clone(catalog),PlayerSettingsLoadStatus.Unavailable);}
        }
        private bool TryRead(string file,out PlayerSettings value)
        {
            value=null;
            try
            {if(new FileInfo(file).Length>65536)return false;value=PlayerSettings.FromJson(File.ReadAllText(file,Encoding.UTF8),catalog);return true;}
            catch(Exception e)when(e is SerializationException||e is ArgumentException||e is System.Xml.XmlException){return false;}
        }
        public void Save(PlayerSettings value)
        {
            string json=value.ToJson(catalog);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            using(var settingsLock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
            try
            {
                // Validate both memory and exact bytes before touching the published file.
                byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes,0,bytes.Length);file.Flush(true);}
                if(PlayerSettings.FromJson(File.ReadAllText(temporary,Encoding.UTF8),catalog).ToJson(catalog)!=json)throw new IOException("Settings verification failed.");
                if(!File.Exists(path))File.Move(temporary,path);
                else if(TryRead(path,out _))File.Replace(temporary,path,path+".bak");
                else
                {
                    // A future/invalid version is preserved for support and never copied over a usable backup.
                    File.Move(path,path+".invalid-"+Guid.NewGuid().ToString("N"));File.Move(temporary,path);
                }
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}

using System;
using System.IO;
using System.Text;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    /// <summary>Independent profile files. Only fully validated, flushed files become discoverable.</summary>
    public sealed class FileCareerProfileStore : ICareerProfileStore
    {
        private readonly string directory;
        public FileCareerProfileStore(string directory)
        { if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("Profile directory required.");this.directory=Path.GetFullPath(directory); }
        private string Latest()
        {
            if(!Directory.Exists(directory))return null;
            string latest=null;DateTime stamp=DateTime.MinValue;
            foreach(string path in Directory.GetFiles(directory,"career_*.json",SearchOption.TopDirectoryOnly))
            {
                var time=File.GetLastWriteTimeUtc(path);
                if(latest==null||time>stamp||time==stamp&&StringComparer.Ordinal.Compare(path,latest)>0){latest=path;stamp=time;}
            }
            return latest;
        }
        public bool HasProfile => Latest()!=null;
        public string ReadLatest()
        { var file=Latest();if(file==null)throw new FileNotFoundException("No saved career.");return File.ReadAllText(file,Encoding.UTF8); }
        public void WriteNew(string id,string json)
        {
            if(string.IsNullOrEmpty(id)||id.Length>64||!id.StartsWith("career_",StringComparison.Ordinal))throw new ArgumentException("Profile ID required.");
            foreach(char c in id)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))throw new ArgumentException("Invalid profile ID.");
            if(CareerSave.FromJson(json).Id!=id)throw new ArgumentException("Profile/save identity mismatch.");
            Directory.CreateDirectory(directory);
            string target=Path.Combine(directory,id+".json"),temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                byte[] bytes=new UTF8Encoding(false).GetBytes(json);
                using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                { file.Write(bytes,0,bytes.Length);file.Flush(true); }
                CareerSave.FromJson(File.ReadAllText(temporary,Encoding.UTF8));
                File.Move(temporary,target); // Fails on an existing profile; its bytes remain intact.
            }
            finally { if(File.Exists(temporary))File.Delete(temporary); }
        }
    }
}

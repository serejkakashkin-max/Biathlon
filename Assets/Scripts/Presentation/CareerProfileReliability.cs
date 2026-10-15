using System;
using System.IO;
using System.Text;

namespace Biathlon.Presentation
{
    public enum CareerProfileSource { Primary, Backup, BeforeRaceCheckpoint }
    public enum CareerProfileFailure { NoProfile, Corrupt, UnsupportedVersion, AccessDenied, IoFailure, Changed }
    public sealed class CareerProfileException : IOException
    {
        public CareerProfileFailure Failure {get;}
        public CareerProfileException(CareerProfileFailure failure,string message,Exception inner=null):base(message,inner) {Failure=failure;}
    }
    /// <summary>Safe metadata for UI/reporting; no athlete names, JSON, paths or identifiers.</summary>
    public sealed class CareerProfileReadInfo
    {
        public CareerProfileSource Source {get;}
        public int SourceSaveVersion {get;}
        public int SaveVersion {get;}
        public bool WasMigrated=>SourceSaveVersion!=SaveVersion;
        public bool WasRecovered=>Source!=CareerProfileSource.Primary;
        public string ContentSha256 {get;}
        internal CareerProfileReadInfo(CareerProfileSource source,int sourceVersion,int saveVersion,string sha256)
        {Source=source;SourceSaveVersion=sourceVersion;SaveVersion=saveVersion;ContentSha256=sha256;}
    }
    /// <summary>Optional store capability. Failure leaves the pre-race session unchanged.</summary>
    public interface ICareerProfileCheckpointStore
    {void SaveBeforeRaceCheckpoint(string id,string expectedJson);}

    /// <summary>Filesystem seam for interrupted/disk-full/access-denied tests.</summary>
    public interface ICareerProfileFileSystem
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        string[] GetFiles(string path,string pattern);
        bool FileExists(string path);
        DateTime LastWriteUtc(string path);
        string ReadUtf8(string path);
        void WriteUtf8Flushed(string path,string contents);
        IDisposable AcquireExclusive(string path);
        void MoveNew(string source,string target);
        void Replace(string source,string target,string backup);
        void Delete(string path);
    }
    public sealed class CareerProfileFileSystem : ICareerProfileFileSystem
    {
        public bool DirectoryExists(string path)=>Directory.Exists(path);
        public void CreateDirectory(string path)=>Directory.CreateDirectory(path);
        public string[] GetFiles(string path,string pattern)=>Directory.GetFiles(path,pattern,SearchOption.TopDirectoryOnly);
        public bool FileExists(string path)=>File.Exists(path);
        public DateTime LastWriteUtc(string path)=>File.GetLastWriteTimeUtc(path);
        public string ReadUtf8(string path)=>File.ReadAllText(path,new UTF8Encoding(false,true));
        public void WriteUtf8Flushed(string path,string contents)
        {
            byte[] bytes=new UTF8Encoding(false,true).GetBytes(contents);
            using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {file.Write(bytes,0,bytes.Length);file.Flush(true);}
        }
        public IDisposable AcquireExclusive(string path)=>new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        public void MoveNew(string source,string target)=>File.Move(source,target);
        public void Replace(string source,string target,string backup)=>File.Replace(source,target,backup);
        public void Delete(string path)=>File.Delete(path);
    }
}

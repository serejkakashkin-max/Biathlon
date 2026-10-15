using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    /// <summary>Verified atomic replacement, compare-and-swap and same-profile recovery.
    /// Reads never mutate disk; abandoned temporary files are never authoritative.</summary>
    public sealed class FileCareerProfileStore : ICareerProfileStore,ICareerProfileCheckpointStore
    {
        private readonly string directory;
        private readonly ICareerProfileFileSystem files;
        public CareerProfileReadInfo LastRead {get;private set;}
        /// <summary>Current successful publication source; LastRead retains recovery history.</summary>
        public CareerProfileSource? CurrentSource {get;private set;}
        public FileCareerProfileStore(string directory):this(directory,new CareerProfileFileSystem()) { }
        public FileCareerProfileStore(string directory,ICareerProfileFileSystem files)
        {
            if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("Profile directory required.");
            this.directory=Path.GetFullPath(directory);this.files=files??throw new ArgumentNullException(nameof(files));
        }
        private static void ValidateId(string id)
        {
            if(string.IsNullOrEmpty(id)||id.Length>64||!id.StartsWith("career_",StringComparison.Ordinal))throw new ArgumentException("Profile ID required.");
            foreach(char c in id)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))throw new ArgumentException("Invalid profile ID.");
        }
        private string Target(string id) {ValidateId(id);return Path.Combine(directory,id+".json");}
        private static string Hash(string json)
        {
            using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-",string.Empty).ToLowerInvariant();
        }
        private string Latest()
        {
            if(!files.DirectoryExists(directory))return null;
            var families=new HashSet<string>(StringComparer.Ordinal);
            foreach(string path in files.GetFiles(directory,"career_*"))
            {
                string name=Path.GetFileName(path);
                if(name.EndsWith(".json.bak",StringComparison.Ordinal))name=name.Substring(0,name.Length-4);
                else if(name.EndsWith(".json.checkpoint",StringComparison.Ordinal))name=name.Substring(0,name.Length-11);
                else if(!name.EndsWith(".json",StringComparison.Ordinal))continue;
                string id=name.Substring(0,name.Length-5);
                try {ValidateId(id);}catch(ArgumentException){continue;}
                families.Add(Path.Combine(directory,name));
            }
            string latest=null;DateTime stamp=DateTime.MinValue;
            foreach(string target in families)
            {
                bool primaryExists=files.FileExists(target);
                DateTime time=primaryExists?files.LastWriteUtc(target):DateTime.MinValue;
                // Recovery artifacts do not reorder an intact independent career.
                if(!primaryExists)foreach(string suffix in new[]{".bak",".checkpoint"})
                    if(files.FileExists(target+suffix)&&files.LastWriteUtc(target+suffix)>time)time=files.LastWriteUtc(target+suffix);
                if(latest==null||time>stamp||time==stamp&&StringComparer.Ordinal.Compare(target,latest)>0){latest=target;stamp=time;}
            }
            return latest;
        }
        // Damaged saves keep Continue available for a useful recovery message.
        public bool HasProfile
        {
            get
            {
                try {return Latest()!=null;}
                // Keep the menu usable. Continue will expose the actual error;
                // an inaccessible directory must not look like a new empty game.
                catch(UnauthorizedAccessException){return true;}
                catch(IOException){return true;}
            }
        }
        public string ReadLatest()
        {
            LastRead=null;
            try
            {
                string target=Latest();if(target==null)throw new CareerProfileException(CareerProfileFailure.NoProfile,"No saved career.");
                using(files.AcquireExclusive(target+".lock"))
                {
                    var candidate=ReadFamily(target);
                    LastRead=new CareerProfileReadInfo(candidate.Source,candidate.Migration.SourceVersion,CareerSave.CurrentVersion,Hash(candidate.Json));
                    CurrentSource=candidate.Source;
                    return candidate.Json;
                }
            }
            catch(CareerProfileException){throw;}
            catch(UnauthorizedAccessException e){throw new CareerProfileException(CareerProfileFailure.AccessDenied,"The saved career cannot be accessed. Its files were preserved.",e);}
            catch(IOException e){throw new CareerProfileException(CareerProfileFailure.IoFailure,"The saved career could not be read. Its files were preserved.",e);}
        }
        private sealed class Candidate
        {internal string Json;internal CareerSaveMigrationResult Migration;internal CareerProfileSource Source;}
        private Candidate ReadCandidate(string path,string id,CareerProfileSource source)
        {
            if(!files.FileExists(path))return null;
            try
            {
                string json=files.ReadUtf8(path);var migration=CareerSaveMigrations.Load(json);
                if(migration.Save.Id!=id)throw new SerializationException("Profile/save identity mismatch.");
                return new Candidate{Json=json,Migration=migration,Source=source};
            }
            catch(UnsupportedCareerSaveVersionException e){throw new CareerProfileException(CareerProfileFailure.UnsupportedVersion,"This career requires a different game version. Its files were preserved.",e);}
            catch(SerializationException){return null;}
            catch(ArgumentException){return null;}
            catch(UnauthorizedAccessException e){throw new CareerProfileException(CareerProfileFailure.AccessDenied,"The saved career cannot be accessed. Its files were preserved.",e);}
            catch(IOException e){throw new CareerProfileException(CareerProfileFailure.IoFailure,"The saved career could not be read. Its files were preserved.",e);}
        }
        private Candidate ReadFamily(string target)
        {
            string id=Path.GetFileNameWithoutExtension(target);
            var candidate=ReadCandidate(target,id,CareerProfileSource.Primary)
                ??ReadCandidate(target+".bak",id,CareerProfileSource.Backup)
                ??ReadCandidate(target+".checkpoint",id,CareerProfileSource.BeforeRaceCheckpoint);
            if(candidate==null)throw new CareerProfileException(CareerProfileFailure.Corrupt,"No valid copy of this career could be recovered. Its files were preserved.");
            return candidate;
        }
        private string VerifyIdentity(string id,string json)
        {var save=CareerSave.FromJson(json);if(save.Id!=id)throw new ArgumentException("Profile/save identity mismatch.");return save.ToJson();}
        private string WriteVerifiedTemporary(string target,string id,string json)
        {
            string temporary=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                files.WriteUtf8Flushed(temporary,json);
                string actual=files.ReadUtf8(temporary);
                if(!StringComparer.Ordinal.Equals(actual,json))throw new IOException("Saved career verification failed; the previous save was preserved.");
                VerifyIdentity(id,actual);return temporary;
            }
            catch {Cleanup(temporary);throw;}
        }
        private void Cleanup(string temporary)
        {
            // Cleanup failure must not make an already committed command appear failed.
            try {if(temporary!=null&&files.FileExists(temporary))files.Delete(temporary);}
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
        public void WriteNew(string id,string json)
        {
            string target=Target(id);VerifyIdentity(id,json);files.CreateDirectory(directory);
            using(files.AcquireExclusive(target+".lock"))
            {
                if(files.FileExists(target)||files.FileExists(target+".bak")||files.FileExists(target+".checkpoint"))throw new IOException("This career profile already exists.");
                string temporary=WriteVerifiedTemporary(target,id,json);
                try {files.MoveNew(temporary,target);CurrentSource=CareerProfileSource.Primary;}finally{Cleanup(temporary);}
            }
        }
        private Candidate RequireExpected(string target,string expectedCanonical)
        {
            var current=ReadFamily(target);
            if(current.Migration.Save.ToJson()!=expectedCanonical)
                throw new CareerProfileException(CareerProfileFailure.Changed,"Career changed since this command was prepared.");
            return current;
        }
        public void Replace(string id,string expectedJson,string json)
        {
            string target=Target(id),expected=VerifyIdentity(id,expectedJson);VerifyIdentity(id,json);
            using(files.AcquireExclusive(target+".lock"))
            {
                var current=RequireExpected(target,expected);
                string temporary=WriteVerifiedTemporary(target,id,json);
                try
                {
                    if(current.Source==CareerProfileSource.Primary)files.Replace(temporary,target,target+".bak");
                    else
                    {
                        // Never copy corrupt primary bytes over a known-good backup.
                        if(current.Source!=CareerProfileSource.Backup)WriteSidecar(target+".bak",id,current.Json);
                        if(files.FileExists(target))files.Replace(temporary,target,target+".corrupt-"+Guid.NewGuid().ToString("N"));
                        else files.MoveNew(temporary,target);
                    }
                    CurrentSource=CareerProfileSource.Primary;
                }
                finally {Cleanup(temporary);}
            }
        }
        private void WriteSidecar(string target,string id,string json)
        {
            string temporary=WriteVerifiedTemporary(target,id,json);
            try
            {
                if(files.FileExists(target))files.Replace(temporary,target,null);
                else files.MoveNew(temporary,target);
            }
            finally {Cleanup(temporary);}
        }
        public void SaveBeforeRaceCheckpoint(string id,string expectedJson)
        {
            string target=Target(id),expected=VerifyIdentity(id,expectedJson);
            using(files.AcquireExclusive(target+".lock"))
            {
                var current=RequireExpected(target,expected);
                // Begin → abandon → Begin may revisit the same committed
                // source. Revalidate the durable copy, then reuse its exact
                // bytes rather than replacing an identical Windows file.
                var existing=ReadCandidate(target+".checkpoint",id,CareerProfileSource.BeforeRaceCheckpoint);
                if(existing!=null&&StringComparer.Ordinal.Equals(existing.Json,current.Json))return;
                WriteSidecar(target+".checkpoint",id,current.Json);
            }
        }
    }
}

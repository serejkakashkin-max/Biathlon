using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

/// <summary>Cold CLI evidence only. Stream complete accepted command batches and exact
/// public state/event bytes, reconstruct from persisted resource snapshots, and replay
/// through the authoritative field without advancing its newly constructed AI runner.</summary>
public static class ProductionReplayHarness
{
    public const string Kind="production_venue_stream_commands_v1";
    private const long MaximumTicks=120000;
    private const int MaximumRecordBytes=8*1024*1024;
    public const string ContextSuffix=".context.json.gz";
    private const long MaximumContextBytes=128*1024*1024;
    private const long MaximumCompressedContextBytes=16*1024*1024;
    private static readonly JsonSerializerOptions Json=Options(false);
    private static readonly JsonSerializerOptions Pretty=Options(true);
    private static JsonSerializerOptions Options(bool pretty)
    {var options=new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,WriteIndented=pretty};options.Converters.Add(new FinishConverter());return options;}
    private sealed class FinishConverter:JsonConverter<FinishTime>
    {
        public override FinishTime Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>throw new NotSupportedException();
        public override void Write(Utf8JsonWriter writer,FinishTime value,JsonSerializerOptions options)
        {writer.WriteStartObject();writer.WriteNumber("whole_ticks",value.WholeTicks);writer.WriteNumber("fraction_parts",value.FractionParts);writer.WriteNumber("tick_frequency",value.TickFrequency);writer.WriteEndObject();}
    }
    public static int Run(string[] args)
    {
        string output=null;
        try
        {
            if(args.Length==3&&args[0]=="replay")
            {string tape=Path.GetFullPath(args[1]),report=Path.GetFullPath(args[2]);ValidatePaths(tape,report);output=report;Replay(tape,output);return 0;}
            if(args.Length<11||args[0]!="record")throw new ArgumentException(
                "record <venue-root> <core-root> <manifest> <profile> <route> <conditions> <count> <tick-limit> <tape.gz> <output.json> [--sprint-result <source.json>] [--max-tape-mib <16..4096>] [--max-raw-mib <16..65536>]\nreplay <tape.gz> <output.json>");
            string tapeFile=Path.GetFullPath(args[9]),reportFile=Path.GetFullPath(args[10]);ValidatePaths(tapeFile,reportFile);output=reportFile;
            long compressedLimit=256L*1024*1024,rawLimit=4096L*1024*1024;string qualification=null;
            for(int i=11;i<args.Length;i+=2)
            {
                if(i+1==args.Length)throw new ArgumentException("Option requires a value.");
                if(args[i]=="--sprint-result")qualification=File.ReadAllText(args[i+1]);
                else if(args[i]=="--max-tape-mib")compressedLimit=BoundedMiB(args[i+1],16,4096);
                else if(args[i]=="--max-raw-mib")rawLimit=BoundedMiB(args[i+1],16,65536);
                else throw new ArgumentException("Unknown option "+args[i]);
            }
            int count=int.Parse(args[7],CultureInfo.InvariantCulture);long ticks=long.Parse(args[8],CultureInfo.InvariantCulture);
            if(ticks<0||ticks>MaximumTicks)throw new ArgumentOutOfRangeException("tick-limit","Tick limit must be 0..120000.");
            Record(args[1],args[2],args[3],args[4],args[5],args[6],count,ticks,tapeFile,output,qualification,compressedLimit,rawLimit);
            return 0;
        }
        catch(Exception error)
        {
            if(output!=null)Save(output,new {schema_version=1,id="production_venue_stream_replay_report_v1",status="FAILED",error=error.ToString()});
            Console.Error.WriteLine(error);return 1;
        }
    }
    private static long BoundedMiB(string value,int minimum,int maximum)
    {int n=int.Parse(value,CultureInfo.InvariantCulture);if(n<minimum||n>maximum)throw new ArgumentOutOfRangeException(value);return n*1024L*1024;}
    private static void ValidatePaths(string tape,string report)
    {if(string.Equals(tape,report,StringComparison.OrdinalIgnoreCase)||string.Equals(tape+ContextSuffix,report,StringComparison.OrdinalIgnoreCase))
        throw new ArgumentException("Tape, context and report paths must differ.");}
    public static PursuitQualificationSnapshot Qualification(string source,CompetitionProfile profile,string venue)
    {
        if(profile.Format!=CompetitionFormat.Pursuit)throw new ArgumentException("Sprint source requires a Pursuit target.");
        using(var document=JsonDocument.Parse(source))
            if(document.RootElement.TryGetProperty("result_json",out var result))
            {if(result.ValueKind!=JsonValueKind.String)throw new ArgumentException("A completed actual Sprint result is required.");source=result.GetString();}
        var sprint=SprintResultSnapshot.FromJson(source);
        return PursuitQualificationSnapshot.Create(venue+"_persisted_sprint_qualification_v1",sprint,profile);
    }
    private sealed class Context
    {
        public Context() { }
        public int SchemaVersion {get;set;}=1;
        public string Id {get;set;}="production_venue_stream_context_v1";
        public string Kind {get;set;}=ProductionReplayHarness.Kind;
        public string Manifest {get;set;} public string Profile {get;set;} public string Route {get;set;} public string Conditions {get;set;}
        public int Count {get;set;} public int Frequency {get;set;}=20;public ulong Seed {get;set;}=123;
        public string QualificationJson {get;set;}
        public Dictionary<string,string> Resources {get;set;}=new Dictionary<string,string>(StringComparer.Ordinal);
        public Dictionary<string,string> ResourceSha256 {get;set;}=new Dictionary<string,string>(StringComparer.Ordinal);
        public Dictionary<string,string> AssemblySha256 {get;set;}
        public Dictionary<string,string> SourceCodeSha256 {get;set;}
        public string FieldContextJson {get;set;}
        public string InitialPublicState {get;set;}
        public string InitialFieldEvents {get;set;}
    }
    private static Dictionary<string,string> Assemblies()
    {
        var assemblies=new Dictionary<string,Assembly>(StringComparer.Ordinal);
        void Visit(Assembly assembly)
        {
            if(!assemblies.TryAdd(assembly.GetName().Name,assembly))return;
            foreach(var dependency in assembly.GetReferencedAssemblies())if(dependency.Name.StartsWith("Biathlon.",StringComparison.Ordinal))Visit(Assembly.Load(dependency));
        }
        Visit(typeof(ProductionReplayHarness).Assembly);
        return assemblies.OrderBy(a=>a.Key,StringComparer.Ordinal).ToDictionary(a=>a.Key,a=>HashFile(a.Value.Location),StringComparer.Ordinal);
    }
    private static Dictionary<string,string> Sources()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"AGENTS.md")))root=root.Parent;
        if(root==null)return new Dictionary<string,string>();
        var files=new List<string>();
        foreach(string area in new[]{"Domain","Simulation","AI","Replay","Career"})
            files.AddRange(Directory.GetFiles(Path.Combine(root.FullName,"Assets/Scripts",area),"*.cs",SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Path.Combine(root.FullName,"dotnet/Biathlon.VenueLab"),"*.cs",SearchOption.TopDirectoryOnly));
        files.Add(Path.Combine(root.FullName,"dotnet/Biathlon.VenueLab/Biathlon.VenueLab.csproj"));
        return files.OrderBy(path=>path,StringComparer.Ordinal).ToDictionary(path=>Path.GetRelativePath(root.FullName,path).Replace('\\','/'),HashFile,StringComparer.Ordinal);
    }
    private static PackAiRaceRunner Build(Context context,EventBatch events,Func<string,string> read)
    {
        var venue=new VenueAuthoringPlan(VenueManifest.FromJson(read(context.Manifest)),read);
        var profile=CompetitionProfile.FromJson(read(context.Profile));
        var route=RaceRoutePlan.FromJson(read(context.Route));var conditions=RaceSnowConditions.FromJson(read(context.Conditions));
        PursuitQualificationSnapshot qualification=null;
        if(context.QualificationJson!=null)
        {
            using(var doc=JsonDocument.Parse(context.QualificationJson))
                qualification=PursuitQualificationSnapshot.FromJson(context.QualificationJson,doc.RootElement.GetProperty("source_result_id").GetString(),profile);
            if(qualification.Count!=context.Count)throw new ArgumentException("Exact qualification roster/count required.");
        }
        string Core(string name)=>read("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        return ProductionVenueRaceFactory.Create(venue,profile,route,conditions,Core,events,new AiDecisionCounts(),qualification:qualification,
            count:context.Count,frequency:context.Frequency,seed:context.Seed);
    }
    private static void Record(string venueRoot,string coreRoot,string manifest,string profile,string route,string conditions,int count,long tickLimit,
        string tapePath,string reportPath,string sprintSource,long compressedLimit,long rawLimit)
    {
        if(count<1||count>60)throw new ArgumentOutOfRangeException(nameof(count));
        if(tapePath==reportPath||tapePath+ContextSuffix==reportPath)throw new ArgumentException("Tape, context and report paths must differ.");
        venueRoot=Path.GetFullPath(venueRoot);coreRoot=Path.GetFullPath(coreRoot);
        var context=new Context {Manifest=manifest,Profile=profile,Route=route,Conditions=conditions,Count=count,AssemblySha256=Assemblies(),SourceCodeSha256=Sources()};
        string Read(string id)
        {
            VenueManifest.ResourceId(id);
            if(context.Resources.TryGetValue(id,out var cached))return cached;
            string relative=id.Replace('/',Path.DirectorySeparatorChar)+".json";string path=Path.Combine(venueRoot,relative);
            if(!File.Exists(path))path=Path.Combine(coreRoot,relative);
            byte[] bytes=File.ReadAllBytes(path);string value=File.ReadAllText(path);
            context.Resources.Add(id,value);context.ResourceSha256.Add(id,Hash(bytes));return value;
        }
        if(sprintSource!=null)context.QualificationJson=Qualification(sprintSource,CompetitionProfile.FromJson(Read(profile)),VenueManifest.FromJson(Read(manifest)).Id).ToJson();
        var events=new EventBatch();var runner=Build(context,events,Read);var field=runner.Field;
        context.FieldContextJson=field.ContextJson;context.InitialPublicState=PublicState(field);context.InitialFieldEvents=events.Take();
        // Persist a complete, caller-owned snapshot before any accepted command is advanced.
        Directory.CreateDirectory(Path.GetDirectoryName(tapePath));
        string contextFile=tapePath+ContextSuffix;long contextRawBytes;
        using(var file=File.Create(contextFile))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))
        using(var bounded=new CapacityStream(gzip,MaximumContextBytes,true))
        {JsonSerializer.Serialize(bounded,context,Json);contextRawBytes=bounded.Transferred;}
        long contextCompressedBytes=new FileInfo(contextFile).Length;
        if(contextCompressedBytes>MaximumCompressedContextBytes)throw new InvalidOperationException("Compressed context exceeds 16 MiB.");
        string contextHash=HashFile(contextFile);
        long rawBytes=0,peakRecord=0;string stopped="TICK_LIMIT";long maximumStationary=0;var stationary=new long[count];var previous=new long[count];
        var watch=System.Diagnostics.Stopwatch.StartNew();
        using(var file=new FileStream(tapePath,FileMode.Create,FileAccess.Write,FileShare.Read))
        using(var gzip=new GZipStream(file,CompressionLevel.Fastest,true))
        using(var writer=new BinaryWriter(gzip,Encoding.UTF8,true))
        {
            writer.Write(Kind);writer.Write(1);writer.Write(contextHash);writer.Write(count);
            while(!field.IsComplete&&field.Tick.Value<tickLimit)
            {
                // Reserving the hard maximum keeps the next complete batch and footer atomic
                // with respect to CLI capacity. This is an evidence cap, never game tuning.
                if(rawBytes>rawLimit-2L*MaximumRecordBytes-65536||file.Position>compressedLimit-2L*MaximumRecordBytes-65536)
                {stopped="FILE_CAPACITY_PREFIX";break;}
                for(int i=0;i<count;i++)previous[i]=field.GetAthlete(i).Motion.DistanceNanometers;
                runner.Step();
                using(var record=new MemoryStream())
                {
                    using(var row=new BinaryWriter(record,Encoding.UTF8,true))
                    {
                        row.Write(field.Tick.Value);row.Write(count);
                        for(int i=0;i<count;i++)WriteCommand(row,runner.GetLastInput(i),runner.GetLastLateral(i));
                        row.Write(PublicState(field));row.Write(events.Take());
                    }
                    if(record.Length>MaximumRecordBytes)throw new InvalidOperationException("A tick exceeds the 8 MiB record contract.");
                    writer.Write((int)record.Length);record.Position=0;record.CopyTo(gzip);writer.Write(SHA256.HashData(record.GetBuffer().AsSpan(0,(int)record.Length)));
                    rawBytes+=record.Length+36;peakRecord=Math.Max(peakRecord,record.Length);
                }
                for(int i=0;i<count;i++)
                {var actor=field.GetAthlete(i);stationary[i]=!actor.HasFinished&&field.IsOnCourse(i)&&actor.ActiveShooting==null&&actor.Motion.DistanceNanometers==previous[i]?stationary[i]+1:0;
                 maximumStationary=Math.Max(maximumStationary,stationary[i]);}
                if(field.Tick.Value%1000==0)Console.WriteLine("record tick="+field.Tick.Value+" raw_mib="+(rawBytes/1048576d).ToString("F1",CultureInfo.InvariantCulture));
            }
            string result=Result(field)??"";if(Encoding.UTF8.GetByteCount(result)>MaximumRecordBytes)throw new InvalidOperationException("Result footer exceeds capacity.");
            writer.Write(-1);writer.Write(field.Tick.Value);writer.Write(field.IsComplete);writer.Write(result);writer.Write(events.FieldCount);
        }
        long compressedBytes=new FileInfo(tapePath).Length;
        if(compressedBytes>compressedLimit)throw new InvalidOperationException("Compressed tape capacity exceeded.");
        var after=Sources();if(after.Count!=context.SourceCodeSha256.Count||after.Any(pair=>!context.SourceCodeSha256.TryGetValue(pair.Key,out var hash)||hash!=pair.Value))
            throw new InvalidOperationException("Source files changed during recording; rebuild evidence under one frozen source version.");
        Save(reportPath,new {schema_version=1,id="production_venue_stream_record_report_v1",status=field.IsComplete?"RACE_COMPLETE_REPLAY_NOT_YET_PROVED":"PREFIX_ONLY_REPLAY_NOT_YET_PROVED",
            kind=Kind,ticks=field.Tick.Value,requested_tick_limit=tickLimit,athletes=count,stopped_reason=field.IsComplete?"RACE_COMPLETE":stopped,
            tape_file=tapePath,tape_sha256=HashFile(tapePath),context_file=contextFile,context_sha256=contextHash,
            context_compressed_bytes=contextCompressedBytes,context_uncompressed_bytes=contextRawBytes,
            context_compressed_capacity_bytes=MaximumCompressedContextBytes,context_uncompressed_capacity_bytes=MaximumContextBytes,
            compressed_bytes=compressedBytes,uncompressed_record_bytes=rawBytes,
            compressed_capacity_bytes=compressedLimit,uncompressed_capacity_bytes=rawLimit,peak_tick_record_bytes=peakRecord,
            field_event_count=events.FieldCount,excluded_ai_event_count=events.ExcludedCount,max_stationary_ticks=maximumStationary,
            elapsed_wall_seconds=watch.Elapsed.TotalSeconds,resource_sha256=context.ResourceSha256,assembly_sha256=context.AssemblySha256,
            source_code_sha256=context.SourceCodeSha256,result_json=Result(field)});
        Console.WriteLine("record "+(field.IsComplete?"complete":"prefix")+" ticks="+field.Tick.Value+" bytes="+compressedBytes);
    }
    private static void Replay(string tapePath,string reportPath)
    {
        if(reportPath==tapePath||reportPath==tapePath+ContextSuffix)throw new ArgumentException("Replay report must differ from tape/context.");
        if(new FileInfo(tapePath).Length>4096L*1024*1024)throw new InvalidOperationException("Compressed input tape exceeds 4096 MiB.");
        var contextFile=tapePath+ContextSuffix;
        if(new FileInfo(contextFile).Length>MaximumCompressedContextBytes)throw new InvalidOperationException("Compressed context exceeds 16 MiB.");
        string contextHash=HashFile(contextFile);int persistedCount;
        using(var file=File.OpenRead(tapePath))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var reader=new BinaryReader(gzip,Encoding.UTF8))
        {
            if(ReadText(reader,256)!=Kind||reader.ReadInt32()!=1||ReadText(reader,64)!=contextHash)
                throw new InvalidOperationException("Tape schema or context hash mismatch.");
            persistedCount=reader.ReadInt32();
        }
        Context context;long contextRawBytes;
        using(var file=File.OpenRead(contextFile))using(var gzip=new GZipStream(file,CompressionMode.Decompress))
        using(var bounded=new CapacityStream(gzip,MaximumContextBytes,false))
        {context=JsonSerializer.Deserialize<Context>(bounded,Json);contextRawBytes=bounded.Transferred;}
        if(context==null||context.SchemaVersion!=1||context.Kind!=Kind||context.Count<1||context.Count>60)throw new InvalidOperationException("Invalid context contract.");
        if(context.Count!=persistedCount)throw new InvalidOperationException("Persisted context/tape roster mismatch.");
        var loaded=Assemblies();
        if(loaded.Count!=context.AssemblySha256.Count||loaded.Any(pair=>!context.AssemblySha256.TryGetValue(pair.Key,out var hash)||hash!=pair.Value))
            throw new InvalidOperationException("Replay assembly/source snapshot differs; rebuild evidence under one frozen source version.");
        string Read(string id)=>context.Resources.TryGetValue(id,out var value)?value:throw new InvalidOperationException("Missing persisted resource "+id);
        var events=new EventBatch();var rebuilt=Build(context,events,Read);var field=rebuilt.Field;
        if(field.ContextJson!=context.FieldContextJson||PublicState(field)!=context.InitialPublicState||events.Take()!=context.InitialFieldEvents)
            throw new InvalidOperationException("Reconstructed actual factory context/initial state differs.");
        // Same context/roster validation as the existing run-compressed replay contract.
        var contextProbe=new PackSprintRaceCommandTape("production_stream_context_probe_v1",field,capacity:context.Count);
        _=contextProbe.ContextJson;
        var commands=new SprintAthleteInput[context.Count];var lateral=new double[context.Count];long records=0;bool complete=false;
        var watch=System.Diagnostics.Stopwatch.StartNew();
        using(var file=File.OpenRead(tapePath))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var reader=new BinaryReader(gzip,Encoding.UTF8))
        {
            if(ReadText(reader,256)!=Kind||reader.ReadInt32()!=1||ReadText(reader,64)!=HashFile(contextFile)||reader.ReadInt32()!=context.Count)
                throw new InvalidOperationException("Tape schema, context hash or roster mismatch.");
            while(true)
            {
                int length=reader.ReadInt32();if(length==-1)break;
                if(length<=0||length>MaximumRecordBytes||records>=MaximumTicks)throw new InvalidOperationException("Record length/tick capacity invalid.");
                byte[] bytes=reader.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException("Truncated tick record.");
                byte[] checksum=reader.ReadBytes(32);if(checksum.Length!=32||!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes),checksum))
                    throw new InvalidOperationException("Tick record checksum differs at tick "+(records+1)+".");
                using(var row=new BinaryReader(new MemoryStream(bytes),Encoding.UTF8))
                {
                    long tick=row.ReadInt64();if(tick!=field.Tick.Value+1||row.ReadInt32()!=context.Count)throw new InvalidOperationException("Tick sequence/roster differs.");
                    for(int i=0;i<context.Count;i++)commands[i]=ReadCommand(row,new SimulationTick(tick),field.GetAthlete(i).AthleteId,out lateral[i]);
                    string state=ReadText(row),expectedEvents=ReadText(row);if(row.BaseStream.Position!=row.BaseStream.Length)throw new InvalidOperationException("Trailing tick bytes.");
                    field.Step(commands,lateral);
                    if(PublicState(field)!=state)throw new InvalidOperationException("Public state differs at tick "+tick+" (physiology/coordinates/lifecycle/shooting/arrival/reservation/history).");
                    if(events.Take()!=expectedEvents)throw new InvalidOperationException("Ordered field events differ at tick "+tick+".");
                }
                records++;
                if(records%1000==0)Console.WriteLine("replay tick="+records);
            }
            long ticks=reader.ReadInt64();complete=reader.ReadBoolean();string result=ReadText(reader);long eventCount=reader.ReadInt64();
            if(ticks!=records||ticks!=field.Tick.Value||complete!=field.IsComplete||result!=(Result(field)??"")||eventCount!=events.FieldCount||reader.BaseStream.ReadByte()!=-1)
                throw new InvalidOperationException("Footer completion, exact result/event total or trailing bytes differ.");
        }
        Save(reportPath,new {schema_version=1,id="production_venue_stream_replay_report_v1",status=complete?"PASS_COMPLETE_EXACT_REPLAY":"PASS_PREFIX_EXACT_REPLAY",
            kind=Kind,ticks=records,athletes=context.Count,complete_race=complete,per_tick_public_state="EXACT_SERIALIZED_BYTE_EQUALITY",
            public_contract="LastFrame including all physiology/shooting outputs; coordinates, motion, lateral, draft, junction tickets, lifecycle, range path/lane, masks, arrival/release/reservation history, checkpoint splits/timing; exact final result when complete",
            ordered_field_events="EXACT_SERIALIZED_BYTE_EQUALITY",ai_event_filter=EventBatch.ExcludedNames,tape_sha256=HashFile(tapePath),
            context_file=contextFile,context_sha256=contextHash,context_compressed_bytes=new FileInfo(contextFile).Length,context_uncompressed_bytes=contextRawBytes,
            field_event_count=events.FieldCount,replay_ai_steps=0,elapsed_wall_seconds=watch.Elapsed.TotalSeconds,resource_sha256=context.ResourceSha256,
            assembly_sha256=context.AssemblySha256,source_code_sha256=context.SourceCodeSha256,result_json=Result(field)});
        Console.WriteLine("replay PASS "+(complete?"complete":"prefix")+" ticks="+records);
    }
    public static void WriteCommand(BinaryWriter writer,SprintAthleteInput input,double lateral)
    {
        var ski=input.Ski;writer.Write(ski.Effort.RequestedEffort);writer.Write(ski.Form);writer.Write(ski.LineQuality);writer.Write(ski.HeadwindMetersPerSecond);
        writer.Write(ski.TuckRequested);writer.Write(ski.SprintRequested);writer.Write(ski.TechniqueId??"");writer.Write(ski.MovementEfficiency);
        writer.Write(input.Aim.Horizontal);writer.Write(input.Aim.Vertical);writer.Write(input.Aim.FineControl);writer.Write(input.Trigger);writer.Write(input.HoldBreath);
        writer.Write(input.Wind.EastMetersPerSecond);writer.Write(input.Wind.NorthMetersPerSecond);
        writer.Write(input.Pressure.RaceImportance);writer.Write(input.Pressure.PositionImportance);writer.Write(input.Pressure.RivalProximity);writer.Write(input.Pressure.Confidence);
        writer.Write(input.Pressure.FinalStanding);writer.Write(input.Pressure.FirstPodium);writer.Write(lateral);
    }
    public static SprintAthleteInput ReadCommand(BinaryReader reader,SimulationTick tick,AthleteId id,out double lateral)
    {
        int effort=reader.ReadInt32();double form=reader.ReadDouble(),line=reader.ReadDouble(),headwind=reader.ReadDouble();
        bool tuck=reader.ReadBoolean(),sprint=reader.ReadBoolean();string technique=ReadText(reader,4096);double efficiency=reader.ReadDouble();
        int x=reader.ReadInt32(),y=reader.ReadInt32();bool fine=reader.ReadBoolean(),trigger=reader.ReadBoolean(),hold=reader.ReadBoolean();
        var wind=new WindVector(reader.ReadDouble(),reader.ReadDouble());var pressure=new ShootingPressureContext(reader.ReadDouble(),reader.ReadDouble(),reader.ReadDouble(),reader.ReadDouble(),reader.ReadBoolean(),reader.ReadBoolean());
        lateral=reader.ReadDouble();
        return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,effort),form,line,headwind,tuck,sprint,technique.Length==0?null:technique,efficiency),
            new WeaponAimCommand(tick,id,x,y,fine),trigger,hold,wind,pressure);
    }
    private static string Result(PackSprintFieldSession field)=>!field.IsComplete?null:field.Profile.Format==CompetitionFormat.MassStart?field.MassStartResult.ToJson():
        field.Profile.Format==CompetitionFormat.Pursuit?field.PursuitResult.ToJson():field.Result.ToJson();
    public static string PublicState(PackSprintFieldSession field)
    {
        var actors=new object[field.Count];var reservations=field.RangeReservations;
        bool arrivalPolicy=field.UsesReusableRangeMats||field.Profile.Format==CompetitionFormat.MassStart;
        var arrivals=new int[field.Profile.BoutCount];for(int b=0;b<arrivals.Length;b++)arrivals[b]=arrivalPolicy?field.GetRangeArrivalCount(b):-1;
        for(int i=0;i<field.Count;i++)
        {
            var actor=field.GetAthlete(i);var point=field.GetPhysicalPoint(i);var life=actor.Lifecycle;
            var splits=new object[actor.Checkpoints.Count];
            for(int s=0;s<splits.Length;s++)
            {var value=actor.Checkpoints.Get(s);splits[s]=new {value.Ordinal,value.Athlete,value.GlobalTime,value.ElapsedTime,value.IsValid,
                board=field.Timing.Get(actor.AthleteId,s),leader=field.Timing.Leader(s),gap=field.Timing.GapSeconds(actor.AthleteId,s)};}
            var histories=new object[field.Profile.BoutCount];var masks=new int[histories.Length];
            for(int b=0;b<histories.Length;b++)
            {
                masks[b]=actor.GetBoutClosedMask(b);
                if(reservations!=null)histories[b]=reservations.GetRecord(i,b);
                else
                {
                    PhysicalRangeArrival? receipt=null;
                    if(arrivals[b]>0)try{receipt=field.GetAcceptedRangeArrival(i,b);}catch(InvalidOperationException){}
                    histories[b]=receipt;
                }
            }
            var shooting=actor.ActiveShooting;
            actors[i]=new {actor.AthleteId,actor.Tick,actor.ScheduledStart,actor.LastFrame,actor.PhysiologyState,actor.Motion,actor.RouteNanometers,
                actor.InitialStartApproachNanometers,actor.RemainingStartApproachNanometers,actor.StartApproachExitTime,actor.TotalPenaltyLoops,actor.TuckActive,
                actor.FinalSprintActive,actor.ActiveBout,actor.HasFinished,actor.AssignedShootingLane,path_lane=actor.RangePath?.LaneOrdinal,path_id=actor.RangePath?.LaneId,
                actor.CurrentRangeMotion,lifecycle=new {life.Tick,life.StartTick,life.Phase,life.CompletedLaps,life.CompletedBouts,life.PendingPenaltyLoops,
                    life.GlobalFinishTime,life.ElapsedFinishTime,life.LastFrame},
                physical=new {point.LoopId,point.LocalNanometers,point.LoopLengthNanometers,point.Closed,point.IsValid},lateral=field.GetLateral(i),
                draft=field.GetDraft(i),junction_wait_since=field.GetJunctionWaitSince(i),on_course=field.IsOnCourse(i),assigned_lane=field.GetAssignedRangeLane(i),
                waiting_for_capacity=field.IsWaitingForRangeCapacity(i),owned_bout=reservations?.GetOwnedBout(i),last_assigned_bout=reservations?.GetLastAssignedBout(i),
                shooting=shooting==null?null:(object)new {shooting.InitialTick,shooting.Tick,shooting.Phase,shooting.ShotsFired,shooting.ClosedMask,
                    shooting.PhaseDurationTicks,shooting.PhaseRemainingTicks,shooting.PhaseElapsedTicks,shooting.LastFrame,shooting.Sight},
                masks,range_history=histories,checkpoint_tick=actor.Checkpoints.Tick,checkpoint_route_nanometers=actor.Checkpoints.RouteNanometers,splits};
        }
        var owners=new int[reservations?.LaneCount??0];for(int l=0;l<owners.Length;l++)owners[l]=reservations.GetOwner(l+1);
        var changes=new object[reservations?.LastChangeCount??0];for(int c=0;c<changes.Length;c++)changes[c]=reservations.GetLastChange(c);
        return JsonSerializer.Serialize(new {field.Tick,field.Count,field.IsComplete,field.PrecisionFallbackTicks,reservation_tick=reservations?.Tick,
            arrivals,reservation_owners=owners,reservation_changes=changes,actors},Json);
    }
    private sealed class EventBatch:IRaceEventSink
    {
        public static readonly string[] ExcludedNames={"AiIntentChanged","AiShootingPolicyChanged","AiPursuitTacticsChanged","OvertakeStarted","OvertakeCompleted","OvertakeAborted"};
        private readonly List<RaceEvent> batch=new List<RaceEvent>(512);private bool overflow;
        public long FieldCount {get;private set;}public long ExcludedCount {get;private set;}
        public void Record(RaceEvent value)
        {
            var type=value.EventType;
            if(type==RaceEventType.AiIntentChanged||type==RaceEventType.AiShootingPolicyChanged||type==RaceEventType.AiPursuitTacticsChanged||
                type==RaceEventType.OvertakeStarted||type==RaceEventType.OvertakeCompleted||type==RaceEventType.OvertakeAborted){ExcludedCount++;return;}
            FieldCount++;if(batch.Count<16384)batch.Add(value);else overflow=true;
        }
        public string Take()
        {if(overflow)throw new InvalidOperationException("Tick event batch exceeds 16384; sink stayed nonthrowing during commit.");string value=JsonSerializer.Serialize(batch,Json);batch.Clear();return value;}
    }
    private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string ReadText(BinaryReader reader,int maximum=MaximumRecordBytes)
    {
        int length=reader.Read7BitEncodedInt();if(length<0||length>maximum)throw new InvalidOperationException("Text exceeds bounded record capacity.");
        byte[] value=reader.ReadBytes(length);if(value.Length!=length)throw new EndOfStreamException("Truncated text.");
        return new UTF8Encoding(false,true).GetString(value);
    }
    private static string HashFile(string path)
    {using(var file=File.OpenRead(path))return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();}
    // A cold context can be large because the factory records the full geometry for
    // every actor. Stream serialization and reject expansion beyond a fixed bound.
    private sealed class CapacityStream:Stream
    {
        private readonly Stream inner;private readonly long maximum;private readonly bool writing;
        public long Transferred {get;private set;}
        public CapacityStream(Stream inner,long maximum,bool writing){this.inner=inner;this.maximum=maximum;this.writing=writing;}
        public override bool CanRead=>!writing;public override bool CanWrite=>writing;public override bool CanSeek=>false;
        public override long Length=>throw new NotSupportedException();public override long Position {get=>Transferred;set=>throw new NotSupportedException();}
        private void Count(int length){if(length>maximum-Transferred)throw new InvalidOperationException("Uncompressed context exceeds 128 MiB capacity.");Transferred+=length;}
        public override int Read(byte[] buffer,int offset,int count)=>Read(buffer.AsSpan(offset,count));
        public override int Read(Span<byte> buffer)
        {int length=inner.Read(buffer.Slice(0,(int)Math.Min(buffer.Length,maximum-Transferred+1)));Count(length);return length;}
        public override void Write(byte[] buffer,int offset,int count)=>Write(buffer.AsSpan(offset,count));
        public override void Write(ReadOnlySpan<byte> buffer){Count(buffer.Length);inner.Write(buffer);}
        public override void Flush()=>inner.Flush();public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
    }
    private static void Save(string path,object value)
    {Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path,JsonSerializer.Serialize(value,Pretty)+Environment.NewLine);}
}

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;

// A complete, real production-career soak. No fixture result or week jump is accepted.
string root=Path.GetFullPath(args.Length>0?args[0]:"../..");
string output=Path.GetFullPath(args.Length>1?args[1]:Path.Combine(root,"artifacts/m16/three-season-soak"));
if(args.Length>3&&args[2]=="--diagnose")
{
    SoakDiagnosis.Run(root,output,Path.GetFullPath(args[3]));return;
}
bool resume=args.Length>2&&(args[2]=="--resume"||args[2]=="--resume-persistence");
bool resumePersistence=args.Length>2&&args[2]=="--resume-persistence";
string? retainedAuthority=args.Length>3&&args[2]=="--requalify"?Path.GetFullPath(args[3]):null;
string? visualReference=args.Length>3&&args[2]=="--visual-regression"?Path.GetFullPath(args[3]):null;
var visualWeeks=visualReference==null?null:JsonSerializer.Deserialize<List<WeekRecord>>(File.ReadAllText(Path.Combine(visualReference,"weeks.json")))!;
Directory.CreateDirectory(output);
var options=new JsonSerializerOptions{WriteIndented=true};
var definitions=new Dictionary<string,string>(StringComparer.Ordinal);
string Read(string name)
{
    if(!definitions.TryGetValue(name,out var value))definitions[name]=value=File.ReadAllText(Path.Combine(root,"Assets/Resources/Biathlon",name));
    return value;
}
string Venue(string name)=>File.ReadAllText(Path.Combine(root,"Assets/Resources",name+".json"));
void Require(bool ok,string reason){if(!ok)throw new InvalidOperationException(reason);}
string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
void Save(string name,object value)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,options));
var paths=new[]{"Assets/Scripts/Domain","Assets/Scripts/Simulation","Assets/Scripts/AI","Assets/Scripts/Career","Assets/Scripts/Presentation","Assets/Resources/Biathlon"}
    .SelectMany(p=>Directory.EnumerateFiles(Path.Combine(root,p),"*",SearchOption.AllDirectories))
    .Where(p=>p.EndsWith(".cs",StringComparison.Ordinal)||p.EndsWith(".json",StringComparison.Ordinal)).OrderBy(p=>p,StringComparer.Ordinal).ToArray();
var inputManifest=paths.Select(p=>new{path=Path.GetRelativePath(root,p).Replace('\\','/'),sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))).ToLowerInvariant()}).ToArray();
string inputSha=Hash(JsonSerializer.Serialize(inputManifest));
var assemblies=new[]{typeof(FrontEndSession).Assembly,typeof(CareerSave).Assembly,typeof(PackAiRaceRunner).Assembly,typeof(ManualScopedFieldStepper).Assembly,typeof(TimeTrialAthlete).Assembly}.Distinct().ToDictionary(a=>a.GetName().Name!,a=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))).ToLowerInvariant());
List<WeekRecord>? retainedWeeks=null;
if(retainedAuthority!=null)
{
    retainedWeeks=JsonSerializer.Deserialize<List<WeekRecord>>(File.ReadAllText(Path.Combine(retainedAuthority,"weeks.json")))!;
    Require(retainedWeeks.Count==156,"Requalification requires all retained 156 completed weekly records.");
    using var priorInputs=JsonDocument.Parse(File.ReadAllText(Path.Combine(retainedAuthority,"input-manifest.json")));
    var prior=priorInputs.RootElement.GetProperty("files").EnumerateArray().ToDictionary(x=>x.GetProperty("path").GetString()!,x=>x.GetProperty("sha256").GetString()!,StringComparer.Ordinal);
    foreach(var input in inputManifest)
    {
        bool resource=input.path.StartsWith("Assets/Resources/Biathlon/",StringComparison.Ordinal)&&!input.path.StartsWith("Assets/Resources/Biathlon/UI/",StringComparison.Ordinal)&&input.path!="Assets/Resources/Biathlon/input_bindings_v1.json";
        bool authority=input.path.StartsWith("Assets/Scripts/Domain/",StringComparison.Ordinal)||input.path.StartsWith("Assets/Scripts/Simulation/",StringComparison.Ordinal)||input.path.StartsWith("Assets/Scripts/AI/",StringComparison.Ordinal)||input.path.StartsWith("Assets/Scripts/Career/",StringComparison.Ordinal)||resource;
        if(authority)Require(prior.TryGetValue(input.path,out var sha)&&sha==input.sha256,"Authority source/resource changed; retained result requalification is forbidden: "+input.path);
    }
}
string manifestFile=Path.Combine(output,"input-manifest.json");
if(resume)
{
    Require(File.Exists(manifestFile),"Resume requires a retained input manifest.");
    using var old=JsonDocument.Parse(File.ReadAllText(manifestFile));
    if(old.RootElement.GetProperty("sha256").GetString()!=inputSha)
    {
        Require(resumePersistence,"Production source changed; preserve this run and use a new output directory.");
        var previous=old.RootElement.GetProperty("files").EnumerateArray().ToDictionary(x=>x.GetProperty("path").GetString()!,x=>x.GetProperty("sha256").GetString()!,StringComparer.Ordinal);
        var changed=inputManifest.Where(x=>!previous.TryGetValue(x.path,out var sha)||sha!=x.sha256).Select(x=>x.path).ToArray();
        Require(previous.Count==inputManifest.Length&&changed.All(x=>x=="Assets/Scripts/Presentation/FileCareerProfileStore.cs"||x=="Assets/Resources/Biathlon/input_bindings_v1.json"||x.StartsWith("Assets/Resources/Biathlon/UI/",StringComparison.Ordinal)),"Persistence-only continuation forbids any authority/resource/other-source change.");
        Save("continuation-manifest.json",new{schema_version=1,id="m16_persistence_only_continuation_v1",prior_input_sha256=old.RootElement.GetProperty("sha256").GetString(),input_sha256=inputSha,changed,scope="Resume exact confirmed weekly checkpoint after idempotent durable pre-race checkpoint fix; all authoritative source/resources unchanged",files=inputManifest,assembly_sha256=assemblies});
    }
}
else
{
    Require(!Directory.Exists(Path.Combine(output,"profiles")),"Use a fresh output directory or explicit --resume; prior evidence is never overwritten.");
    Save("input-manifest.json",new{schema_version=1,id="m16_soak_inputs_v1",sha256=inputSha,files=inputManifest,assembly_sha256=assemblies});
}
var files=new FileCareerProfileStore(Path.Combine(output,"profiles"));
var store=new FaultStore(files);
FrontEndSession NewSession()=>new FrontEndSession(Read,store,s=>new SeededRandomSource(s));
var session=NewSession();
if(resume)session.ContinueCareer();
else
{
    var draft=new CareerCreationDraft(CareerCreationDefinition.FromJson(Read("career_creation_v1.json")),CareerStartDefinition.FromJson(Read("career_start_v1.json")),BiathlonRatingDefinition.FromJson(Read("career_ratings_v1.json")),CareerRosterDefinition.FromJson(Read("career_roster_v1.json")),1615215){Name="M16 Soak Athlete",Nation="NOR"};
    draft.SelectTier("junior_novice");draft.SelectPreset("balanced");
    session.OpenNewCareer();session.CreateCareer("career_m16_soak",draft);
}
var weeks=new List<WeekRecord>();
string weekLog=Path.Combine(output,"weeks.json");
if(resume&&File.Exists(weekLog))weeks=JsonSerializer.Deserialize<List<WeekRecord>>(File.ReadAllText(weekLog))!;
int expectedCompleted=(session.Career.Season-1)*52+session.Career.Week-1;
Require(weeks.Count==expectedCompleted,"Interrupted evidence sidecar does not match its durable save; retain it and rerun in a fresh directory.");
string playerId=session.Career.PlayerId;
string originalAppearance=session.Player.Appearance.ToJson();
string originalGrowth=session.Player.Growth.ToJson();
int originalAge=session.Player.Age-(session.Career.Season-1);
int rejectedWrites=weeks.Count(x=>x.rejected_write);
int abandoned=weeks.Count(x=>x.abandoned_retry);
int authorityRacesAtStart=weeks.Count(x=>x.event_id!=null);
var timer=Stopwatch.StartNew();
try
{
    while(session.Career.Season<=3&&(visualReference==null||session.Career.Season==1&&session.Career.Week<=14))
    {
        var save=session.Career;int season=save.Season,week=save.Week;string before=files.ReadLatest();
        Require(save.ToJson()==CareerSave.FromJson(before).ToJson(),"Session and durable profile disagree before command.");
        var definition=session.Definitions;var e=definition.Calendar.AtWeek(week);bool fault=(season==1&&week==2)||(e!=null&&week==6);
        CareerRaceRecord? race=null;long ticks=0;int shots=0;ulong eventHash=0;bool wasAbandoned=false;
        var player=session.Player;var plan=CareerAiTraining.Plan(player,definition.Training,definition.Calendar,definition.Policy,week,definition.Growth,definition.Adaptation);
        session.OpenCareerPage(FrontEndScreen.Training);
        for(int i=0;i<plan.Count;i++)session.AddTraining(plan.Get(i));
        string preview=session.TrainingPreview().Appearance.ToJson();
        Require(preview==originalAppearance,"Training preview changed player appearance.");session.Back();
        if(e==null)
        {
            if(fault)
            {
                store.FailNextReplace=true;
                try{session.CompleteTrainingWeek();throw new InvalidOperationException("Injected write unexpectedly succeeded.");}
                catch(InjectedWriteFailure){rejectedWrites++;}
                Require(session.Career.ToJson()==before&&files.ReadLatest()==before,"Failed training write published state.");
            }
            session.CompleteTrainingWeek();
        }
        else
        {
            Require(e.Eligible(session.Player),"This complete-season study needs an eligible player.");
            session.OpenPreRace();string snapshot=session.PreparedWeek.RaceSnapshotJson;ulong seed=session.PreparedWeek.RaceSeed;
            session.Back();session.OpenPreRace();
            Require(session.PreparedWeek.RaceSeed==seed&&session.PreparedWeek.RaceSnapshotJson==snapshot,"Preview/cancel changed prepared authority or RNG.");
            if(week==6)
            {
                session.BeginRace();session.AbandonRace();abandoned++;wasAbandoned=true;
                Require(files.ReadLatest()==before&&session.PreparedWeek.RaceSnapshotJson==snapshot,"Abandon changed durable career or prepared race.");
            }
            session.BeginRace();
            Require(CareerSave.FromJson(File.ReadAllText(Path.Combine(output,"profiles",save.Id+".json.checkpoint"))).ToJson()==CareerSave.FromJson(before).ToJson(),"BeginRace checkpoint did not preserve the confirmed pre-race career.");
            Console.WriteLine("RACE season="+season+" week="+week+" event="+e.Id+" format="+e.Format);
            if(retainedAuthority==null)
            {
                var provider=new CareerRaceProvider(Read,definition,Venue);
                race=provider.Run(CareerSave.FromJson(snapshot),e,seed);ticks=provider.LastTicks;shots=provider.LastShots;eventHash=provider.LastEventHash;
            }
            else
            {
                var retained=retainedWeeks!.Single(x=>x.season==season&&x.week==week);
                Require(retained.before_sha256==Hash(before)&&retained.calendar_id==definition.Calendar.Id&&retained.event_id==e.Id,"Retained authority belongs to different career state.");
                using(var recordStream=File.OpenRead(Path.Combine(retainedAuthority,"race-"+season+"-"+week+".json")))race=(CareerRaceRecord)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(CareerRaceRecord)).ReadObject(recordStream)!;
                _=race.ToJson();
                Require(race.Seed==seed&&race.SeasonId==save.SeasonId&&race.CalendarEventId==e.Id,"Retained authority seed/season/event do not match exact prepared state.");
                ticks=retained.authority_ticks;shots=retained.shots;eventHash=retained.event_hash;
            }
            Require(race.AuthorityResultJson!=null&&BalanceRegression.Hash(race.AuthorityResultJson)==race.AuthoritySha256,"Invalid authoritative result payload/hash.");
            if(visualWeeks!=null)
            {
                var expected=visualWeeks.Single(x=>x.season==season&&x.week==week);
                Require(expected.authority_sha256==race.AuthoritySha256&&expected.authority_ticks==ticks&&expected.shots==shots&&expected.event_hash==eventHash,"Presentation update changed the full production race: "+e.Id);
                string prior=File.ReadAllText(Path.Combine(visualReference!,"race-"+season+"-"+week+".json"));
                Require(prior==race.ToJson(),"Presentation update changed full result/bout/split payload: "+e.Id);
            }
            Require(race.Count==30&&shots==30*(e.Format==CompetitionFormat.Sprint?2:4)*5&&ticks>0,"Incomplete authority field/shot accounting.");
            File.WriteAllText(Path.Combine(output,"race-"+season+"-"+week+".json"),race.ToJson());
            if(fault)
            {
                store.FailNextReplace=true;
                try{session.FinishRace(race);throw new InvalidOperationException("Injected result write unexpectedly succeeded.");}
                catch(InjectedWriteFailure){rejectedWrites++;}
                Require(session.Career.ToJson()==before&&files.ReadLatest()==before&&session.Screen==FrontEndScreen.Race&&session.Result==null,"Failed finish published state or lost pending race.");
            }
            session.FinishRace(race);
            Require(session.ResultsSummary!=null&&session.Result?.AuthoritySha256==race.AuthoritySha256,"Results page lost authoritative outcome.");
            Require(session.Career.RaceCount==save.RaceCount+1,"Result was not published exactly once.");
            try{session.FinishRace(race);throw new InvalidOperationException("Duplicate finish unexpectedly succeeded.");}catch(InvalidOperationException ex)when(ex.Message=="Invalid front-end transition."){}
            session.ReturnFromResults();
        }
        string after=files.ReadLatest();var next=session.Career;
        Require(next.Season==season+(week==52?1:0)&&next.Week==(week==52?1:week+1),"Weekly/annual progression skipped or repeated.");
        Require(next.PlayerId==playerId&&session.Player.Age==originalAge+next.Season-1,"Player identity/age changed incorrectly.");
        Require(session.Player.Appearance.ToJson()==originalAppearance&&session.Player.Growth.ToJson()==originalGrowth,"Immutable cosmetics/talent changed across career transition.");
        Require(next.WorldAthleteCount>=definition.World.MinimumPopulation&&next.WorldAthleteCount<=definition.World.MaximumPopulation,"World population escaped authored bounds.");
        Require(next.HistoryCount==next.Season-1,"Season history missing or duplicated.");
        Require(CareerSave.FromJson(File.ReadAllText(Path.Combine(output,"profiles",next.Id+".json.bak"))).ToJson()==CareerSave.FromJson(before).ToJson(),"Backup does not retain previous validated profile.");
        Require(Directory.GetFiles(Path.Combine(output,"profiles"),"*.tmp").Length==0,"Successful command leaked a temporary file.");
        int sourceBytes=Encoding.UTF8.GetByteCount(after);
        session=NewSession();session.ContinueCareer();
        Require(session.Career.ToJson()==CareerSave.FromJson(after).ToJson(),"Fresh session changed saved career during continue.");
        var row=new WeekRecord(season,week,definition.Calendar.Id,e?.Id,e?.Format.ToString(),race?.AuthoritySha256,ticks,shots,eventHash,Hash(before),Hash(after),sourceBytes,next.WorldAthleteCount,next.HistoryCount,fault,wasAbandoned);
        weeks.Add(row);Save("weeks.json",weeks);
        if(retainedWeeks!=null)Require(retainedWeeks[weeks.Count-1].after_sha256==row.after_sha256,"Final implementation changed career output relative to retained authentic race chain.");
        if(visualWeeks!=null)Require(visualWeeks[weeks.Count-1].after_sha256==row.after_sha256,"Presentation update changed a complete weekly save boundary.");
        Console.WriteLine("SAVED "+weeks.Count+"/156 season="+next.Season+" week="+next.Week+" bytes="+sourceBytes);
    }
    var completed=session.Career;
    if(visualReference!=null)
    {
        Require(weeks.Count==14&&weeks.Where(x=>x.event_id!=null).Select(x=>x.format).Distinct().Count()==3,"All three complete production formats must be re-executed.");
        Save("summary.json",new{schema_version=1,id="m16_1_visual_authority_regression_v1",status="PASS",input_sha256=inputSha,scope="14 new weekly transitions and five fully re-executed 30-athlete races; exact baseline result payload, event hash, tick count, shots and complete saved-career boundary comparison",reference=Path.GetRelativePath(root,visualReference).Replace('\\','/'),weeks=weeks.Count,races=weeks.Count(x=>x.event_id!=null),formats=weeks.Where(x=>x.event_id!=null).GroupBy(x=>x.format).ToDictionary(g=>g.Key!,g=>g.Count()),ticks=weeks.Sum(x=>x.authority_ticks),shots=weeks.Sum(x=>x.shots),final_save_sha256=Hash(completed.ToJson()),elapsed_seconds=timer.Elapsed.TotalSeconds});
        return;
    }
    Require(weeks.Count==156&&completed.Season==4&&completed.Week==1&&completed.HistoryCount==3,"Did not complete all three actual seasons.");
    Require(weeks.Count(x=>x.event_id!=null)==48&&session.Player.Races==48,"Expected all 48 authored real race results.");
    Require(completed.History(0).RaceCount==16&&completed.History(1).RaceCount==16&&completed.History(2).RaceCount==16,"Archived event totals differ.");
    string target=Path.Combine(output,"profiles",completed.Id+".json"),backup=File.ReadAllText(target+".bak");
    string completedJson=File.ReadAllText(target);File.WriteAllText(target,"{interrupted-test-primary");
    var recovered=NewSession();recovered.ContinueCareer();
    Require(files.LastRead.WasRecovered&&files.LastRead.Source==CareerProfileSource.Backup,"Corrupt primary did not disclose same-profile backup recovery.");
    Require(recovered.Career.ToJson()==CareerSave.FromJson(backup).ToJson(),"Recovery returned a different career checkpoint.");
    Require(File.ReadAllText(target)=="{interrupted-test-primary"&&File.ReadAllText(target+".bak")==backup,"Continue silently mutated damaged evidence.");
    var recoveredPlan=CareerAiTraining.Plan(recovered.Player,recovered.Definitions.Training,recovered.Definitions.Calendar,recovered.Definitions.Policy,recovered.Career.Week,recovered.Definitions.Growth,recovered.Definitions.Adaptation);
    recovered.OpenCareerPage(FrontEndScreen.Training);for(int i=0;i<recoveredPlan.Count;i++)recovered.AddTraining(recoveredPlan.Get(i));recovered.Back();
    recovered.CompleteTrainingWeek();Require(recovered.Career.ToJson()==completed.ToJson(),"Recovered final season transition did not reproduce the exact completed save.");
    Require(Directory.GetFiles(Path.Combine(output,"profiles"),"*.corrupt-*").Length==1,"Repair did not preserve damaged primary evidence.");
    Require(File.ReadAllText(target)==completedJson&&File.ReadAllText(target+".bak")==backup,"Repair lost valid historical save or produced different final save.");
    Save("summary.json",new{schema_version=1,id="m16_three_season_soak_v1",status="PASS",input_sha256=inputSha,scope="156 actual weekly FrontEndSession prepare/train/race/result/save/reload transitions. All 48 records come from production Woodland authority with shared player/AI physics and geometric shooting. Automated AI controls all entrants; no human steering or 48 native-rendered races is claimed.",authority_mode=retainedAuthority==null?"48 races executed across this checkpointed study; this invocation may continue a retained exact weekly checkpoint":"Requalified retained 48 actually executed races; unchanged authority source/resources; exact before/after save hashes, event/seed checks throughout all 156 boundaries",seasons=3,weeks=weeks.Count,authoritative_races=weeks.Count(x=>x.event_id!=null),newly_executed_authority_races=retainedAuthority==null?48-authorityRacesAtStart:0,formats=weeks.Where(x=>x.event_id!=null).GroupBy(x=>x.format).ToDictionary(g=>g.Key!,g=>g.Count()),authority_ticks=weeks.Sum(x=>x.authority_ticks),geometric_shots=weeks.Sum(x=>x.shots),durable_reloads=weeks.Count,injected_failed_writes=rejectedWrites,abandon_retry=abandoned,corrupt_primary_recovery="PASS; same-profile backup read disclosed; read-only Continue; repair preserved corrupt evidence and reproduced exact final transition",final_save_sha256=Hash(completed.ToJson()),max_save_bytes=weeks.Max(x=>x.save_bytes),final_world=completed.WorldAthleteCount,final_history=completed.HistoryCount,authority_assembly_sha256=assemblies,elapsed_seconds_this_invocation=timer.Elapsed.TotalSeconds});
}
catch(Exception ex)
{
    var failure=new{status="FAIL",input_sha256=inputSha,completed_weeks=weeks.Count,season=session.Career.Season,week=session.Career.Week,error=ex.ToString()};
    Save("failure-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+".json",failure);Save("failure.json",failure);throw;
}

sealed class InjectedWriteFailure:IOException{public InjectedWriteFailure():base("Injected boundary write failure before durable store mutation."){} }
sealed class FaultStore:ICareerProfileStore,ICareerProfileCheckpointStore
{
    readonly FileCareerProfileStore inner;public bool FailNextReplace;
    public FaultStore(FileCareerProfileStore inner){this.inner=inner;}
    public bool HasProfile=>inner.HasProfile;public string ReadLatest()=>inner.ReadLatest();
    public void WriteNew(string id,string json)=>inner.WriteNew(id,json);
    public void SaveBeforeRaceCheckpoint(string id,string expectedJson)=>inner.SaveBeforeRaceCheckpoint(id,expectedJson);
    public void Replace(string id,string expected,string json){if(FailNextReplace){FailNextReplace=false;throw new InjectedWriteFailure();}inner.Replace(id,expected,json);}
}
sealed record WeekRecord(int season,int week,string calendar_id,string? event_id,string? format,string? authority_sha256,long authority_ticks,int shots,ulong event_hash,string before_sha256,string after_sha256,int save_bytes,int world_count,int history_count,bool rejected_write,bool abandoned_retry);

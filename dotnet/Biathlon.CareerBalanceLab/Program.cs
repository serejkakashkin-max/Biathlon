using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

// Real production race authority. This is intentionally separate from the development lab.
string root=Path.GetFullPath(args.Length>0?args[0]:"../..");
string selection=args.Length>1?args[1]:"all";
string output=Path.Combine(root,"artifacts/m15-6/authoritative-race-study");Directory.CreateDirectory(output);
var options=new JsonSerializerOptions{WriteIndented=true};
var definitionsCache=new Dictionary<string,string>(StringComparer.Ordinal);
string Read(string n){if(!definitionsCache.TryGetValue(n,out var text))definitionsCache[n]=text=File.ReadAllText(Path.Combine(root,"Assets/Resources/Biathlon",n));return text;}
string Venue(string n)=>File.ReadAllText(Path.Combine(root,"Assets/Resources",n+".json"));
void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
var baseDefinitions=new CareerDefinitions(Read);
var creation=CareerCreationDefinition.FromJson(Read("career_creation_v1.json"));
var starts=CareerStartDefinition.FromJson(Read("career_start_v1.json"));
var cases=new List<StudyCase>();
foreach(string profile in new[]{"junior_novice","senior_debut","world_cup_debut"})
foreach(string preset in new[]{"balanced","skier","shooter"})
{
    var d=CareerDefinitions.ForNewProductionCareer(Read,profile);
    var draft=new CareerCreationDraft(creation,starts,d.Ratings,d.Roster,71000){Name="Study athlete",Nation="NOR"};draft.SelectTier(profile);draft.SelectPreset(preset);
    var save=draft.Confirm("race_study_"+profile,d.Calendar,s=>new SeededRandomSource(s));
    save=CareerWorld.Initialize(save,d,s=>new SeededRandomSource(s));CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s),draft.Tier.BaselineStat);
    cases.Add(new StudyCase(profile+"_"+preset,save,false,false));
}
var catalog=new CareerAthleteCatalog(Read,"race_study_catalog",71000,s=>new SeededRandomSource(s));
var international=catalog.Search(new CareerAthleteFilter{Circuit=CareerCircuit.WorldCup});
cases.Add(new StudyCase("catalog_world_cup_strong",catalog.Confirm(international[0].Id),false,false));
cases.Add(new StudyCase("catalog_world_cup_weak",catalog.Confirm(international[^1].Id),false,false));
cases.Add(cases[0] with {Id="junior_controlled_full_effort",Controlled=true});
foreach(ulong seed in new ulong[]{124,125})foreach(string preset in new[]{"balanced","skier","shooter"})
    cases.Add(cases.Single(c=>c.Id=="world_cup_debut_"+preset) with {Id="world_cup_debut_"+preset+"_seed_"+seed,RaceSeed=seed});
foreach(string profile in new[]{"junior_novice","senior_debut","world_cup_debut"})cases.Add(cases.Single(c=>c.Id==profile+"_balanced") with {Id=profile+"_fresh_mass_start",OpeningOnly=true});
cases.Add(cases.Single(c=>c.Id=="junior_novice_balanced") with {Id="junior_novice_balanced_repeat"});
if(selection!="all")cases=cases.Where(c=>c.Id==selection||selection=="smoke"&&c.Id=="junior_novice_balanced"||selection=="sprints"&&!c.OpeningOnly||selection=="counterfactuals"&&c.RaceSeed>123||selection=="fairness"&&c.Id.StartsWith("world_cup_debut_",StringComparison.Ordinal)&&!c.OpeningOnly||selection=="openings"&&c.OpeningOnly).ToList();
Require(cases.Count>0,"Unknown case selection.");
var results=new List<StudyResult>();var failures=new List<object>();long totalTicks=0;int fullRaces=0;var stopwatch=Stopwatch.StartNew();
foreach(var study in cases)
{
    var timer=Stopwatch.StartNew();Console.WriteLine("START "+study.Id);
    try
    {
        var save=study.Save;string before=save.ToJson();var d=CareerDefinitions.ForSave(Read,save);
        var e=Enumerable.Range(0,d.Calendar.Count).Select(d.Calendar.Get).First(x=>x.Format==(study.OpeningOnly?CompetitionFormat.MassStart:CompetitionFormat.Sprint));
        var observer=new EventAudit();var p=new CareerRaceProvider(Read,d,Venue).Prepare(save,e,study.RaceSeed,observer);
        var r=p.Runner;var field=r.Field;int n=field.Count;Require(n==30,"Study requires an actual production30 field.");
        var movement=ManualProductionFieldComposition.Create(field,p.Venue,p.ManualRead);
        r.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(Read("player_course_assistance_v1.json")));
        if(study.Id=="junior_novice_balanced")
        {
            var metadata=JsonNode.Parse(before)!;var firstActor=metadata["athletes"]!.AsArray().Single(x=>x!["id"]!.GetValue<string>()==save.PlayerId)!;
            firstActor["appearance_profile"]=JsonNode.Parse(AppearanceProfile.Create("angular","dark_deep","athletic").ToJson());firstActor["potential"]=100000;
            var changed=CareerSave.FromJson(metadata.ToJsonString());var counterfactual=new CareerRaceProvider(Read,d,Venue).Prepare(changed,e,123);
            // Compare before adding the identical manual binding. Cosmetic/legacy-potential metadata is not race capability.
            var baseline=new CareerRaceProvider(Read,d,Venue).Prepare(save,e,123);
            Require(counterfactual.Runner.ContextJson==baseline.Runner.ContextJson,"Appearance or legacy potential altered authority race composition.");
            File.WriteAllText(Path.Combine(output,"metadata-invariance.json"),JsonSerializer.Serialize(new{status="PASS",case_id=study.Id,changed="actual saved appearance and legacy potential; all13 current stats unchanged",context_sha256=BalanceRegression.Hash(baseline.Runner.ContextJson)},options));
            firstActor["fatigue"]=6000;firstActor["form"]=2000;firstActor["confidence"]=2500;firstActor["morale"]=3000;
            var tired=CareerSave.FromJson(metadata.ToJsonString());var tiredPlayer=tired.FindWorldAthlete(tired.PlayerId);
            var ai=new CareerRaceProvider(Read,d,Venue).Prepare(tired,e,123);var manual=new CareerRaceProvider(Read,d,Venue).Prepare(tired,e,123);
            foreach(var prepared in new[]{ai,manual})prepared.Runner.BindManualScopedCandidate(ManualProductionFieldComposition.Create(prepared.Runner.Field,prepared.Venue,prepared.ManualRead),ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
            ai.Runner.Step();var aiInput=ai.Runner.GetLastInput(ai.PlayerIndex);var controlled=manual.Runner.Field.GetAthlete(manual.PlayerIndex);var next=manual.Runner.Field.Tick.Next();
            manual.Runner.StepWithManualPlayer(manual.PlayerIndex,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,controlled.AthleteId,aiInput.Ski.Effort.RequestedEffort),1,1,0),new WeaponAimCommand(next,controlled.AthleteId,0,0)),0,finalMatAssistance:true);
            var manualInput=manual.Runner.GetLastInput(manual.PlayerIndex);
            Require(aiInput.Ski.Form<1&&aiInput.Ski.Form==manualInput.Ski.Form,"Non-neutral readiness differs under player and AI control.");
            Require(aiInput.Pressure.RaceImportance==manualInput.Pressure.RaceImportance&&manualInput.Pressure.RaceImportance==CareerMental.Pressure(tiredPlayer,d.Mental)&&aiInput.Pressure.Confidence==manualInput.Pressure.Confidence&&manualInput.Pressure.Confidence==.25,"Player control bypassed shared psychological projection.");
            File.WriteAllText(Path.Combine(output,"nonneutral-player-ai-readiness.json"),JsonSerializer.Serialize(new{status="PASS",fatigue=tiredPlayer.Fatigue,form=tiredPlayer.Form,confidence=tiredPlayer.Confidence,ai_effective_form=aiInput.Ski.Form,player_effective_form=manualInput.Ski.Form,ai_pressure=aiInput.Pressure.RaceImportance,player_pressure=manualInput.Pressure.RaceImportance,ai_confidence=aiInput.Pressure.Confidence,player_confidence=manualInput.Pressure.Confidence},options));
        }
        int projectionChecks=0;
        var skiField=typeof(SprintAthleteSession).GetField("skiAthlete",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var shootingField=typeof(SprintAthleteSession).GetField("shootingAthlete",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var skiNames=new[]{"Endurance","AnaerobicCapacity","Recovery","Strength","SkiTechnique","Climbing","Downhill","Cornering"};
        var shootingNames=new[]{"BreathControl","ProneStability","StandingStability","ShootingSpeed","Composure"};
        for(int i=0;i<n;i++)
        {
            var actor=field.GetAthlete(i);var source=Enumerable.Range(0,save.AthleteCount).Select(save.Athlete).Single(a=>a.Id==actor.AthleteId.Value);
            var ski=(TimeTrialAthlete)skiField.GetValue(actor)!;var shooting=(ShootingAthleteDefinition)shootingField.GetValue(actor)!;
            for(int stat=0;stat<13;stat++)
            {
                int value=(int)(stat<8?typeof(TimeTrialAthlete).GetProperty(skiNames[stat])!.GetValue(ski)!:typeof(ShootingAthleteDefinition).GetProperty(shootingNames[stat-8])!.GetValue(shooting)!);
                int expected=(int)Math.Round(source.Stat((CareerStat)stat)/(decimal)CareerUnits.StatScale,0,MidpointRounding.ToEven);
                Require(value==expected,"Actual race stat projection differs: "+source.Id+" "+stat);projectionChecks++;
            }
        }
        var still=new long[n];var worstStill=new long[n];var skiTicks=new long[n];var effortSum=new long[n];var rangeTicks=new long[n];var crestTimes=new long[n];var hillMin=new double[n];var hillTicks=new long[n];var hillSlow=new long[n];var hillEffort=new long[n];var hillTuck=new long[n];var uphillTuck=new long[n];var priorClimbing=new bool[n];var mainSampler=new TrackSampler(field.Track.GetLoop("main"));var splits=new double?[n];var priorSessions=new ShootingSession?[n];var entryHr=Enumerable.Range(0,n).Select(_=>new List<double>()).ToArray();var minimumReserve=Enumerable.Repeat(1.0,n).ToArray();Array.Fill(hillMin,double.MaxValue);
        var model=new ShootingModelDefinitions(ProductionGameplayDefinitions.Bind(Read));ShootingAiController? shooter=null;ShootingSession? priorShooting=null;int manualBolts=0;
        var raceDefinition=CareerRaceDefinition.FromJson(Read("career_race_v1.json"));long tickBudget=raceDefinition.MaximumTicks;
        var savedPlayer=save.FindWorldAthlete(save.PlayerId);double expectedPlayerForm=CareerForm.RaceMultiplier(savedPlayer,d.Form)*(1-savedPlayer.Fatigue/(double)CareerUnits.Basis*raceDefinition.MaximumFatiguePenalty/CareerUnits.Basis);
        while(!field.IsComplete)
        {
            for(int i=0;i<n;i++)
            {
                var at=field.GetPhysicalPoint(i);priorClimbing[i]=at.LoopId=="main"&&at.LocalNanometers>=80e9&&at.LocalNanometers<180e9&&field.GetAthlete(i).Lifecycle.CompletedBouts==0&&mainSampler.Sample(new TrackDistance(at.LocalNanometers/SkiState.NanometersPerMillimeter)).Grade>.005;
            }
            if(study.Controlled)
            {
                var a=field.GetAthlete(p.PlayerIndex);
                if(a.HasFinished)r.StepRemainingField(p.PlayerIndex);
                else
                {
                    var tick=field.Tick.Next();var shooting=a.ActiveShooting;var aim=new WeaponAimCommand(tick,a.AthleteId,0,0);bool fire=false,hold=false,bolt=false;
                    if(shooting!=null)
                    {
                        if(!ReferenceEquals(priorShooting,shooting))
                        {
                            priorShooting=shooting;
                            var precise=ShootingAiDefinition.FromJson(Read("shooting_ai_v1.json").Replace("\"reaction_seconds\":0.2","\"reaction_seconds\":0.05").Replace("\"tracking_error_rad\":0.00015","\"tracking_error_rad\":0").Replace("\"aim_gain_per_s\":2.5","\"aim_gain_per_s\":10"));
                            shooter=new ShootingAiController(precise,model.Targets,model.Aim,model.Stances.Get(field.Profile.GetBout(a.ActiveBout).Stance),a.AthleteId,field.TickFrequency,new SeededRandomSource(444+(ulong)a.ActiveBout),field.Tick);
                        }
                        var sight=shooting.Sight;var frame=shooter!.Step(tick,new ShootingAiObservation(shooting.Phase,shooting.ClosedMask,shooting.ShotsFired,sight.YawRadians,sight.PitchRadians));
                        aim=frame.Aim;fire=frame.Trigger;hold=frame.HoldBreath;bolt=shooting.NeedsBoltCycle;if(bolt)manualBolts++;
                    }
                    var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,1000),1,1,0),aim,fire&&!bolt,hold,cycleBolt:bolt,manualBoltCycle:true);
                    // Explicit route automation isolates sports capability from human steering mistakes.
                    r.StepWithManualPlayer(p.PlayerIndex,input,0,finalMatAssistance:true);
                    var actual=r.GetLastInput(p.PlayerIndex);
                    Require(actual.Ski.Form==expectedPlayerForm,"Manual control bypassed shared career readiness.");
                }
            }
            else r.Step();
            Require(field.Tick.Value<tickBudget,"Tick budget exceeded.");Require(movement.World.Tick.Value==field.Tick.Value,"World/field clock diverged.");
            for(int i=0;i<n;i++)
            {
                var a=field.GetAthlete(i);if(a.HasFinished)continue;
                minimumReserve[i]=Math.Min(minimumReserve[i],a.PhysiologyState.AnaerobicReserve);
                if(priorClimbing[i]&&r.GetLastInput(i).Ski.TuckRequested)uphillTuck[i]++;
                if(a.ActiveShooting!=null&&!ReferenceEquals(a.ActiveShooting,priorSessions[i])){priorSessions[i]=a.ActiveShooting;entryHr[i].Add(a.PhysiologyState.HeartRateBpm);}
                if(a.Checkpoints.Count>0&&splits[i]==null)splits[i]=a.Checkpoints.Get(0).ElapsedTime.Seconds;
                if(a.Lifecycle.Phase==CompetitionPhase.Range){rangeTicks[i]++;still[i]=0;continue;}
                if(field.Tick.Value<a.ScheduledStart.Value){still[i]=0;continue;}
                skiTicks[i]++;effortSum[i]+=r.GetLastInput(i).Ski.Effort.RequestedEffort;
                still[i]=a.Motion.SpeedMetersPerSecond<.3?still[i]+1:0;worstStill[i]=Math.Max(worstStill[i],still[i]);
                var point=field.GetPhysicalPoint(i);if(point.LoopId!="main")continue;
                if(point.LocalNanometers>=80e9&&point.LocalNanometers<180e9&&a.Lifecycle.CompletedBouts==0)
                {hillTicks[i]++;hillMin[i]=Math.Min(hillMin[i],a.Motion.SpeedMetersPerSecond);hillEffort[i]+=r.GetLastInput(i).Ski.Effort.RequestedEffort;if(a.Motion.SpeedMetersPerSecond<1)hillSlow[i]++;if(r.GetLastInput(i).Ski.TuckRequested)hillTuck[i]++;}
                else if(point.LocalNanometers>=180e9&&crestTimes[i]==0)crestTimes[i]=field.Tick.Value;
            }
            if(study.OpeningOnly&&splits.All(x=>x.HasValue))break;
            if(field.Tick.Value%2000==0)Console.WriteLine(study.Id+" tick="+field.Tick.Value+" finished="+Enumerable.Range(0,n).Count(i=>field.GetAthlete(i).HasFinished));
        }
        Require(save.ToJson()==before,"Race preparation mutated source career.");
        var player=save.FindWorldAthlete(save.PlayerId);var rows=new List<FinishRow>();string? authorityHash=null;
        if(!study.OpeningOnly)
        {
            var record=p.CollectResult();authorityHash=record.AuthoritySha256;fullRaces++;
            Require(observer.Shots==n*field.Profile.BoutCount*field.Profile.ShotsPerBout,"Incomplete authoritative geometric shot accounting.");
            for(int rank=0;rank<record.Count;rank++)
            {
                var row=record.Get(rank);int index=Enumerable.Range(0,n).Single(i=>field.GetAthlete(i).AthleteId.Value==row.AthleteId);var source=save.FindWorldAthlete(row.AthleteId);
                var actor=field.GetAthlete(index);
                rows.Add(new FinishRow(rank+1,row.AthleteId,BiathlonRatingEngine.Calculate(source,d.Ratings).Overall,row.ElapsedMilliseconds,row.Penalties,rangeTicks[index]/(double)field.TickFrequency,effortSum[index]/(double)Math.Max(1,skiTicks[index]),worstStill[index]/(double)field.TickFrequency,entryHr[index].ToArray(),minimumReserve[index],actor.PhysiologyState.MuscularFatigue));
            }
        }
        var orderedSplits=splits.Where(x=>x.HasValue).Select(x=>x!.Value).Order().ToArray();var playerFinish=rows.SingleOrDefault(x=>x.AthleteId==save.PlayerId);double? lead=playerFinish==null?null:(rows.Where(x=>x.AthleteId!=save.PlayerId).Min(x=>x.ElapsedMilliseconds)-playerFinish.ElapsedMilliseconds)/1000.0;
        int? budgetSpent=save.Creation?.Mode==CareerCreationMode.CreateOwn?Enumerable.Range(0,13).Sum(i=>creation.Cost((CareerStat)i,(player.Stat((CareerStat)i)-creation.Tier(save.StartProfileId).BaselineStat)/creation.StatStep)):null;
        var result=new StudyResult(study.Id,save.PlayerId,player.Age,BiathlonRatingEngine.Calculate(player,d.Ratings).Overall,Enumerable.Range(0,13).Select(i=>player.Stat((CareerStat)i)).ToArray(),budgetSpent,study.Controlled,study.OpeningOnly,study.RaceSeed,field.Tick.Value,authorityHash,observer.Hash.ToString("x16"),observer.Shots,observer.Hits,projectionChecks,manualBolts,playerFinish?.Rank,lead,orderedSplits.Length==0?null:orderedSplits[(orderedSplits.Length-1)*9/10]-orderedSplits[0],worstStill.Max()/(double)field.TickFrequency,timer.Elapsed.TotalSeconds,rows,Enumerable.Range(0,n).Select(i=>new HillRow(field.GetAthlete(i).AthleteId.Value,crestTimes[i]==0?null:(crestTimes[i]-field.GetAthlete(i).ScheduledStart.Value)/(double)field.TickFrequency,hillTicks[i]==0?null:hillMin[i],hillSlow[i],hillTicks[i],hillTicks[i]==0?null:hillEffort[i]/(double)hillTicks[i],hillTuck[i],uphillTuck[i])).ToArray());
        results.Add(result);totalTicks+=field.Tick.Value;File.WriteAllText(Path.Combine(output,study.Id+".json"),JsonSerializer.Serialize(result,options));
        Console.WriteLine("PASS "+study.Id+" ticks="+result.Ticks+" rank="+result.PlayerRank+" lead_s="+result.PlayerLeadSeconds+" wall_s="+result.WallSeconds.ToString("F2"));
    }
    catch(Exception ex)
    {failures.Add(new{case_id=study.Id,error=ex.ToString(),wall_seconds=timer.Elapsed.TotalSeconds});File.WriteAllText(Path.Combine(output,study.Id+"-failure.json"),JsonSerializer.Serialize(failures[^1],options));Console.WriteLine("FAIL "+study.Id+" "+ex.Message);}
    File.WriteAllText(Path.Combine(output,"progress.json"),JsonSerializer.Serialize(new{completed=results.Count,failed=failures.Count,total=cases.Count,full_races=fullRaces,total_ticks=totalTicks,wall_seconds=stopwatch.Elapsed.TotalSeconds},options));
    GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
}
var first=results.SingleOrDefault(x=>x.Id=="junior_novice_balanced");var repeated=results.SingleOrDefault(x=>x.Id=="junior_novice_balanced_repeat");
bool? deterministic=first==null||repeated==null?null:first.AuthoritySha256==repeated.AuthoritySha256&&first.EventFnv64==repeated.EventFnv64&&first.Ticks==repeated.Ticks;
var assemblyHashes=new[]{typeof(PackAiRaceRunner).Assembly,typeof(ManualScopedFieldStepper).Assembly,typeof(CareerSave).Assembly,typeof(TimeTrialAthlete).Assembly}.Distinct().ToDictionary(a=>a.GetName().Name!,a=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(a.Location))).ToLowerInvariant());
string summary=JsonSerializer.Serialize(new{schema_version=1,id="m15_6_authoritative_career_race_study_v1",status=failures.Count==0&&deterministic!=false?"PASS":"FAIL",selection,venue="woodland_basin_v1",fixed_career_seed=71000,race_seeds=cases.Select(c=>c.RaceSeed).Distinct().Order().ToArray(),full_races=fullRaces,opening_only=results.Count(x=>x.OpeningOnly),completed=results.Count,failed=failures.Count,geometric_shots=results.Sum(x=>x.Shots),actual_ticks=totalTicks,stat_projection_checks=results.Sum(x=>x.StatProjectionChecks),deterministic_repeat=deterministic,wall_seconds=stopwatch.Elapsed.TotalSeconds,definition_sha256=definitionsCache.ToDictionary(x=>x.Key,x=>BalanceRegression.Hash(x.Value)),authority_assembly_sha256=assemblyHashes,scope="Representative fresh actual production30 Sprint races; separate actual MassStart first-split openings; one explicitly controlled full-effort/precise-aim route-automated junior. Not hundreds of full career races or human skill proof.",results,failures},options);
File.WriteAllText(Path.Combine(output,"summary.json"),summary);File.WriteAllText(Path.Combine(output,"summary-"+selection+".json"),summary);
if(failures.Count>0||deterministic==false)Environment.ExitCode=1;

sealed record StudyCase(string Id,CareerSave Save,bool Controlled,bool OpeningOnly,ulong RaceSeed=123);
sealed record FinishRow(int Rank,string AthleteId,int Overall,long ElapsedMilliseconds,int Penalties,double RangeSeconds,double AverageEffort,double MaximumStationarySeconds,double[] RangeEntryHeartRates,double MinimumAnaerobicReserve,double FinishMuscularFatigue);
sealed record HillRow(string AthleteId,double? CrestElapsedSeconds,double? MinimumSpeedMetersPerSecond,long SlowTicks,long ObservedTicks,double? AverageEffort,long TuckTicks,long UphillTuckTicks);
sealed record StudyResult(string Id,string PlayerId,int PlayerAge,int PlayerOverall,int[] PlayerStats,int? CreationBudgetSpent,bool Controlled,bool OpeningOnly,ulong RaceSeed,long Ticks,string? AuthoritySha256,string EventFnv64,int Shots,int Hits,int StatProjectionChecks,int ManualBoltCommands,int? PlayerRank,double? PlayerLeadSeconds,double? FirstSplitP90GapSeconds,double MaximumStationarySeconds,double WallSeconds,List<FinishRow> Results,HillRow[] FirstClimb);
sealed class EventAudit:IRaceEventSink
{
    public int Shots,Hits;public ulong Hash=14695981039346656037UL;
    void Add(long x){unchecked{Hash=(Hash^(ulong)x)*1099511628211UL;}}
    public void Record(RaceEvent e){Add(e.Tick.Value);Add(e.TickFrequency);Add((int)e.EventType);Add(e.Payload);foreach(char c in e.AthleteId.Value)Add(c);if(e.EventType==RaceEventType.ShotResolved){Shots++;if(e.Payload>0)Hits++;}}
}

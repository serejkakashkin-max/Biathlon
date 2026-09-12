using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using System.Text.Json;

string root=Path.GetFullPath(args.Length>0?args[0]:"../..");
string output=Path.Combine(root,"artifacts/balance/career-soak");Directory.CreateDirectory(output);
string Read(string name)=>File.ReadAllText(Path.Combine(root,"Assets/Resources/Biathlon",name));
string Fixture(string name)=>File.ReadAllText(Path.Combine(root,"tests/fixtures/tracks",name));
void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
var d=new CareerDefinitions(Read);var provider=new CareerRaceProvider(Read,Fixture,d);
var save=CareerFactory.Create(d.Roster,d.Calendar,"career_soak","Alex North","NOR","player_appearance",new SeededRandomSource(71000));
var allIds=new HashSet<string>();for(int i=0;i<save.AthleteCount;i++)allIds.Add(save.Athlete(i).Id);
long spent=0,earned=0,totalTicks=0;int totalRaces=0,maximumBytes=0,retired=0;var seasons=new List<object>();
var equipment=SkiEquipmentCatalog.FromJson(Read("ski_equipment_provisional_v1.json"));
CareerTrainingKind[] Plan(CareerSave state)
{CareerAthlete player=Enumerable.Range(0,state.AthleteCount).Select(state.Athlete).Single(a=>a.Id==state.PlayerId);var plan=CareerAiTraining.Plan(player,d.Training,d.Calendar,d.Policy,state.Week);return Enumerable.Range(0,plan.Count).Select(plan.Get).ToArray();}
using var raceLog=new StreamWriter(Path.Combine(output,"races.jsonl"));
for(int year=1;year<=15;year++)
{
    long yearSpent=0;int championAge=0;
    for(int week=1;week<=52;week++)
    {
        // A reproducible purchasing cohort: every athlete follows the same rule.
        for(int i=0;i<save.AthleteCount;i++)
        {
            var a=save.Athlete(i);long before=a.Money;
            if(week==1&&a.Money>=d.Economy.CampPrice&&a.PendingCampQuality==0)CareerEconomy.BuyCamp(a,d.Economy,d.Adaptation);
            if(week==2&&!CareerInventory.Owns(a,"ski_glide")&&a.Money>=d.Economy.EquipmentPrice("ski_glide")){CareerEconomy.BuyEquipment(a,d.Economy,equipment,"ski_glide");CareerInventory.Select(a,equipment,"ski_glide");}
            yearSpent+=before-a.Money;
        }
        var e=d.Calendar.AtWeek(save.Week);string beforeSave=save.ToJson();var priorIds=Enumerable.Range(0,save.AthleteCount).Select(i=>save.Athlete(i).Id).ToHashSet();
        var next=CareerSeasonEngine.AdvanceWeek(save,d,Plan(save),provider,s=>new SeededRandomSource(s));
        Require(save.ToJson()==beforeSave,"Source save mutated");
        if(year==8&&week==9)
        {
            var fork=CareerSave.FromJson(beforeSave);var replay=CareerSeasonEngine.AdvanceWeek(fork,d,Plan(fork),provider,s=>new SeededRandomSource(s));Require(replay.ToJson()==next.ToJson(),"Save/load race continuation diverged");
        }
        save=CareerSave.FromJson(next.ToJson());int bytes=System.Text.Encoding.UTF8.GetByteCount(save.ToJson());maximumBytes=Math.Max(maximumBytes,bytes);
        Require(save.AthleteCount==d.Roster.Population,"Population collapse");Require(bytes<4000000,"Save growth budget exceeded");
        for(int i=0;i<save.AthleteCount;i++)
        {
            var a=save.Athlete(i);Require(a.Money>=0,"Negative money");if(!priorIds.Contains(a.Id)){Require(allIds.Add(a.Id),"Reused athlete identity");retired++;}
            for(int stat=0;stat<CareerUnits.StatCount;stat++)Require(a.Stat((CareerStat)stat)>=0&&a.Stat((CareerStat)stat)<=CareerUnits.MaximumStat,"Invalid stat");
        }
        if(e!=null)
        {
            totalRaces++;totalTicks+=provider.LastTicks;Require(provider.LastShots==30*(e.Format==CompetitionFormat.Sprint?2:4)*5,"Missing geometric shots");
            var result=save.Race(save.RaceCount-1);for(int rank=1;rank<=result.Count;rank++)earned+=d.Economy.Prize(rank);
            raceLog.WriteLine(JsonSerializer.Serialize(new{season=year,week,event_id=e.Id,ticks=provider.LastTicks,shots=provider.LastShots,event_hash=provider.LastEventHash,result_hash=result.AuthoritySha256,save_bytes=bytes}));raceLog.Flush();
        }
        if(week==51)
        {
            var table=CareerStandings.Table(save,d.Scoring);var winner=Enumerable.Range(0,save.AthleteCount).Select(save.Athlete).Single(a=>a.Id==table[0].AthleteId);championAge=winner.Age;
            for(int i=1;i<table.Length;i++)Require(table[i-1].Points>=table[i].Points,"Broken standings order");
        }
    }
    spent+=yearSpent;Require(save.Season==year+1&&save.Week==1&&save.RaceCount==0,"Broken season boundary");Require(save.HistoryCount==Math.Min(year,d.History.RetainedSeasons),"Broken history budget");
    var athletes=Enumerable.Range(0,save.AthleteCount).Select(save.Athlete).ToArray();var player=athletes.Single(a=>a.Id==save.PlayerId);
    seasons.Add(new{season=year,population=save.AthleteCount,retired_total=retired,mean_age=athletes.Average(a=>a.Age),champion_age=championAge,mean_stat=athletes.Average(a=>Enumerable.Range(0,CareerUnits.StatCount).Average(s=>a.Stat((CareerStat)s)))/CareerUnits.StatScale,player_age=player.Age,player_tier=(int)player.Tier,player_mean_stat=Enumerable.Range(0,CareerUnits.StatCount).Average(s=>player.Stat((CareerStat)s))/CareerUnits.StatScale,player_money=player.Money,world_money=athletes.Sum(a=>a.Money),spent_minor=yearSpent,save_bytes=System.Text.Encoding.UTF8.GetByteCount(save.ToJson())});
    Console.WriteLine($"Season {year}/15 complete; races={totalRaces}; retired={retired}; population={save.AthleteCount}");
    File.WriteAllText(Path.Combine(output,"progress.json"),JsonSerializer.Serialize(seasons,new JsonSerializerOptions{WriteIndented=true}));
}
Require(totalRaces==90&&retired>0,"Incomplete living-world soak");
File.WriteAllText(Path.Combine(output,"final-save.json"),save.ToJson());
File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(new{schema_version=1,id="career_soak_v1",seasons=15,races=totalRaces,total_ticks=totalTicks,retired,unique_ids=allIds.Count,population=save.AthleteCount,maximum_save_bytes=maximumBytes,earned_minor=earned,spent_minor=spent,save_sha256=BalanceRegression.Hash(save.ToJson()),status="PASS"},new JsonSerializerOptions{WriteIndented=true}));

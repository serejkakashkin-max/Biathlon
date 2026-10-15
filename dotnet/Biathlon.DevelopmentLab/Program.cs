using Biathlon.Career;
using Biathlon.Simulation;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

// Controlled shared-development trajectories, deliberately separate from authoritative race balance.
string root=Path.GetFullPath(args.Length>0?args[0]:"../..");
int samples=args.Length>1?int.Parse(args[1],System.Globalization.CultureInfo.InvariantCulture):12;
int years=args.Length>2?int.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture):15;
string scenario=args.Length>3?args[3]:"control";
if(!new[]{"control","junior","national","world_cup","catalog_low","catalog_high"}.Contains(scenario))throw new ArgumentException("Unknown development scenario.");
if(samples<1||samples>100||years<10||years>15)throw new ArgumentOutOfRangeException("study bounds");
string output=Path.Combine(root,"artifacts/m15-6/development-study"+(scenario=="control"?"":"-"+scenario));Directory.CreateDirectory(output);
string Read(string n)=>File.ReadAllText(Path.Combine(root,"Assets/Resources/Biathlon",n));
var d=new CareerDefinitions(Read);var watch=Stopwatch.StartNew();var rows=new List<object>();var profiles=new Dictionary<string,int>();var peaks=new List<int>();var peakAges=new List<int>();var changes=new List<int>();var strategies=new Dictionary<string,List<int>>();
var starts=CareerStartDefinition.FromJson(Read("career_start_v1.json"));var creation=CareerCreationDefinition.FromJson(Read("career_creation_v1.json"));
long weeks=0,sessions=0;int stagnantSeasons=0,decliningSeasons=0,latePeaks=0,activeSeasonObservations=0;var seasonal=new List<object>();
CareerSave LastWeek(CareerSave save)
{string json=save.ToJson();int at=json.LastIndexOf("\"week\":",StringComparison.Ordinal);return CareerSave.FromJson(json[..at]+Regex.Replace(json[at..],"^(\"week\":)[0-9]+","${1}52"));}
for(int sample=0;sample<samples;sample++)
{
    ulong seed=(ulong)(91000+sample);var save=CareerFactory.Create(d.Roster,d.Calendar,"growth_study_"+sample,"Study athlete","NOR","player_appearance",new SeededRandomSource(seed));
    // Common-ability controls and authored creation presets isolate development choices.
    if(scenario.StartsWith("catalog_",StringComparison.Ordinal))
    {
        var catalog=new CareerAthleteCatalog(Read,"growth_catalog_"+sample,seed,s=>new SeededRandomSource(s));var sorted=catalog.Search(new CareerAthleteFilter());
        save=catalog.Confirm(sorted[scenario=="catalog_high"?0:sorted.Length-1].Id);
        d=CareerDefinitions.ForSave(Read,save);
    }
    else
    {
        string profile=scenario=="national"?"senior_debut":scenario=="world_cup"?"world_cup_debut":"junior_novice";
        var draft=new CareerCreationDraft(creation,starts,d.Ratings,d.Roster,seed){Name="Study athlete",Nation="NOR"};draft.SelectTier(profile);
        if(scenario!="control")
        {
            d=CareerDefinitions.ForNewProductionCareer(Read,profile);
            save=CareerFactory.Create(d.Roster,d.Calendar,"growth_study_"+sample,"Study athlete","NOR","player_appearance",new SeededRandomSource(seed),starts,profile);
        }
        int age=scenario=="national"?24:scenario=="world_cup"?25:18;draft.SetAge(age);
        string json=save.ToJson();int actor=0;
        json=Regex.Replace(json,"\"stats\":\\[[^\\]]*\\]",m=>
        {
            draft.SelectPreset(new[]{"balanced","skier","shooter"}[(actor++/3)%3]);
            return "\"stats\":["+string.Join(",",Enumerable.Range(0,13).Select(i=>scenario=="control"?49000:draft.Stat((CareerStat)i)))+"]";
        });
        json=Regex.Replace(json,"(\"age_years\":)[0-9]+","${1}"+age);save=CareerSave.FromJson(json);
        CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s),draft.Tier.BaselineStat);
    }
    // Cohort traces study these30 entrants. Their off-field peers remain an independently tested world concern.
    var previous=Enumerable.Range(0,30).Select(i=>BiathlonRatingEngine.Calculate(save.Athlete(i),d.Ratings).Overall).ToArray();var best=(int[])previous.Clone();var bestAge=Enumerable.Repeat(18,30).ToArray();var series=new List<int>[30];
    var initial=(int[])previous.Clone();var retiredAt=new int[30];var activeSeasons=new int[30];var initialAges=Enumerable.Range(0,30).Select(i=>save.Athlete(i).Age).ToArray();
    for(int i=0;i<30;i++){bestAge[i]=save.Athlete(i).Age;series[i]=new List<int>{previous[i]};string style=save.Athlete(i).Growth.StyleId;profiles[style]=profiles.GetValueOrDefault(style)+1;}
    for(int year=1;year<=years;year++)
    {
        var activeThisYear=Enumerable.Range(0,30).Select(i=>save.Athlete(i).Age<save.Athlete(i).Growth.RetirementAge).ToArray();
        for(int week=1;week<=52;week++)for(int i=0;i<30;i++)
        {
            var a=save.Athlete(i);int strategy=i%3;CareerWeekPlan plan;
            if(a.Age>=a.Growth.RetirementAge){if(retiredAt[i]==0)retiredAt[i]=year;continue;}
            if(strategy==0)plan=CareerAiTraining.Plan(a,d.Training,d.Calendar,d.Policy,week,d.Growth,d.Adaptation);
            else
            {
                int slots=d.Calendar.TrainingSlots(week);var requested=new List<CareerTrainingKind>();
                for(int slot=0;slot<slots;slot++)requested.Add(strategy==1?CareerTrainingKind.Intensity:
                    a.Fatigue>=d.Policy.RecoveryThreshold&&slot==0?CareerTrainingKind.Recovery:(CareerTrainingKind)((week+slot+year)%5));
                plan=new CareerWeekPlan(d.Training,d.Calendar,week,requested);
            }
            bool taper=week<52&&d.Calendar.AtWeek(week+1)!=null&&plan.UsedSlots<d.Calendar.TrainingSlots(week);
            CareerTrainingWeek.Apply(a,plan,d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,d.Policy.Quality,taper,d.Growth);
            // Authored aggregate load only. No points, podiums, wins or fabricated geometric shots.
            if(d.Calendar.AtWeek(week)!=null)CareerFatigue.Race(a,d.Fatigue,d.World.AggregateRaceMinutes*60000L);
            CareerFatigue.Rest(a,d.Fatigue,d.Calendar.TrainingSlots(week)-plan.UsedSlots);CareerMental.Rest(a,d.Mental);weeks++;sessions+=plan.Count;
            int rating=BiathlonRatingEngine.Calculate(a,d.Ratings).Overall;if(rating>best[i]){best[i]=rating;bestAge[i]=a.Age;}
        }
        for(int i=0;i<30;i++)
        {
            int rating=BiathlonRatingEngine.Calculate(save.Athlete(i),d.Ratings).Overall,delta=rating-previous[i];
            if(activeThisYear[i]){activeSeasons[i]++;activeSeasonObservations++;if(delta==0)stagnantSeasons++;if(delta<0)decliningSeasons++;}
            previous[i]=rating;series[i].Add(rating);
        }
        var activeRatings=previous.Where((_,i)=>activeThisYear[i]).ToArray();
        seasonal.Add(new{sample,year,cohort_mean_ovr_including_frozen_retirees=previous.Average(),active_athletes=activeRatings.Length,active_mean_ovr=activeRatings.Length==0?(double?)null:activeRatings.Average(),min_ovr=previous.Min(),max_ovr=previous.Max()});
        save=LastWeek(save);CareerDevelopment.SealSeason(save,d.Growth,d.Ratings);save=CareerHistory.SealSeason(save,d.Calendar,d.Scoring,d.History);
        for(int i=0;i<30;i++)if(save.Athlete(i).Age<save.Athlete(i).Growth.RetirementAge)CareerAgeing.AdvanceYear(save.Athlete(i),d.Adaptation,d.Growth);
    }
    for(int i=0;i<30;i++)
    {
        var a=save.Athlete(i);string strategy=new[]{"adaptive_ai","intensity_only","rotating_balanced"}[i%3];int rating=BiathlonRatingEngine.Calculate(a,d.Ratings).Overall;
        peaks.Add(best[i]);peakAges.Add(bestAge[i]);changes.Add(rating-initial[i]);if(bestAge[i]>=30)latePeaks++;
        if(!strategies.ContainsKey(strategy))strategies.Add(strategy,new List<int>());strategies[strategy].Add(best[i]);
        rows.Add(new{sample,athlete=a.Id,style=a.Growth.StyleId,talent_band=a.Growth.TalentBandId,strategy,preset=scenario.StartsWith("catalog_",StringComparison.Ordinal)?"existing":new[]{"balanced","skier","shooter"}[(i/3)%3],initial_age=initialAges[i],age=a.Age,initial_ovr=initial[i],ovr=rating,peak_ovr=best[i],peak_age=bestAge[i],retirement_age=a.Growth.RetirementAge,retired_in_year=retiredAt[i],active_seasons=activeSeasons[i],stagnation_ages=Enumerable.Range(initialAges[i],activeSeasons[i]).Where(a.Growth.IsStagnating).ToArray(),season_ovr=series[i]});
    }
    Console.WriteLine($"Development sample {sample+1}/{samples} complete; trajectories={(sample+1)*30}; years={years}");
}
var options=new JsonSerializerOptions{WriteIndented=true};
File.WriteAllText(Path.Combine(output,"trajectories.json"),JsonSerializer.Serialize(rows,options));File.WriteAllText(Path.Combine(output,"seasons.json"),JsonSerializer.Serialize(seasonal,options));
var summary=new{schema_version=1,id="m15_6_shared_development_study_v1",scope="fixed entrant cohorts developed with aggregate load until individual retirement, then frozen; zero authoritative race simulations; stagnant/declining counts include active seasons only",scenario,samples,athletes=samples*30,seasons_per_athlete=years,active_season_observations=activeSeasonObservations,shared_training_weeks=weeks,training_sessions=sessions,authoritative_races=0,elapsed_seconds=watch.Elapsed.TotalSeconds,peak_mean=peaks.Average(),peak_min=peaks.Min(),peak_max=peaks.Max(),peak_95_or_more=peaks.Count(n=>n>=95),peak_age_min=peakAges.Min(),peak_age_max=peakAges.Max(),late_peaks=latePeaks,stagnant_seasons=stagnantSeasons,declining_seasons=decliningSeasons,mean_end_delta=changes.Average(),styles=profiles,strategy_peak_means=strategies.ToDictionary(p=>p.Key,p=>p.Value.Average()),growth_policy_sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Read("career_growth_v1.json").Replace("\r\n","\n")))).ToLowerInvariant(),creation_policy_sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Read("career_creation_v1.json").Replace("\r\n","\n")))).ToLowerInvariant()};
File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summary,options));Console.WriteLine(JsonSerializer.Serialize(summary,options));

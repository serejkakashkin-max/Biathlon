using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>Read-only catalog projection. Hidden development state stays in the original world snapshot.</summary>
    public sealed class CareerCatalogAthlete
    {
        private readonly int[] stats=new int[CareerUnits.StatCount];
        public string Id {get;}
        public string Name {get;}
        public string Nation {get;}
        public int Age {get;}
        public CareerCircuit Circuit {get;}
        public string StartProfileId {get;}
        public string RosterVersion {get;}
        public AppearanceProfile Appearance {get;}
        public BiathlonRatings Ratings {get;}
        public CareerStat StrongestStat {get;}
        public CareerStat WeakestStat {get;}
        public string SpecializationId=>Ratings.Ski>Ratings.Shooting?"skier":Ratings.Shooting>Ratings.Ski?"shooter":"balanced";
        public int Stat(CareerStat stat){CareerUnits.Range((int)stat,0,12,"catalog stat");return stats[(int)stat];}
        internal string WorldJson {get;}
        internal CareerCatalogAthlete(CareerAthlete a,CareerCircuit circuit,string profile,string roster,string world,BiathlonRatingDefinition ratings)
        {
            Id=a.Id;Name=a.Name;Nation=a.Nation;Age=a.Age;Circuit=circuit;StartProfileId=profile;RosterVersion=roster;Appearance=a.Appearance;
            WorldJson=world;Ratings=BiathlonRatingEngine.Calculate(a,ratings);int best=0,worst=0;
            for(int i=0;i<13;i++){stats[i]=a.Stat((CareerStat)i);if(stats[i]>a.Stat((CareerStat)best))best=i;if(stats[i]<a.Stat((CareerStat)worst))worst=i;}
            StrongestStat=(CareerStat)best;WeakestStat=(CareerStat)worst;
        }
    }
    public sealed class CareerAthleteFilter
    {
        public string Name {get;set;}="";
        public string Nation {get;set;}
        public CareerCircuit? Circuit {get;set;}
        public int MinimumAge {get;set;}=16;
        public int MaximumAge {get;set;}=70;
        public int MinimumRating {get;set;}=1;
        public int MaximumRating {get;set;}=99;
        public string SpecializationId {get;set;}
        internal bool Matches(CareerCatalogAthlete a)=>
            (string.IsNullOrWhiteSpace(Name)||a.Name.IndexOf(Name.Trim(),StringComparison.OrdinalIgnoreCase)>=0)&&
            (Nation==null||a.Nation==Nation)&&(!Circuit.HasValue||a.Circuit==Circuit)&&
            a.Age>=MinimumAge&&a.Age<=MaximumAge&&a.Ratings.Overall>=MinimumRating&&a.Ratings.Overall<=MaximumRating&&
            (SpecializationId==null||a.SpecializationId==SpecializationId);
        internal void Validate()
        {
            if(Name==null||Name.Length>100||Name.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new ArgumentException("Invalid catalog search.");
            if(Nation!=null)CareerUnits.Id(Nation);if(SpecializationId!=null&&SpecializationId!="balanced"&&SpecializationId!="skier"&&SpecializationId!="shooter")throw new ArgumentException("Unknown catalog specialization.");
            if(Circuit.HasValue)CareerUnits.Range((int)Circuit.Value,1,3,"catalog circuit");
            CareerUnits.Range(MinimumAge,16,70,"catalog age floor");CareerUnits.Range(MaximumAge,MinimumAge,70,"catalog age ceiling");
            CareerUnits.Range(MinimumRating,1,99,"catalog rating floor");CareerUnits.Range(MaximumRating,MinimumRating,99,"catalog rating ceiling");
        }
    }
    /// <summary>One generated fictional world catalog per creation session. Navigation never regenerates an athlete.</summary>
    public sealed class CareerAthleteCatalog
    {
        private readonly CareerCatalogAthlete[] entries;
        private readonly Dictionary<string,CareerCatalogAthlete> byId=new Dictionary<string,CareerCatalogAthlete>(StringComparer.Ordinal);
        public string CareerId {get;}
        public ulong DraftSeed {get;}
        public string RosterVersion {get;}
        public int Count=>entries.Length;
        public CareerCatalogAthlete Get(int index)=>entries[index];
        public CareerCatalogAthlete Get(string id)=>id!=null&&byId.TryGetValue(id,out var entry)?entry:throw new ArgumentException("Unknown catalog athlete.");
        public CareerAthleteCatalog(Func<string,string> read,string careerId,ulong seed,Func<ulong,IRandomSource> restoreRandom)
        {
            if(read==null||restoreRandom==null)throw new ArgumentNullException();CareerUnits.Id(careerId);
            CareerId=careerId;DraftSeed=seed;
            var starts=CareerStartDefinition.FromJson(read("career_start_v1.json"));var roster=CareerRosterDefinition.FromJson(read("career_roster_v1.json"));
            RosterVersion=roster.Id;var ratings=BiathlonRatingDefinition.FromJson(read("career_ratings_v1.json"));
            var random=restoreRandom(seed);if(random==null||random.State!=seed)throw new ArgumentException("Catalog random checkpoint mismatch.");
            var worlds=new CareerSave[starts.Count];entries=new CareerCatalogAthlete[checked(starts.Count*roster.Population)];
            for(int i=0;i<starts.Count;i++)
            {
                var p=starts.Get(i);var calendar=CareerCalendar.FromJson(read(p.CalendarResource));
                var world=CareerFactory.Create(roster,calendar,careerId,"Catalog athlete",roster.Nation(0),"appearance_default",random,starts,p.Id);
                // Index0 is also an authentic fictional athlete, rather than a named placeholder.
                var first=world.Athlete(0);first.Name=roster.GivenName(CareerFactory.Draw(random,0,roster.GivenNameCount-1))+" "+roster.FamilyName(CareerFactory.Draw(random,0,roster.FamilyNameCount-1));
                worlds[i]=world;
            }
            for(int i=0;i<worlds.Length;i++)
            {
                var world=worlds[i];world.RebaseUnstartedRoster((long)i*roster.Population,entries.Length);world.RandomState=random.State;
                new CareerRivalAppearance(read).InitializeNewWorld(world,includePlayer:true);
                CareerDevelopment.InitializeWorld(world,CareerGrowthDefinition.FromJson(read("career_growth_v1.json")),ratings,restoreRandom);
            }
            for(int i=0;i<worlds.Length;i++)
            {
                var world=worlds[i];var pool=new List<CareerWorldEntry>();
                for(int other=0;other<worlds.Length;other++)if(other!=i)
                {var otherCircuit=CareerCalendar.FromJson(read(starts.Get(other).CalendarResource)).Circuit;for(int j=0;j<worlds[other].AthleteCount;j++)pool.Add(new CareerWorldEntry(worlds[other].Athlete(j),otherCircuit));}
                world.SetPool(CareerWorldDefinition.FromJson(read("career_world_v1.json")).Id,pool);
                string snapshot=world.ToJson();var circuit=CareerCalendar.FromJson(read(starts.Get(i).CalendarResource)).Circuit;
                for(int j=0;j<world.AthleteCount;j++)
                {
                    var row=new CareerCatalogAthlete(world.Athlete(j),circuit,world.StartProfileId,roster.Id,snapshot,ratings);
                    entries[i*roster.Population+j]=row;byId.Add(row.Id,row);
                }
            }
        }
        public CareerCatalogAthlete[] Search(CareerAthleteFilter filter)
        {
            if(filter==null)throw new ArgumentNullException(nameof(filter));filter.Validate();var result=new List<CareerCatalogAthlete>();
            foreach(var entry in entries)if(filter.Matches(entry))result.Add(entry);
            result.Sort((a,b)=>{int compared=b.Ratings.Overall.CompareTo(a.Ratings.Overall);return compared!=0?compared:StringComparer.Ordinal.Compare(a.Id,b.Id);});return result.ToArray();
        }
        public CareerSave Confirm(string athleteId)
        {
            var entry=Get(athleteId);var save=CareerSave.FromJson(entry.WorldJson);CareerAthlete athlete=null;
            for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==entry.Id)athlete=save.Athlete(i);
            save.AdoptPlayer(entry.Id,CareerCreationRecord.FromExisting(athlete,entry.StartProfileId,RosterVersion,entry.SpecializationId));
            return save;
        }
    }
}
